using AutoMapper;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Services;

// Flash questions (§17.11, rama 44). A flash is its own question, launched the moment it is
// bought and open for 24 h. Several can be open at once (one per member and group per local
// day); none of them touches the daily lifecycle, streaks, coins or the selector rotation.
public class FlashService(
    AppDbContext context,
    IMapper mapper,
    ICoinService coinService,
    IInventoryService inventoryService,
    INotificationService notificationService,
    TimeProvider clock
) : IFlashService
{
    public static readonly TimeSpan OpenFor = TimeSpan.FromHours(24);

    // Extension (rama 50): +24 h, once per flash, by any member while it's open.
    public static readonly TimeSpan ExtendBy = TimeSpan.FromHours(24);

    // Time capsule (rama 50): the buyer picks the opening day, 1 day to 1 year after launch.
    public const int CapsuleMinDays = 1;
    public const int CapsuleMaxDays = 365;

    // ==========================================
    // LAUNCH
    // Same validation as POST /questions (§9). The question, the flash and the charge commit
    // together, so a failure at any step refunds the coins.
    // ==========================================
    public async Task<(FlashResult Result, LaunchFlashResponseDto? Response)> LaunchAsync(
        int groupId,
        int userId,
        CreateQuestionDto dto,
        bool useOwned = false,
        FlashOptions? options = null
    )
    {
        options ??= new FlashOptions();
        var group =
            await context.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId)
            ?? throw new KeyNotFoundException("Group not found.");

        // Nobody to answer it until the group has started (§4.3).
        if (group.Members.Count < 2)
            return (FlashResult.GroupNotStarted, null);

        var memberIds = group.Members.Select(m => m.UserId).ToHashSet();
        if (QuestionMembershipValidator.Validate(dto, memberIds, userId) != null)
            return (FlashResult.InvalidQuestion, null);

        var nowUtc = DailyClock.UtcNow(clock);
        var today = DailyClock.Today(clock);

        // The capsule opens on the chosen local day, at the same time of day it was launched.
        DateTime? hiddenUntil = null;
        if (options.CapsuleOpensOn is DateOnly opensOn)
        {
            var days = opensOn.DayNumber - today.DayNumber;
            if (days < CapsuleMinDays || days > CapsuleMaxDays)
                return (FlashResult.InvalidCapsuleDate, null);
            hiddenUntil = nowUtc.AddDays(days);
        }

        // The add-ons are always paid in coins, so check the whole bill before charging.
        var addOns = new List<(CoinReason Reason, int Price)>();
        if (options.Anonymous)
            addOns.Add((CoinReason.FlashAnonymous, CoinRewards.FlashAnonymousPrice));
        if (hiddenUntil != null)
            addOns.Add((CoinReason.FlashCapsule, CoinRewards.FlashCapsulePrice));
        var coinsNeeded = addOns.Sum(a => a.Price) + (useOwned ? 0 : CoinRewards.FlashQuestionPrice);
        if (await coinService.GetBalanceAsync(userId) < coinsNeeded)
            return (FlashResult.NotEnoughCoins, null);

        if (
            await context.FlashEntries.AnyAsync(f =>
                f.GroupId == groupId && f.LaunchedByUserId == userId && f.LocalDate == today
            )
        )
            return (FlashResult.DailyLimitReached, null);

        await using var transaction = await context.Database.BeginTransactionAsync();

        var question = new Question
        {
            Text = dto.Text,
            Type = dto.Type,
            CreatorId = userId,
            GroupId = groupId,
            // Used from birth: it never enters the pool or the daily draw.
            IsUsed = true,
            Source = QuestionSource.UserCreated,
            DateActivated = nowUtc,
            Metadata = QuestionMetadataBuilder.Build(dto),
        };
        if (dto.Type == QuestionType.CustomPoll && dto.Options.Count > 0)
            question.Options = dto.Options.Select(o => new Option { Text = o.Text }).ToList();

        var flash = new FlashEntry
        {
            GroupId = groupId,
            Question = question,
            LaunchedByUserId = userId,
            ActivatedAt = nowUtc,
            ClosesAt = nowUtc + OpenFor,
            LocalDate = today,
            IsAnonymous = options.Anonymous,
            ResultsHiddenUntil = hiddenUntil,
        };
        context.FlashEntries.Add(flash);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // A simultaneous launch by the same member got the day's slot first.
            return (FlashResult.DailyLimitReached, null);
        }

        // From the inventory (rama 45) or paid in place. Disposing the transaction rolls back.
        if (useOwned)
        {
            if (!await inventoryService.TryConsumeAsync(userId, InventoryItem.FlashQuestion))
                return (FlashResult.NoneOwned, null);
        }
        else if (
            !await coinService.SpendAsync(
                userId,
                CoinReason.FlashQuestion,
                CoinRewards.FlashQuestionPrice,
                groupId,
                flash.Id
            )
        )
            return (FlashResult.NotEnoughCoins, null);

        // One ledger row per add-on, so "Movimientos" names each charge.
        foreach (var (reason, price) in addOns)
            if (!await coinService.SpendAsync(userId, reason, price, groupId, flash.Id))
                return (FlashResult.NotEnoughCoins, null);

        await transaction.CommitAsync();

        var launcher = await context.Users.FindAsync(userId);
        var launcherName = launcher?.Username ?? string.Empty;
        // A 1-vs-1 flash Deathmatch is a duel (§17.15 item 6): its sides get the duel push.
        var duelists = DuelRules.Duelists(question);
        _ = notificationService.SendFlashQuestionAsync(
            groupId,
            userId,
            group.Name,
            launcherName,
            question.Text,
            duelists is (int first, int second)
                ? new DuelPush(userId, launcherName, first, second, Anonymous: flash.IsAnonymous)
                : null,
            flash.IsAnonymous
        );

        return (
            FlashResult.Success,
            new LaunchFlashResponseDto
            {
                Flash = (await BuildStatesAsync([flash.Id], userId)).Single(),
                Balance = await coinService.GetBalanceAsync(userId),
                Inventory = await inventoryService.GetAsync(userId),
            }
        );
    }

    // ==========================================
    // VOTE
    // On time while open; afterwards it is a late vote from the history (§17.7). A flash vote
    // never earns streak or coins (§17.2) and sends no push.
    // ==========================================
    public async Task<(FlashResult Result, FlashStateDto? State)> VoteAsync(
        int groupId,
        int userId,
        int flashId,
        CreateVoteDto dto
    )
    {
        var flash = await context
            .FlashEntries.Include(f => f.Question)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(f => f.Id == flashId && f.GroupId == groupId);
        if (flash == null)
            return (FlashResult.NotFound, null);

        var alreadyVoted = await context.Votes.AnyAsync(v =>
            v.QuestionId == flash.QuestionId && v.UserId == userId
        );
        if (alreadyVoted)
            return (FlashResult.AlreadyVoted, null);

        var memberIds = await context
            .GroupMembers.Where(m => m.GroupId == groupId)
            .Select(m => m.UserId)
            .ToHashSetAsync();
        if (VotePayload.Validate(dto, flash.Question, memberIds) != null)
            return (FlashResult.InvalidVote, null);

        var isOpen = flash.ClosesAt > DailyClock.UtcNow(clock);
        var votes = VotePayload.Build(dto, flash.Question, userId);
        votes.ForEach(v => v.IsLate = !isOpen);
        context.Votes.AddRange(votes);
        await context.SaveChangesAsync();

        return (FlashResult.Success, isOpen ? (await BuildStatesAsync([flash.Id], userId)).Single() : null);
    }

    // ==========================================
    // EXTEND (rama 50)
    // +24 h on an open flash, once, by any member. The conditional update makes the first
    // buyer win a race; the charge commits with it, so a failure refunds.
    // ==========================================
    public async Task<(FlashResult Result, ExtendFlashResponseDto? Response)> ExtendAsync(
        int groupId,
        int userId,
        int flashId
    )
    {
        var flash = await context.FlashEntries.AsNoTracking().FirstOrDefaultAsync(f => f.Id == flashId && f.GroupId == groupId);
        if (flash == null)
            return (FlashResult.NotFound, null);

        var nowUtc = DailyClock.UtcNow(clock);
        if (flash.ClosesAt <= nowUtc)
            return (FlashResult.Closed, null);
        if (flash.ExtendedByUserId != null)
            return (FlashResult.AlreadyExtended, null);

        await using var transaction = await context.Database.BeginTransactionAsync();

        var extended = await context
            .FlashEntries.Where(f => f.Id == flashId && f.ExtendedByUserId == null && f.ClosesAt > nowUtc)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(f => f.ExtendedByUserId, userId).SetProperty(f => f.ClosesAt, f => f.ClosesAt + ExtendBy)
            );
        if (extended == 0)
            return (FlashResult.AlreadyExtended, null);

        if (
            !await coinService.SpendAsync(
                userId,
                CoinReason.FlashExtension,
                CoinRewards.FlashExtensionPrice,
                groupId,
                flashId
            )
        )
            return (FlashResult.NotEnoughCoins, null);

        await transaction.CommitAsync();

        return (
            FlashResult.Success,
            new ExtendFlashResponseDto
            {
                Flash = (await BuildStatesAsync([flashId], userId)).Single(),
                Balance = await coinService.GetBalanceAsync(userId),
            }
        );
    }

    // ==========================================
    // TIME CAPSULES (rama 50)
    // Run by the minute tick: every capsule whose day has come gets its push, once.
    // ==========================================
    public async Task OpenDueCapsulesAsync()
    {
        var nowUtc = DailyClock.UtcNow(clock);
        var due = await context
            .FlashEntries.Include(f => f.Group)
            .Include(f => f.Question)
            .Include(f => f.LaunchedBy)
            .Where(f => f.ResultsHiddenUntil != null && f.CapsuleOpenedAt == null && f.ResultsHiddenUntil <= nowUtc)
            .OrderBy(f => f.ResultsHiddenUntil)
            .Take(50)
            .ToListAsync();
        if (due.Count == 0)
            return;

        foreach (var flash in due)
            flash.CapsuleOpenedAt = nowUtc;
        await context.SaveChangesAsync();

        foreach (var flash in due)
            _ = notificationService.SendCapsuleOpenedAsync(
                flash.GroupId,
                flash.Id,
                flash.Group.Name,
                flash.IsAnonymous ? null : flash.LaunchedBy.Username,
                flash.LocalDate,
                flash.Question.Text
            );
    }

    // ==========================================
    // OPEN FLASHES (daily/current)
    // ==========================================
    public async Task<List<FlashStateDto>> GetOpenAsync(int groupId, int userId)
    {
        var nowUtc = DailyClock.UtcNow(clock);
        var openIds = await context
            .FlashEntries.Where(f => f.GroupId == groupId && f.ClosesAt > nowUtc)
            .Select(f => f.Id)
            .ToListAsync();
        return openIds.Count == 0 ? [] : await BuildStatesAsync(openIds, userId);
    }

    public async Task<FlashStateDto> GetStateAsync(int flashId, int userId) =>
        (await BuildStatesAsync([flashId], userId)).Single();

    // ==========================================
    // HISTORY DETAIL (§17.7 gating)
    // Without your vote: the question and the vote count, not the results. Once closed it
    // also carries the question to vote late; while open, it is voted from the group screen.
    // ==========================================
    public async Task<QuestionResultDto?> GetDetailAsync(int groupId, int userId, int flashId)
    {
        var flash = await context
            .FlashEntries.Include(f => f.LaunchedBy)
            .Include(f => f.Question)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(f => f.Id == flashId && f.GroupId == groupId);
        if (flash == null)
            return null;

        var nowUtc = DailyClock.UtcNow(clock);
        var result = await CalculateResultsAsync(flash.Question);
        result.Author = ToVoter(flash.LaunchedBy);
        if (flash.IsAnonymous)
            ResultsBuilder.Anonymize(result, userId);
        result.CapsuleOpensAt = flash.ResultsHiddenUntil;

        // A sealed capsule shows only its vote count, to everyone (its voters too).
        if (flash.IsSealed(nowUtc))
        {
            result.Sealed = true;
            result.Results = [];
            result.FreeTextResponses = [];
        }

        var voted = await context.Votes.AnyAsync(v => v.QuestionId == flash.QuestionId && v.UserId == userId);
        if (!voted)
        {
            result.Locked = true;
            result.Results = [];
            result.FreeTextResponses = [];
            if (flash.ClosesAt <= nowUtc)
                result.LateVoteQuestion = mapper.Map<QuestionToVoteDto>(flash.Question);
        }

        return result;
    }

    // ==========================================
    // PRIVATE HELPERS
    // ==========================================
    private async Task<List<FlashStateDto>> BuildStatesAsync(List<int> flashIds, int userId)
    {
        var flashes = await context
            .FlashEntries.Include(f => f.LaunchedBy)
            .Include(f => f.ExtendedBy)
            .Include(f => f.Question)
                .ThenInclude(q => q.Options)
            .Where(f => flashIds.Contains(f.Id))
            .OrderBy(f => f.ActivatedAt)
            .ToListAsync();

        var questionIds = flashes.Select(f => f.QuestionId).ToList();
        var voters = await context
            .Votes.Where(v => questionIds.Contains(v.QuestionId))
            .Select(v => new { v.QuestionId, v.UserId })
            .Distinct()
            .ToListAsync();

        var changedQuestionIds = await context
            .VoteChanges.Where(c => c.UserId == userId && questionIds.Contains(c.QuestionId))
            .Select(c => c.QuestionId)
            .ToListAsync();

        var nowUtc = DailyClock.UtcNow(clock);
        var states = new List<FlashStateDto>();
        foreach (var flash in flashes)
        {
            var voterIds = voters.Where(v => v.QuestionId == flash.QuestionId).Select(v => v.UserId).ToList();
            var hasVoted = voterIds.Contains(userId);
            var isSealed = flash.IsSealed(nowUtc);

            QuestionResultDto? results = null;
            if (hasVoted && !isSealed)
            {
                results = await CalculateResultsAsync(flash.Question);
                if (flash.IsAnonymous)
                    ResultsBuilder.Anonymize(results, userId);
            }

            states.Add(
                new FlashStateDto
                {
                    Id = flash.Id,
                    Question = mapper.Map<QuestionToVoteDto>(flash.Question),
                    // An anonymous flash names nobody: not who launched it, extended it or voted.
                    LaunchedBy = flash.IsAnonymous ? null : ToVoter(flash.LaunchedBy),
                    ActivatedAt = flash.ActivatedAt,
                    ClosesAt = flash.ClosesAt,
                    UserHasVoted = hasVoted,
                    Results = results,
                    VoteCount = voterIds.Count,
                    VoterIds = flash.IsAnonymous ? [] : voterIds,
                    CanChangeVote = hasVoted && !changedQuestionIds.Contains(flash.QuestionId),
                    IsAnonymous = flash.IsAnonymous,
                    CapsuleOpensAt = flash.ResultsHiddenUntil,
                    Sealed = isSealed,
                    Extended = flash.ExtendedByUserId != null,
                    ExtendedBy = flash.IsAnonymous || flash.ExtendedBy == null ? null : ToVoter(flash.ExtendedBy),
                }
            );
        }
        return states;
    }

    private async Task<QuestionResultDto> CalculateResultsAsync(Question question)
    {
        var votes = await context
            .Votes.Include(v => v.User)
            .Include(v => v.SelectedTargetUser)
            .Where(v => v.QuestionId == question.Id)
            .ToListAsync();
        return await ResultsBuilder.BuildAsync(context, question, votes, mapper);
    }

    private static VoterDto ToVoter(User user) =>
        new()
        {
            Id = user.Id,
            Username = user.Username,
            AvatarUrl = user.AvatarUrl,
            FrameColor = user.FrameColor,
        };
}
