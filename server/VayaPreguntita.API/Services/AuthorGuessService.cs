using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Coins;
using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Services;

// The guess (§17.6, §17.17): after voting, a member may guess who wrote the question, who chose
// it, or who launched a flash — once, while it is open. The answer is revealed at once; a right
// guess pays 5 🪙 (never capped). The hint (20 🪙, §17.10) hides half of the wrong candidates.
// The daily guess lives in AuthorGuess, the flash one in FlashGuess; the rules are the same.
public class AuthorGuessService(
    AppDbContext context,
    ICoinService coinService,
    IInventoryService inventoryService,
    TimeProvider clock
)
    : IAuthorGuessService
{
    // What one guess is about: the open daily entry or a flash.
    private sealed record Subject(GuessTarget Target, int RefId, bool IsFlash, int GroupId)
    {
        public CoinReason RewardReason => IsFlash ? CoinReason.FlashGuessCorrect : CoinReason.GuessCorrect;
        public CoinReason HintReason => IsFlash ? CoinReason.FlashHint : CoinReason.AuthorHint;

        // Seeds the hint's shuffle; flashes are offset so a flash and a daily entry with the
        // same id don't hide the same members.
        public int HintSeed => IsFlash ? -RefId : RefId;
    }

    public async Task<AuthorGuessDto?> GetStateAsync(DailyEntry entry, int userId)
    {
        var target = AuthorGuessRules.TargetOf(entry);
        var members = await LoadMembersAsync(entry.GroupId);
        if (!AuthorGuessRules.IsGuessable(target, members.Keys))
            return null;
        return await GetStateAsync(new Subject(target!, entry.Id, false, entry.GroupId), userId, members);
    }

    public async Task<AuthorGuessDto?> GetFlashStateAsync(FlashEntry flash, int userId, IReadOnlyCollection<int> memberIds)
    {
        var target = AuthorGuessRules.TargetOf(flash);
        if (!AuthorGuessRules.IsGuessable(target, memberIds))
            return null;
        var members = await LoadMembersAsync(flash.GroupId);
        return await GetStateAsync(new Subject(target!, flash.Id, true, flash.GroupId), userId, members);
    }

    public async Task<(GuessResult result, GuessAuthorResponseDto? response)> GuessAsync(
        int groupId,
        int userId,
        GuessAuthorRequestDto dto
    ) => await GuessAsync(await LoadDailyAsync(groupId, userId), userId, dto);

    public async Task<(GuessResult result, GuessAuthorResponseDto? response)> GuessFlashAsync(
        int groupId,
        int userId,
        int flashId,
        GuessAuthorRequestDto dto
    ) => await GuessAsync(await LoadFlashAsync(groupId, userId, flashId), userId, dto);

    public async Task<(GuessResult result, AuthorHintResponseDto? response)> BuyHintAsync(
        int groupId,
        int userId,
        bool useOwned = false
    ) => await BuyHintAsync(await LoadDailyAsync(groupId, userId), userId, useOwned);

    public async Task<(GuessResult result, AuthorHintResponseDto? response)> BuyFlashHintAsync(
        int groupId,
        int userId,
        int flashId,
        bool useOwned = false
    ) => await BuyHintAsync(await LoadFlashAsync(groupId, userId, flashId), userId, useOwned);

    // ── The game, for either subject ─────────────────────────────────────────

    private async Task<AuthorGuessDto> GetStateAsync(Subject subject, int userId, Dictionary<int, VoterDto> members)
    {
        var target = members[subject.Target.UserId];
        if (subject.Target.UserId == userId)
        {
            var guesses = subject.IsFlash
                ? await context
                    .FlashGuesses.Where(g =>
                        g.FlashEntryId == subject.RefId && g.GuessedAt != null && g.GuessedUserId != null
                    )
                    .Select(g => g.IsCorrect)
                    .ToListAsync()
                : await context
                    .AuthorGuesses.Where(g =>
                        g.DailyEntryId == subject.RefId && g.GuessedAt != null && g.GuessedUserId != null
                    )
                    .Select(g => g.IsCorrect)
                    .ToListAsync();
            return new AuthorGuessDto
            {
                Kind = subject.Target.KindKey,
                State = AuthorGuessDto.Mine,
                Author = target,
                GuessCount = guesses.Count,
                CaughtCount = guesses.Count(correct => correct),
            };
        }

        return BuildState(subject, userId, target, members.Keys, await FindRowAsync(subject, userId));
    }

    private async Task<(GuessResult result, GuessAuthorResponseDto? response)> GuessAsync(
        (GuessResult check, Subject? subject, Dictionary<int, VoterDto>? members) load,
        int userId,
        GuessAuthorRequestDto dto
    )
    {
        var (check, subject, members) = load;
        if (check != GuessResult.Success)
            return (check, null);

        var targetId = subject!.Target.UserId;
        var row = await FindRowAsync(subject, userId);
        if (row?.GuessedAt != null)
            return (GuessResult.AlreadyGuessed, null);

        int? guessedId = null;
        if (!dto.Skip)
        {
            if (dto.CreatorId is not int creatorId)
                return (GuessResult.InvalidCandidate, null);
            if (!VisibleCandidates(subject, userId, members!.Keys, row?.HintUsed == true).Contains(creatorId))
                return (GuessResult.InvalidCandidate, null);
            guessedId = creatorId;
        }

        var now = DailyClock.UtcNow(clock);
        var correct = guessedId == targetId;

        if (row == null)
        {
            row = NewRow(subject, userId);
            row.GuessedUserId = guessedId;
            row.IsCorrect = correct;
            row.GuessedAt = now;
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // A concurrent request (another guess, or the hint) created the row first.
                return (GuessResult.AlreadyGuessed, null);
            }
        }
        else
        {
            // Conditional write: of two simultaneous guesses, only one lands.
            var updated = subject.IsFlash
                ? await context
                    .FlashGuesses.Where(g => g.FlashEntryId == subject.RefId && g.UserId == userId && g.GuessedAt == null)
                    .ExecuteUpdateAsync(s =>
                        s.SetProperty(g => g.GuessedUserId, guessedId)
                            .SetProperty(g => g.IsCorrect, correct)
                            .SetProperty(g => g.GuessedAt, now)
                    )
                : await context
                    .AuthorGuesses.Where(g => g.DailyEntryId == subject.RefId && g.UserId == userId && g.GuessedAt == null)
                    .ExecuteUpdateAsync(s =>
                        s.SetProperty(g => g.GuessedUserId, guessedId)
                            .SetProperty(g => g.IsCorrect, correct)
                            .SetProperty(g => g.GuessedAt, now)
                    );
            if (updated == 0)
                return (GuessResult.AlreadyGuessed, null);
            await context.Entry(row).ReloadAsync();
        }

        var coins = correct
            ? await coinService.GrantAsync(userId, subject.RewardReason, CoinRewards.GuessCorrect, subject.GroupId, subject.RefId)
            : new CoinGrant(0, false);

        return (
            GuessResult.Success,
            new GuessAuthorResponseDto
            {
                Guess = BuildState(subject, userId, members![targetId], members.Keys, row),
                Coins = new CoinsEarnedDto
                {
                    Earned = coins.Granted,
                    CapReached = coins.CapReached,
                    Balance = await coinService.GetBalanceAsync(userId),
                },
            }
        );
    }

    private async Task<(GuessResult result, AuthorHintResponseDto? response)> BuyHintAsync(
        (GuessResult check, Subject? subject, Dictionary<int, VoterDto>? members) load,
        int userId,
        bool useOwned
    )
    {
        var (check, subject, members) = load;
        if (check != GuessResult.Success)
            return (check, null);

        var row = await FindRowAsync(subject!, userId);
        if (row?.GuessedAt != null)
            return (GuessResult.AlreadyGuessed, null);

        if (row?.HintUsed != true)
        {
            var wrongCount = AuthorGuessRules.Candidates(members!.Keys, userId).Count - 1;
            if (AuthorGuessRules.HintRemovals(wrongCount) == 0)
                return (GuessResult.HintNotAvailable, null);

            try
            {
                await using var transaction = await context.Database.BeginTransactionAsync();
                if (useOwned)
                {
                    // An owned hint (rama 45) writes no ledger row, so the row itself guards a
                    // double tap: only the request that flips HintUsed spends the item.
                    if (row == null)
                    {
                        row = NewRow(subject!, userId);
                        row.HintUsed = true;
                        await context.SaveChangesAsync();
                    }
                    else if (await MarkHintUsedAsync(subject!, userId) == 0)
                        throw new DbUpdateException("Hint already used.");

                    if (!await inventoryService.TryConsumeAsync(userId, InventoryItem.AuthorHint))
                        return (GuessResult.NoneOwned, null);
                }
                else
                {
                    var paid = await coinService.SpendAsync(
                        userId,
                        subject!.HintReason,
                        CoinRewards.AuthorHintPrice,
                        subject.GroupId,
                        subject.RefId
                    );
                    if (!paid)
                        return (GuessResult.NotEnoughCoins, null);

                    row ??= NewRow(subject, userId);
                    row.HintUsed = true;
                    await context.SaveChangesAsync();
                }
                await transaction.CommitAsync();
            }
            catch (DbUpdateException)
            {
                // A concurrent purchase won (the ledger's unique (user, reason, ref) index, or
                // the guess row for an owned hint): nothing was charged twice, so just show the
                // hint it bought.
                context.ChangeTracker.Clear();
                row = await FindRowAsync(subject!, userId);
            }
        }

        return (
            GuessResult.Success,
            new AuthorHintResponseDto
            {
                Guess = BuildState(subject!, userId, members![subject!.Target.UserId], members.Keys, row),
                Balance = await coinService.GetBalanceAsync(userId),
                Inventory = await inventoryService.GetAsync(userId),
            }
        );
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    // The open entry, checked for: something to guess, the caller voted, and isn't its target.
    private async Task<(GuessResult, Subject?, Dictionary<int, VoterDto>?)> LoadDailyAsync(int groupId, int userId)
    {
        var entry = await context
            .DailyEntries.Include(d => d.Question)
            .Where(d => d.GroupId == groupId && d.ActivatedAt != null)
            .OrderByDescending(d => d.Date)
            .FirstOrDefaultAsync();
        if (entry == null)
            return (GuessResult.NoActiveQuestion, null, null);

        return await CheckAsync(
            AuthorGuessRules.TargetOf(entry),
            entry.Id,
            isFlash: false,
            groupId,
            entry.QuestionId,
            userId
        );
    }

    // An open flash, with the same checks.
    private async Task<(GuessResult, Subject?, Dictionary<int, VoterDto>?)> LoadFlashAsync(
        int groupId,
        int userId,
        int flashId
    )
    {
        var flash = await context.FlashEntries.FirstOrDefaultAsync(f => f.Id == flashId && f.GroupId == groupId);
        if (flash == null || flash.ClosesAt <= DailyClock.UtcNow(clock))
            return (GuessResult.NoActiveQuestion, null, null);

        return await CheckAsync(
            AuthorGuessRules.TargetOf(flash),
            flash.Id,
            isFlash: true,
            groupId,
            flash.QuestionId,
            userId
        );
    }

    private async Task<(GuessResult, Subject?, Dictionary<int, VoterDto>?)> CheckAsync(
        GuessTarget? target,
        int refId,
        bool isFlash,
        int groupId,
        int questionId,
        int userId
    )
    {
        var members = await LoadMembersAsync(groupId);
        if (!AuthorGuessRules.IsGuessable(target, members.Keys) || target!.UserId == userId)
            return (GuessResult.NotGuessable, null, null);

        var hasVoted = await context.Votes.AnyAsync(v => v.QuestionId == questionId && v.UserId == userId);
        if (!hasVoted)
            return (GuessResult.MustVoteFirst, null, null);

        return (GuessResult.Success, new Subject(target, refId, isFlash, groupId), members);
    }

    private async Task<GuessRow?> FindRowAsync(Subject subject, int userId) =>
        subject.IsFlash
            ? await context.FlashGuesses.FirstOrDefaultAsync(g => g.FlashEntryId == subject.RefId && g.UserId == userId)
            : await context.AuthorGuesses.FirstOrDefaultAsync(g => g.DailyEntryId == subject.RefId && g.UserId == userId);

    // A new tracked row for the subject (saved by the caller).
    private GuessRow NewRow(Subject subject, int userId)
    {
        if (subject.IsFlash)
        {
            var flashRow = new FlashGuess { FlashEntryId = subject.RefId, UserId = userId };
            context.FlashGuesses.Add(flashRow);
            return flashRow;
        }
        var row = new AuthorGuess { DailyEntryId = subject.RefId, UserId = userId };
        context.AuthorGuesses.Add(row);
        return row;
    }

    private async Task<int> MarkHintUsedAsync(Subject subject, int userId) =>
        subject.IsFlash
            ? await context
                .FlashGuesses.Where(g => g.FlashEntryId == subject.RefId && g.UserId == userId && !g.HintUsed)
                .ExecuteUpdateAsync(s => s.SetProperty(g => g.HintUsed, true))
            : await context
                .AuthorGuesses.Where(g => g.DailyEntryId == subject.RefId && g.UserId == userId && !g.HintUsed)
                .ExecuteUpdateAsync(s => s.SetProperty(g => g.HintUsed, true));

    private async Task<Dictionary<int, VoterDto>> LoadMembersAsync(int groupId) =>
        await context
            .GroupMembers.Where(m => m.GroupId == groupId)
            .Select(m => new VoterDto
            {
                Id = m.UserId,
                Username = m.User.Username,
                AvatarUrl = m.User.AvatarUrl,
                FrameColor = m.User.FrameColor,
            })
            .ToDictionaryAsync(m => m.Id);

    private static List<int> VisibleCandidates(
        Subject subject,
        int userId,
        IEnumerable<int> memberIds,
        bool hintUsed
    )
    {
        var candidates = AuthorGuessRules.Candidates(memberIds, userId);
        if (!hintUsed)
            return candidates;

        var hidden = AuthorGuessRules.HiddenByHint(
            subject.HintSeed,
            userId,
            candidates.Where(id => id != subject.Target.UserId)
        );
        return candidates.Where(id => !hidden.Contains(id)).ToList();
    }

    private static AuthorGuessDto BuildState(
        Subject subject,
        int userId,
        VoterDto target,
        IReadOnlyCollection<int> memberIds,
        GuessRow? row
    )
    {
        var kind = subject.Target.KindKey;
        if (row?.GuessedAt != null)
            return row.GuessedUserId is int guessed
                ? new AuthorGuessDto
                {
                    Kind = kind,
                    State = AuthorGuessDto.Guessed,
                    GuessedUserId = guessed,
                    Correct = row.IsCorrect,
                    Author = target,
                }
                : new AuthorGuessDto { Kind = kind, State = AuthorGuessDto.Skipped, Author = target };

        var hintUsed = row?.HintUsed == true;
        var wrongCount = AuthorGuessRules.Candidates(memberIds, userId).Count - 1;
        return new AuthorGuessDto
        {
            Kind = kind,
            State = AuthorGuessDto.Pending,
            CandidateIds = VisibleCandidates(subject, userId, memberIds, hintUsed),
            HintUsed = hintUsed,
            HintAvailable = !hintUsed && AuthorGuessRules.HintRemovals(wrongCount) > 0,
            HintPrice = CoinRewards.AuthorHintPrice,
        };
    }
}
