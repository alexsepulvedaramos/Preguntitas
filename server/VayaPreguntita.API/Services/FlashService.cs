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
    INotificationService notificationService,
    TimeProvider clock
) : IFlashService
{
    public static readonly TimeSpan OpenFor = TimeSpan.FromHours(24);

    // ==========================================
    // LAUNCH
    // Same validation as POST /questions (§9). The question, the flash and the charge commit
    // together, so a failure at any step refunds the coins.
    // ==========================================
    public async Task<(FlashResult Result, LaunchFlashResponseDto? Response)> LaunchAsync(
        int groupId,
        int userId,
        CreateQuestionDto dto
    )
    {
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

        if (
            !await coinService.SpendAsync(
                userId,
                CoinReason.FlashQuestion,
                CoinRewards.FlashQuestionPrice,
                groupId,
                flash.Id
            )
        )
            return (FlashResult.NotEnoughCoins, null); // disposing the transaction rolls back

        await transaction.CommitAsync();

        var launcher = await context.Users.FindAsync(userId);
        _ = notificationService.SendFlashQuestionAsync(
            groupId,
            userId,
            group.Name,
            launcher?.Username ?? string.Empty,
            question.Text
        );

        return (
            FlashResult.Success,
            new LaunchFlashResponseDto
            {
                Flash = (await BuildStatesAsync([flash.Id], userId)).Single(),
                Balance = await coinService.GetBalanceAsync(userId),
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

        var result = await CalculateResultsAsync(flash.Question);
        result.Author = ToVoter(flash.LaunchedBy);

        var voted = await context.Votes.AnyAsync(v => v.QuestionId == flash.QuestionId && v.UserId == userId);
        if (!voted)
        {
            result.Locked = true;
            result.Results = [];
            result.FreeTextResponses = [];
            if (flash.ClosesAt <= DailyClock.UtcNow(clock))
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

        var states = new List<FlashStateDto>();
        foreach (var flash in flashes)
        {
            var voterIds = voters.Where(v => v.QuestionId == flash.QuestionId).Select(v => v.UserId).ToList();
            var hasVoted = voterIds.Contains(userId);
            states.Add(
                new FlashStateDto
                {
                    Id = flash.Id,
                    Question = mapper.Map<QuestionToVoteDto>(flash.Question),
                    LaunchedBy = ToVoter(flash.LaunchedBy),
                    ActivatedAt = flash.ActivatedAt,
                    ClosesAt = flash.ClosesAt,
                    UserHasVoted = hasVoted,
                    Results = hasVoted ? await CalculateResultsAsync(flash.Question) : null,
                    VoterIds = voterIds,
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
        var usersById = await ResultsBuilder.BuildDeathmatchUsersById(context, question);
        return ResultsBuilder.Build(question, votes, mapper, usersById);
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
