using AutoMapper;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Services;

public class DailyService(
    AppDbContext context,
    IMapper mapper,
    INotificationService notificationService,
    IStreakService streakService
) : IDailyService
{
    // Max length of an OpenText answer or a CustomPoll "Otro" free-text answer.
    private const int FreeTextMaxLength = 280;

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
            Selection = BuildSelectionState(pendingEntry, openEntry, group, userId),
            MyStreak = await streakService.GetMyStreakAsync(groupId, userId),
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

        var hasVoted = await context.Votes.AnyAsync(v =>
            v.QuestionId == openEntry.QuestionId && v.UserId == userId
        );

        var state = new TodayStateDto
        {
            Question = mapper.Map<QuestionToVoteDto>(openEntry.Question),
            UserHasVoted = hasVoted,
            // The open question activated at Date@T and closes 24 h later, at (Date+1)@T.
            ClosesAt = DailyClock.ToUtc(openEntry.Date.AddDays(1), group.DailyQuestionTime),
        };

        if (hasVoted)
        {
            state.Status = "results";
            state.Results = await CalculateResultsAsync(openEntry.Question);
        }
        else
        {
            state.Status = "voting";
        }

        return state;
    }

    private SelectionStateDto? BuildSelectionState(
        DailyEntry? pendingEntry,
        DailyEntry? openEntry,
        Group group,
        int userId
    )
    {
        if (pendingEntry != null)
        {
            var isSelector = pendingEntry.SelectorUserId == userId;
            var selectorMember = group.Members.FirstOrDefault(m =>
                m.UserId == pendingEntry.SelectorUserId
            );

            return new SelectionStateDto
            {
                Date = pendingEntry.Date,
                ActivatesAt = DailyClock.ToUtc(pendingEntry.Date, group.DailyQuestionTime),
                SelectorUserId = pendingEntry.SelectorUserId,
                SelectorUsername = selectorMember?.User.Username ?? string.Empty,
                IsCurrentUserSelector = isSelector,
                IsAutoSelected = pendingEntry.IsAutoSelected,
                // Only the selector sees tomorrow's question — keeps the surprise.
                PendingQuestion = isSelector
                    ? mapper.Map<QuestionToVoteDto>(pendingEntry.Question)
                    : null,
            };
        }

        // No preselection yet. Show whose turn it is only once the group has started
        // a cycle (≥ 2 members); otherwise there is no selection state (pre-start).
        if (group.Members.Count < 2)
            return null;

        var upcoming = NextActivationDate(openEntry, group);
        var selector = CalculateSelector(group.Members, group.DateCreated, upcoming);

        return new SelectionStateDto
        {
            Date = upcoming,
            ActivatesAt = DailyClock.ToUtc(upcoming, group.DailyQuestionTime),
            SelectorUserId = selector.UserId,
            SelectorUsername = selector.User.Username,
            IsCurrentUserSelector = selector.UserId == userId,
            IsAutoSelected = false,
            PendingQuestion = null,
        };
    }

    // The next date that will activate, when no pending entry exists yet.
    private static DateOnly NextActivationDate(DailyEntry? openEntry, Group group)
    {
        if (openEntry != null)
            return openEntry.Date.AddDays(1);

        // Brand-new group: the first question activates today if T hasn't passed, else tomorrow.
        var today = DailyClock.Today();
        return DailyClock.TimeOfDay() < group.DailyQuestionTime ? today : today.AddDays(1);
    }

    // ==========================================
    // SELECT QUESTION (§4.1, §4.2)
    // The selector chooses the NEXT-day question. It is NEVER activated here — activation
    // happens only at T in DailyPreselectionService.
    // ==========================================
    public async Task<SelectResult> SelectQuestionAsync(
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

        DateOnly targetDate;
        if (pendingEntry != null)
        {
            targetDate = pendingEntry.Date;
        }
        else
        {
            var openEntry = await context
                .DailyEntries.Where(d => d.GroupId == groupId && d.ActivatedAt != null)
                .OrderByDescending(d => d.Date)
                .FirstOrDefaultAsync();
            targetDate = NextActivationDate(openEntry, group);
        }

        // Only the selector for the target date may choose.
        // If a pending entry already exists, use its stored SelectorUserId — recalculating
        // would break for members who left and rejoined (new JoinedAt shifts the rotation).
        var selectorUserId = pendingEntry?.SelectorUserId
            ?? CalculateSelector(group.Members, group.DateCreated, targetDate).UserId;
        if (selectorUserId != userId)
            return SelectResult.NotYourTurn;

        var (question, resolveResult) = await ResolveSelectedQuestionAsync(dto, group, userId, targetDate);
        if (resolveResult != SelectResult.Success)
            return resolveResult;

        if (pendingEntry == null)
        {
            context.DailyEntries.Add(
                new DailyEntry
                {
                    GroupId = groupId,
                    Date = targetDate,
                    SelectorUserId = userId,
                    QuestionId = question!.Id,
                    ActivatedAt = null, // NEVER activate here — only the background service at T.
                    IsAutoSelected = false,
                    PreselectedAt = DateTime.UtcNow,
                }
            );
        }
        else
        {
            var oldQuestionId = pendingEntry.QuestionId;
            pendingEntry.QuestionId = question!.Id;
            pendingEntry.IsAutoSelected = false;

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
        }

        await context.SaveChangesAsync();
        return SelectResult.Success;
    }

    // Resolves the chosen question (pool / pack template / inline-create / overwrite) WITHOUT activating it.
    private async Task<(Question? question, SelectResult result)> ResolveSelectedQuestionAsync(
        SelectQuestionDto dto,
        Group group,
        int userId,
        DateOnly targetDate
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

            existing.Text = dto.NewQuestion.Text;
            existing.Type = dto.NewQuestion.Type;
            existing.Metadata = QuestionMetadataBuilder.Build(dto.NewQuestion);
            existing.Options.Clear();
            if (dto.NewQuestion.Type == QuestionType.CustomPoll && dto.NewQuestion.Options.Count > 0)
                existing.Options = dto.NewQuestion.Options.Select(o => new Option { Text = o.Text }).ToList();

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
    // plus the voter's streak change (rama 19).
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

        if (ValidateVotePayload(dto, question, memberIds) != null)
            return (VoteResult.InvalidPayload, null);

        context.Votes.AddRange(BuildVotes(dto, question, userId));
        await context.SaveChangesAsync();

        var streak = await streakService.RegisterVoteAsync(groupId, userId, openEntry);

        var group = await context.Groups.FindAsync(groupId);
        var voter = await context.Users.FindAsync(userId);
        if (group != null && voter != null)
            _ = notificationService.SendUserVotedAsync(groupId, userId, group.Name, voter.Username);

        var results = await CalculateResultsAsync(question);
        return (VoteResult.Success, new VoteResponseDto { Results = results, Streak = streak });
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

        var selector = CalculateSelector(group.Members, group.DateCreated, date);

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
        if (activateImmediately)
            question.DateActivated = DateTime.UtcNow;

        context.DailyEntries.Add(
            new DailyEntry
            {
                GroupId = groupId,
                Date = date,
                QuestionId = question.Id,
                SelectorUserId = selector.UserId,
                IsAutoSelected = true,
                PreselectedAt = DateTime.UtcNow,
                ActivatedAt = activateImmediately ? DateTime.UtcNow : null,
            }
        );

        await context.SaveChangesAsync();
        return selector.UserId;
    }

    // ==========================================
    // PRIVATE HELPERS
    // ==========================================

    private static GroupMember CalculateSelector(
        List<GroupMember> members,
        DateTime groupCreatedAt,
        DateOnly date
    )
    {
        var daysSinceCreation = date.DayNumber - DateOnly.FromDateTime(groupCreatedAt).DayNumber;
        var ordered = members.OrderBy(m => m.JoinedAt).ToList();
        // Safe modulo: the bootstrap entry (see JoinGroupAsync) can land one day before the
        // group's creation date, making daysSinceCreation negative.
        var index = ((daysSinceCreation % ordered.Count) + ordered.Count) % ordered.Count;
        return ordered[index];
    }

    // Total CustomPoll selections = picked options + an optional "Otro" free-text answer,
    // which must land within [MinSelections, MaxSelections] and be at least 1.
    private static bool IsPollSelectionCountValid(CreateVoteDto dto, Question question)
    {
        var optionCount = dto.SelectedOptionIds?.Count ?? 0;
        var hasFreeText =
            question.Metadata.AllowOther && !string.IsNullOrWhiteSpace(dto.FreeText);
        var total = optionCount + (hasFreeText ? 1 : 0);

        return total >= 1
            && total >= question.Metadata.MinSelections
            && total <= question.Metadata.MaxSelections;
    }

    private static string? ValidateVotePayload(
        CreateVoteDto dto,
        Question question,
        ISet<int> memberIds
    )
    {
        return question.Type switch
        {
            QuestionType.CustomPoll
                when !question.Metadata.AllowOther && !string.IsNullOrWhiteSpace(dto.FreeText) =>
                "This poll does not allow a free-text answer.",

            QuestionType.CustomPoll when (dto.FreeText?.Length ?? 0) > FreeTextMaxLength =>
                "Free-text answer is too long.",

            QuestionType.CustomPoll
                when (dto.SelectedOptionIds?.Any(id => !question.Options.Any(o => o.Id == id))
                    ?? false) => "One or more selected options are invalid.",

            QuestionType.CustomPoll
                when dto.SelectedOptionIds != null
                    && dto.SelectedOptionIds.Distinct().Count() != dto.SelectedOptionIds.Count =>
                "Selected options must be unique.",

            QuestionType.CustomPoll when !IsPollSelectionCountValid(dto, question) =>
                "Number of selected options is out of the allowed range.",

            QuestionType.OpenText when string.IsNullOrWhiteSpace(dto.FreeText) =>
                "Must provide a free-text answer.",

            QuestionType.OpenText when dto.FreeText!.Length > FreeTextMaxLength =>
                "Free-text answer is too long.",

            QuestionType.Superlative when dto.SelectedTargetUserId == null =>
                "Must select a target user.",

            QuestionType.Superlative
                when dto.SelectedTargetUserId == 0 && !question.Metadata.AllowNobody =>
                "\"Nobody\" is not allowed for this question.",

            QuestionType.Superlative
                when dto.SelectedTargetUserId != 0
                    && question.Metadata.BlacklistedUserIds.Contains(
                        dto.SelectedTargetUserId!.Value
                    ) => "Selected user is blacklisted.",

            QuestionType.Superlative
                when dto.SelectedTargetUserId != 0
                    && !memberIds.Contains(dto.SelectedTargetUserId!.Value) =>
                "Selected user is not a group member.",

            QuestionType.Scale when dto.NumericValue == null => "Must provide a numeric value.",

            QuestionType.Scale
                when dto.NumericValue < (question.Metadata.RangeMin ?? 1)
                    || dto.NumericValue > (question.Metadata.RangeMax ?? 10) =>
                "Numeric value out of range.",

            QuestionType.SecretPairing
                when dto.SelectedTargetUserIds == null
                    || dto.SelectedTargetUserIds.Distinct().Count() != 2 =>
                "Must select exactly 2 distinct users.",

            QuestionType.SecretPairing when !dto.SelectedTargetUserIds!.All(memberIds.Contains) =>
                "Selected users must be group members.",

            QuestionType.Deathmatch
                when dto.SelectedTargetUserIds == null
                    || !question.Metadata.Teams.Any(t =>
                        t.OrderBy(id => id)
                            .SequenceEqual(dto.SelectedTargetUserIds.OrderBy(id => id))
                    ) => "Must select a valid team.",

            _ => null, // valid
        };
    }

    private static List<Vote> BuildVotes(CreateVoteDto dto, Question question, int userId)
    {
        var votes = new List<Vote>();

        switch (question.Type)
        {
            case QuestionType.CustomPoll:
                foreach (var optionId in dto.SelectedOptionIds ?? [])
                    votes.Add(
                        new Vote
                        {
                            UserId = userId,
                            QuestionId = question.Id,
                            SelectedOptionId = optionId,
                        }
                    );
                // An "Otro" answer rides along as an extra row carrying only FreeText.
                if (question.Metadata.AllowOther && !string.IsNullOrWhiteSpace(dto.FreeText))
                    votes.Add(
                        new Vote
                        {
                            UserId = userId,
                            QuestionId = question.Id,
                            FreeText = dto.FreeText!.Trim(),
                        }
                    );
                break;

            case QuestionType.OpenText:
                votes.Add(
                    new Vote
                    {
                        UserId = userId,
                        QuestionId = question.Id,
                        FreeText = dto.FreeText!.Trim(),
                    }
                );
                break;

            case QuestionType.Superlative:
                votes.Add(
                    new Vote
                    {
                        UserId = userId,
                        QuestionId = question.Id,
                        SelectedTargetUserId =
                            dto.SelectedTargetUserId == 0
                                ? null // Nobody
                                : dto.SelectedTargetUserId,
                    }
                );
                break;

            case QuestionType.Scale:
                votes.Add(
                    new Vote
                    {
                        UserId = userId,
                        QuestionId = question.Id,
                        NumericValue = dto.NumericValue,
                    }
                );
                break;

            case QuestionType.SecretPairing:
                foreach (var targetId in dto.SelectedTargetUserIds!)
                    votes.Add(
                        new Vote
                        {
                            UserId = userId,
                            QuestionId = question.Id,
                            SelectedTargetUserId = targetId,
                        }
                    );
                break;

            case QuestionType.Deathmatch:
                var matchedTeamLeader = question
                    .Metadata.Teams.FirstOrDefault(t =>
                        t.OrderBy(id => id)
                            .SequenceEqual((dto.SelectedTargetUserIds ?? []).OrderBy(id => id))
                    )
                    ?.First();

                if (matchedTeamLeader.HasValue)
                    votes.Add(
                        new Vote
                        {
                            UserId = userId,
                            QuestionId = question.Id,
                            SelectedTargetUserId = matchedTeamLeader,
                        }
                    );
                break;
        }

        return votes;
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
