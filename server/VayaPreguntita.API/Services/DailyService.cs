using AutoMapper;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Coins;
using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.DTOs.Streaks;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Services;

public class DailyService(
    AppDbContext context,
    IMapper mapper,
    INotificationService notificationService,
    IStreakService streakService,
    ICoinService coinService,
    IAuthorGuessService authorGuessService,
    IFlashService flashService,
    TimeProvider clock
) : IDailyService
{
    // ==========================================
    // GET CURRENT STATUS (§4.7)
    // Surfaces BOTH coexisting states: today (the open question) and selection
    // (the next-day question being chosen). The lifecycle is driven off ActivatedAt
    // state, not the calendar date — so it is correct across the daily-time boundary.
    // ==========================================
    public async Task<DailyStatusDto> GetCurrentStatusAsync(int groupId, int userId)
    {
        var group =
            await context
                .Groups.Include(g => g.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.Id == groupId)
            ?? throw new KeyNotFoundException("Group not found.");

        // The currently-open question = the most recently activated entry. The previous
        // one is "closed" precisely because a newer one activated.
        var openEntry = await context
            .DailyEntries.Include(d => d.Question)
                .ThenInclude(q => q.Options)
            .Where(d => d.GroupId == groupId && d.ActivatedAt != null)
            .OrderByDescending(d => d.Date)
            .FirstOrDefaultAsync();

        // The next question to activate (the one the selector can still change).
        var pendingEntry = await context
            .DailyEntries.Include(d => d.Question)
                .ThenInclude(q => q.Options)
            .Where(d => d.GroupId == groupId && d.ActivatedAt == null)
            .OrderBy(d => d.Date)
            .FirstOrDefaultAsync();

        return new DailyStatusDto
        {
            Today = await BuildTodayStateAsync(openEntry, group, userId),
            Selection = BuildSelectionState(pendingEntry, group, userId),
            MyStreak = await streakService.GetMyStreakAsync(groupId, userId),
            Flashes = await flashService.GetOpenAsync(groupId, userId),
        };
    }

    private async Task<TodayStateDto> BuildTodayStateAsync(
        DailyEntry? openEntry,
        Group group,
        int userId
    )
    {
        if (openEntry == null)
            return new TodayStateDto { Status = "no_question" };

        // Who has voted is public already (the "X ha votado" push); only the answers wait.
        var voterIds = await context
            .Votes.Where(v => v.QuestionId == openEntry.QuestionId)
            .Select(v => v.UserId)
            .Distinct()
            .ToListAsync();
        var hasVoted = voterIds.Contains(userId);

        var state = new TodayStateDto
        {
            Question = mapper.Map<QuestionToVoteDto>(openEntry.Question),
            UserHasVoted = hasVoted,
            // The open question activated at Date@T and closes 24 h later, at (Date+1)@T.
            ClosesAt = DailyClock.ToUtc(openEntry.Date.AddDays(1), group.DailyQuestionTime),
            VoterIds = voterIds,
            MemberCount = group.Members.Count,
        };

        if (hasVoted)
        {
            state.Status = "results";
            state.Results = await CalculateResultsAsync(openEntry.Question);
            state.Guess = await authorGuessService.GetStateAsync(openEntry, userId);
        }
        else
        {
            state.Status = "voting";
        }

        return state;
    }

    private SelectionStateDto? BuildSelectionState(
        DailyEntry? pendingEntry,
        Group group,
        int userId
    )
    {
        if (pendingEntry != null)
        {
            var isSelector = pendingEntry.SelectorUserId == userId;
            var raceOpen = IsRaceOpen(pendingEntry);

            return new SelectionStateDto
            {
                Date = pendingEntry.Date,
                ActivatesAt = DailyClock.ToUtc(pendingEntry.Date, group.DailyQuestionTime),
                // Who picks is only told to the selector: the selector is often the author,
                // and that would spoil "guess the author" (§17.6).
                SelectorUserId = isSelector ? pendingEntry.SelectorUserId : null,
                SelectorUsername = isSelector
                    ? group.Members.FirstOrDefault(m => m.UserId == userId)?.User.Username
                    : null,
                IsCurrentUserSelector = isSelector,
                IsAutoSelected = pendingEntry.SelectionKind == SelectionKind.Auto,
                RaceOpen = raceOpen,
                // Only the selector sees tomorrow's question — keeps the surprise. During the
                // race (§17.5) anyone may keep it, so everyone gets to see it.
                PendingQuestion = isSelector || raceOpen
                    ? mapper.Map<QuestionToVoteDto>(pendingEntry.Question)
                    : null,
            };
        }

        // No pending entry: either the group hasn't started (< 2 members) or the next
        // selector hasn't been drawn yet (the tick preselects right after T). The draw is
        // random (§17.4), so nobody is the selector until it happens.
        return null;
    }

    // ==========================================
    // SELECT QUESTION (§4.1, §4.2)
    // The selector chooses the NEXT-day question. It is NEVER activated here — activation
    // happens only at T in DailyPreselectionService.
    // ==========================================
    public async Task<(SelectResult Result, CoinsEarnedDto? Coins)> SelectQuestionAsync(
        int groupId,
        int userId,
        SelectQuestionDto dto
    )
    {
        var group =
            await context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId)
            ?? throw new KeyNotFoundException("Group not found.");

        // The pending (next-day) entry is the only one a selector may change.
        var pendingEntry = await context
            .DailyEntries.Where(d => d.GroupId == groupId && d.ActivatedAt == null)
            .OrderBy(d => d.Date)
            .FirstOrDefaultAsync();

        var access = CheckSelectAccess(pendingEntry, userId);
        if (access != SelectResult.Success)
            return (access, null);

        // The race claim and the choice commit together: a choice that fails validation
        // gives the turn back.
        await using var transaction = await context.Database.BeginTransactionAsync();
        var wonRace = false;
        if (IsRaceOpen(pendingEntry!))
        {
            if (!await ClaimRaceAsync(pendingEntry!, userId))
                return (SelectResult.RaceLost, null);
            wonRace = pendingEntry!.SelectorUserId != userId;
            pendingEntry.SelectorUserId = userId;
        }

        var (question, resolveResult) = await ResolveSelectedQuestionAsync(dto, group, userId);
        if (resolveResult != SelectResult.Success)
            return (resolveResult, null);

        var oldQuestionId = pendingEntry!.QuestionId;
        pendingEntry.QuestionId = question!.Id;
        pendingEntry.SelectionKind = SelectionKindFor(dto, pendingEntry.SelectionKind);

        // Always release the old question when the selection changes, regardless of
        // whether it was auto-selected or manually chosen. This keeps pool questions
        // available after a re-selection instead of leaving them stranded as IsUsed=true.
        if (oldQuestionId != question.Id)
        {
            var oldQuestion = await context.Questions.FindAsync(oldQuestionId);
            if (oldQuestion != null)
            {
                if (oldQuestion.Source == QuestionSource.Pack)
                    context.Questions.Remove(oldQuestion); // a pack clone has no pool value
                else
                {
                    oldQuestion.IsUsed = false;
                    oldQuestion.DateActivated = null;
                }
            }
        }

        await context.SaveChangesAsync();
        var coins = wonRace ? await PayRaceRewardAsync(pendingEntry, userId) : null;
        await transaction.CommitAsync();
        return (SelectResult.Success, coins);
    }

    // Inline create = Created (earns the selector's bonus, §17.2). Editing the pending
    // question in place keeps a Created entry Created; any other choice is Picked.
    private static SelectionKind SelectionKindFor(SelectQuestionDto dto, SelectionKind current) =>
        dto.NewQuestion == null ? SelectionKind.Picked
        : dto.OverwriteQuestionId.HasValue
            ? (current == SelectionKind.Created ? SelectionKind.Created : SelectionKind.Picked)
        : SelectionKind.Created;

    // ==========================================
    // CONFIRM SELECTION (§17.4)
    // The selector keeps the auto-selected question: it becomes an active choice without
    // changing the question. Idempotent — an entry already actively chosen stays as it is.
    // ==========================================
    public async Task<(SelectResult Result, CoinsEarnedDto? Coins)> ConfirmSelectionAsync(
        int groupId,
        int userId
    )
    {
        var pendingEntry = await context
            .DailyEntries.Where(d => d.GroupId == groupId && d.ActivatedAt == null)
            .OrderBy(d => d.Date)
            .FirstOrDefaultAsync();

        var access = CheckSelectAccess(pendingEntry, userId);
        if (access != SelectResult.Success)
            return (access, null);
        if (pendingEntry!.SelectionKind != SelectionKind.Auto)
            return (SelectResult.Success, null);

        await using var transaction = await context.Database.BeginTransactionAsync();
        var wonRace = false;
        if (IsRaceOpen(pendingEntry))
        {
            if (!await ClaimRaceAsync(pendingEntry, userId))
                return (SelectResult.RaceLost, null);
            wonRace = pendingEntry.SelectorUserId != userId;
            pendingEntry.SelectorUserId = userId;
        }

        pendingEntry.SelectionKind = SelectionKind.Confirmed;
        await context.SaveChangesAsync();
        var coins = wonRace ? await PayRaceRewardAsync(pendingEntry, userId) : null;
        await transaction.CommitAsync();
        return (SelectResult.Success, coins);
    }

    // ==========================================
    // RACE TO SELECT (§17.5)
    // From T − 10 min, while the pending question is still the automatic pick, any member
    // may choose and the first write wins. Before that, only the drawn selector may.
    // ==========================================
    private static bool IsRaceOpen(DailyEntry entry) =>
        entry.RaceOpenedAt != null && entry.SelectionKind == SelectionKind.Auto;

    private static SelectResult CheckSelectAccess(DailyEntry? entry, int userId)
    {
        // Until the tick has drawn a selector (§17.4), nobody's turn has started.
        if (entry == null)
            return SelectResult.NotYourTurn;
        if (entry.SelectorUserId == userId)
            return SelectResult.Success;
        if (entry.RaceOpenedAt == null)
            return SelectResult.NotYourTurn;
        // The race opened, but the selector or a faster member has already chosen.
        return entry.SelectionKind == SelectionKind.Auto ? SelectResult.Success : SelectResult.RaceLost;
    }

    // Conditional update: the row lock queues concurrent claims, and once the first one
    // commits a real choice the others match nothing. The original selector claims too,
    // so a racer can never overwrite their choice.
    private async Task<bool> ClaimRaceAsync(DailyEntry entry, int userId) =>
        await context
            .DailyEntries.Where(d =>
                d.Id == entry.Id && d.ActivatedAt == null && d.SelectionKind == SelectionKind.Auto
            )
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.SelectorUserId, userId)) == 1;

    // The winner is paid now; the active-choice reward follows at T as for any selector.
    private async Task<CoinsEarnedDto> PayRaceRewardAsync(DailyEntry entry, int userId)
    {
        var grant = await coinService.GrantAsync(
            userId,
            CoinReason.RaceWon,
            CoinRewards.RaceWon,
            entry.GroupId,
            entry.Id
        );
        return new CoinsEarnedDto
        {
            Earned = grant.Granted,
            CapReached = grant.CapReached,
            Balance = await coinService.GetBalanceAsync(userId),
        };
    }

    // Resolves the chosen question (pool / pack template / inline-create / overwrite) WITHOUT activating it.
    private async Task<(Question? question, SelectResult result)> ResolveSelectedQuestionAsync(
        SelectQuestionDto dto,
        Group group,
        int userId
    )
    {
        // Overwrite path: update an existing pending question's content in place.
        // Used when the selector edits an already-chosen Scale/CustomPoll question.
        // Runs before the plain NewQuestion branch so we don't create a duplicate entity.
        if (dto.OverwriteQuestionId.HasValue && dto.NewQuestion != null)
        {
            var memberIds = group.Members.Select(m => m.UserId).ToHashSet();
            var membershipError = QuestionMembershipValidator.Validate(dto.NewQuestion, memberIds, userId);
            if (membershipError != null)
                return (null, SelectResult.InvalidQuestion);

            var existing = await context.Questions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == dto.OverwriteQuestionId.Value && q.GroupId == group.Id);
            if (existing == null)
                return (null, SelectResult.QuestionNotFound);

            var overwriteIsAdmin = group.Members.Any(m => m.UserId == userId && m.IsAdmin);
            if (existing.CreatorId != null && existing.CreatorId != userId && !overwriteIsAdmin)
                return (null, SelectResult.InvalidQuestion);
            existing.CreatorId ??= userId;

            // A pack clone keeps its translations only while its wording is untouched; once the
            // selector rewrites it, everyone reads the selector's own words (rama 24).
            var oldOptions = existing.Options.OrderBy(o => o.Id).ToList();
            var newOptions =
                dto.NewQuestion.Type == QuestionType.CustomPoll ? dto.NewQuestion.Options : [];
            var reworded =
                existing.Text != dto.NewQuestion.Text
                || !oldOptions.Select(o => o.Text).SequenceEqual(newOptions.Select(o => o.Text));

            existing.Text = dto.NewQuestion.Text;
            existing.Type = dto.NewQuestion.Type;
            existing.Metadata = QuestionMetadataBuilder.Build(dto.NewQuestion);
            if (reworded)
                existing.TextTranslations = [];
            existing.Options.Clear();
            if (newOptions.Count > 0)
                existing.Options = newOptions
                    .Select((o, i) => new Option
                    {
                        Text = o.Text,
                        TextTranslations = reworded ? [] : new(oldOptions[i].TextTranslations),
                    })
                    .ToList();

            return (existing, SelectResult.Success);
        }

        if (dto.NewQuestion != null)
        {
            // Same membership validation as POST /questions (§9). Structural rules already
            // ran via SelectQuestionDtoValidator -> CreateQuestionDtoValidator.
            var memberIds = group.Members.Select(m => m.UserId).ToHashSet();
            var membershipError = QuestionMembershipValidator.Validate(
                dto.NewQuestion,
                memberIds,
                userId
            );
            if (membershipError != null)
                return (null, SelectResult.InvalidQuestion);

            var created = new Question
            {
                Text = dto.NewQuestion.Text,
                Type = dto.NewQuestion.Type,
                CreatorId = userId,
                GroupId = group.Id,
                IsUsed = true,
                Source = QuestionSource.UserCreated,
                Metadata = QuestionMetadataBuilder.Build(dto.NewQuestion),
                // No DateActivated — set only at activation.
            };

            if (
                dto.NewQuestion.Type == QuestionType.CustomPoll
                && dto.NewQuestion.Options.Count > 0
            )
            {
                created.Options = dto
                    .NewQuestion.Options.Select(o => new Option { Text = o.Text })
                    .ToList();
            }

            context.Questions.Add(created);
            await context.SaveChangesAsync();
            return (created, SelectResult.Success);
        }

        if (dto.ExistingQuestionId.HasValue)
        {
            // TeamsOverride also allows re-selecting an already-used question (the current
            // pending DM) to update its teams without creating a duplicate.
            var existing = await context.Questions.FirstOrDefaultAsync(q =>
                q.Id == dto.ExistingQuestionId.Value
                && q.GroupId == group.Id
                && (!q.IsUsed || dto.TeamsOverride != null)
            );
            if (existing == null)
                return (null, SelectResult.QuestionNotFound);

            if (dto.TeamsOverride != null)
            {
                var teamsIsAdmin = group.Members.Any(m => m.UserId == userId && m.IsAdmin);
                if (existing.CreatorId != null && existing.CreatorId != userId && !teamsIsAdmin)
                    return (null, SelectResult.InvalidQuestion);
                existing.CreatorId ??= userId;

                if (existing.Type == QuestionType.Deathmatch)
                    existing.Metadata.Teams = dto.TeamsOverride;
            }

            existing.IsUsed = true;

            return (existing, SelectResult.Success);
        }

        if (dto.TemplateId.HasValue)
        {
            var template = await context
                .QuestionTemplates.Include(t => t.Options)
                .FirstOrDefaultAsync(t => t.Id == dto.TemplateId.Value && !t.IsRetired);
            if (template == null)
                return (null, SelectResult.QuestionNotFound);

            var usedTemplateIds = await TemplateReusePolicy.GetUsedTemplateIdsAsync(context, group.Id);
            if (usedTemplateIds.Contains(dto.TemplateId.Value))
            {
                var enabledPackIds = await TemplateReusePolicy.GetEnabledPackIdsAsync(context, group.Id);
                var exhausted = await TemplateReusePolicy.IsGroupExhaustedAsync(
                    context,
                    group.Id,
                    enabledPackIds,
                    usedTemplateIds
                );
                if (!exhausted)
                    return (null, SelectResult.TemplateAlreadyUsed);
            }

            var memberIds = group.Members.Select(m => m.UserId).ToList();
            var cloned = TemplateCloner.CloneToGroup(template, group.Id, memberIds);
            cloned.IsUsed = true;
            context.Questions.Add(cloned);
            await context.SaveChangesAsync();
            return (cloned, SelectResult.Success);
        }

        return (null, SelectResult.QuestionNotFound);
    }

    // ==========================================
    // VOTE
    // Votes on the currently-open question (latest activated entry). Returns fresh results
    // plus the voter's streak change (rama 19) and the coins it earned (rama 34).
    // ==========================================
    public async Task<(VoteResult result, VoteResponseDto? response)> VoteAsync(
        int groupId,
        int userId,
        CreateVoteDto dto
    )
    {
        var openEntry = await context
            .DailyEntries.Include(d => d.Question)
                .ThenInclude(q => q.Options)
            .Where(d => d.GroupId == groupId && d.ActivatedAt != null)
            .OrderByDescending(d => d.Date)
            .FirstOrDefaultAsync();

        if (openEntry == null)
            return (VoteResult.NoActiveQuestion, null);

        var alreadyVoted = await context.Votes.AnyAsync(v =>
            v.QuestionId == openEntry.QuestionId && v.UserId == userId
        );
        if (alreadyVoted)
            return (VoteResult.AlreadyVoted, null);

        var question = openEntry.Question;
        var memberIds = await context
            .GroupMembers.Where(m => m.GroupId == groupId)
            .Select(m => m.UserId)
            .ToHashSetAsync();

        if (VotePayload.Validate(dto, question, memberIds) != null)
            return (VoteResult.InvalidPayload, null);

        context.Votes.AddRange(VotePayload.Build(dto, question, userId));
        await context.SaveChangesAsync();

        var streak = await streakService.RegisterVoteAsync(groupId, userId, openEntry);
        var coins = await PayVoteRewardsAsync(groupId, userId, openEntry, streak);

        var group = await context.Groups.FindAsync(groupId);
        var voter = await context.Users.FindAsync(userId);
        if (group != null && voter != null)
            _ = notificationService.SendUserVotedAsync(groupId, userId, group.Name, voter.Username);

        var results = await CalculateResultsAsync(question);
        return (
            VoteResult.Success,
            new VoteResponseDto
            {
                Results = results,
                Streak = streak,
                Coins = coins,
                Guess = await authorGuessService.GetStateAsync(openEntry, userId),
            }
        );
    }

    // ==========================================
    // LATE VOTE (§17.7)
    // Votes on a closed question from the history. It counts in the tally with no mark, but
    // is stored as IsLate so it never earns streak, coins or monthly awards; no push either.
    // Same validation as the live vote (targets must be current members).
    // ==========================================
    public async Task<VoteResult> LateVoteAsync(int groupId, int userId, DateOnly date, CreateVoteDto dto)
    {
        var entry = await context
            .DailyEntries.Include(d => d.Question)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(d => d.GroupId == groupId && d.Date == date && d.ActivatedAt != null);
        if (entry == null)
            return VoteResult.QuestionNotFound;

        // Closed = a newer entry has activated (the lifecycle runs on ActivatedAt, not dates).
        var isClosed = await context.DailyEntries.AnyAsync(d =>
            d.GroupId == groupId && d.ActivatedAt != null && d.Date > date
        );
        if (!isClosed)
            return VoteResult.QuestionNotClosed;

        var alreadyVoted = await context.Votes.AnyAsync(v =>
            v.QuestionId == entry.QuestionId && v.UserId == userId
        );
        if (alreadyVoted)
            return VoteResult.AlreadyVoted;

        var memberIds = await context
            .GroupMembers.Where(m => m.GroupId == groupId)
            .Select(m => m.UserId)
            .ToHashSetAsync();
        if (VotePayload.Validate(dto, entry.Question, memberIds) != null)
            return VoteResult.InvalidPayload;

        var votes = VotePayload.Build(dto, entry.Question, userId);
        votes.ForEach(v => v.IsLate = true);
        context.Votes.AddRange(votes);
        await context.SaveChangesAsync();

        return VoteResult.Success;
    }

    // Coins for an on-time vote (§17.2, §17.15): the vote itself, the first-voter bonus, the
    // daily streak bonus and any tier or milestone this vote reached for the first time ever.
    private async Task<CoinsEarnedDto> PayVoteRewardsAsync(
        int groupId,
        int userId,
        DailyEntry entry,
        StreakUpdateDto streak
    )
    {
        var grants = new List<CoinGrant>
        {
            await coinService.GrantAsync(userId, CoinReason.Vote, CoinRewards.Vote, groupId, entry.Id),
        };

        // Conditional write: only the vote that claims the empty slot gets the bonus.
        var isFirstVoter =
            await context
                .DailyEntries.Where(d => d.Id == entry.Id && d.FirstVoterUserId == null)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.FirstVoterUserId, userId)) == 1;
        if (isFirstVoter)
            grants.Add(
                await coinService.GrantAsync(
                    userId,
                    CoinReason.FirstVoter,
                    CoinRewards.FirstVoter,
                    groupId,
                    entry.Id
                )
            );

        // The daily streak bonus (§17.15), shown apart from the vote reward.
        var streakBonus = new CoinGrant(0, false);
        if (streak.Current > streak.Previous)
        {
            streakBonus = await coinService.GrantAsync(
                userId,
                CoinReason.StreakBonus,
                CoinRewards.StreakBonus(streak.Current),
                groupId,
                entry.Id
            );
            grants.Add(streakBonus);
        }

        // One-off tier and milestone coins: only the first time the user ever reaches them.
        if (streak.Current > streak.Previous && streak.IsNewRecord)
        {
            if (CoinRewards.StreakTiers.TryGetValue(streak.Current, out var tierCoins))
                grants.Add(
                    await coinService.GrantAsync(userId, CoinReason.StreakTier, tierCoins, groupId, entry.Id)
                );
            if (CoinRewards.StreakMilestones.TryGetValue(streak.Current, out var milestoneCoins))
                grants.Add(
                    await coinService.GrantAsync(
                        userId,
                        CoinReason.StreakMilestone,
                        milestoneCoins,
                        groupId,
                        entry.Id
                    )
                );
        }

        return new CoinsEarnedDto
        {
            Earned = grants.Sum(g => g.Granted),
            StreakBonus = streakBonus.Granted,
            CapReached = grants.Any(g => g.CapReached),
            Balance = await coinService.GetBalanceAsync(userId),
        };
    }

    // ==========================================
    // PRESELECT FOR TOMORROW (called by DailyPreselectionService)
    // Only groups with ≥ 2 members run a cycle (§4.3, §4.5).
    // ==========================================
    public async Task<int?> PreselectForGroupAsync(
        int groupId,
        DateOnly date,
        bool activateImmediately = false
    )
    {
        // Idempotent: if an entry already exists for that date, do nothing.
        var exists = await context.DailyEntries.AnyAsync(d =>
            d.GroupId == groupId && d.Date == date
        );

        if (exists)
            return null;

        var group = await context
            .Groups.Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null || group.Members.Count < 2)
            return null;

        var selector = await DrawSelectorAsync(group, isBootstrap: activateImmediately);

        // Pick a random unused question from the group pool.
        var question = await context
            .Questions.Where(q => q.GroupId == groupId && !q.IsUsed)
            .OrderBy(_ => Guid.NewGuid())
            .FirstOrDefaultAsync();

        if (question == null)
        {
            // Fallback: clone a base-pack template (§6 — there is always a question). Prefer
            // a template never used in this group (§6.4 — reuse is a last resort), falling
            // back to any enabled-pack template only once the group is fully exhausted.
            var enabledPackIds = await TemplateReusePolicy.GetEnabledPackIdsAsync(context, groupId);
            var usedTemplateIds = await TemplateReusePolicy.GetUsedTemplateIdsAsync(context, groupId);

            var template =
                await context
                    .QuestionTemplates.Include(t => t.Options)
                    .Where(t =>
                        !t.IsRetired
                        && enabledPackIds.Contains(t.PackId)
                        && !usedTemplateIds.Contains(t.Id)
                    )
                    .OrderBy(_ => Guid.NewGuid())
                    .FirstOrDefaultAsync()
                ?? await context
                    .QuestionTemplates.Include(t => t.Options)
                    .Where(t => !t.IsRetired && enabledPackIds.Contains(t.PackId))
                    .OrderBy(_ => Guid.NewGuid())
                    .FirstOrDefaultAsync();

            if (template == null)
                return null; // No active templates (should not happen with the base pack).

            var memberIds = group.Members.Select(m => m.UserId).ToList();
            question = TemplateCloner.CloneToGroup(template, groupId, memberIds);
            question.IsUsed = true;
            context.Questions.Add(question);
            await context.SaveChangesAsync(); // persist to obtain the cloned question Id
        }
        else
        {
            question.IsUsed = true;
        }
        // DateActivated/ActivatedAt stay null here unless activateImmediately — the bootstrap
        // exception (§4.1) for a group's very first question. Every later cycle activates at T.
        var nowUtc = DailyClock.UtcNow(clock);
        if (activateImmediately)
            question.DateActivated = nowUtc;

        context.DailyEntries.Add(
            new DailyEntry
            {
                GroupId = groupId,
                Date = date,
                QuestionId = question.Id,
                SelectorUserId = selector.UserId,
                SelectionKind = SelectionKind.Auto,
                PreselectedAt = nowUtc,
                ActivatedAt = activateImmediately ? nowUtc : null,
            }
        );

        await context.SaveChangesAsync();
        return selector.UserId;
    }

    // ==========================================
    // PRIVATE HELPERS
    // ==========================================

    // Shuffled rounds (§17.4). The group's first question (bootstrap, §4.1) always goes to
    // the creator, which spends their round-1 turn; every later turn is a random draw.
    private async Task<GroupMember> DrawSelectorAsync(Group group, bool isBootstrap)
    {
        var creator = group.Members.FirstOrDefault(m => m.UserId == group.CreatorId);
        if (isBootstrap && creator != null)
        {
            SelectorRotation.ClaimTurn(group, creator);
            return creator;
        }

        var previousSelectorId = await context
            .DailyEntries.Where(d => d.GroupId == group.Id)
            .OrderByDescending(d => d.Date)
            .Select(d => (int?)d.SelectorUserId)
            .FirstOrDefaultAsync();

        return SelectorRotation.Draw(group, previousSelectorId, Random.Shared);
    }

    // ==========================================
    // CALCULATE RESULTS
    // ==========================================
    private async Task<QuestionResultDto> CalculateResultsAsync(Question question)
    {
        var allVotes = await context
            .Votes.Include(v => v.User)
            .Include(v => v.SelectedTargetUser)
            .Where(v => v.QuestionId == question.Id)
            .ToListAsync();

        var usersById = await ResultsBuilder.BuildDeathmatchUsersById(context, question);

        return ResultsBuilder.Build(question, allVotes, mapper, usersById);
    }
}
