using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Coins;
using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Services;

// "Guess the author" (§17.6): after voting, a member may guess who wrote a member-written
// question, once, while it is open. The answer is revealed at once; a correct guess pays
// 5 🪙 (capped). The hint (20 🪙, §17.10) hides half of the wrong candidates.
public class AuthorGuessService(
    AppDbContext context,
    ICoinService coinService,
    IInventoryService inventoryService,
    TimeProvider clock
)
    : IAuthorGuessService
{
    public async Task<AuthorGuessDto?> GetStateAsync(DailyEntry entry, int userId)
    {
        var members = await LoadMembersAsync(entry.GroupId);
        if (!AuthorGuessRules.IsGuessable(entry.Question, members.Keys))
            return null;

        var authorId = AuthorGuessRules.AuthorOf(entry.Question)!.Value;
        var author = members[authorId];

        if (authorId == userId)
        {
            var guesses = await context
                .AuthorGuesses.Where(g =>
                    g.DailyEntryId == entry.Id && g.GuessedAt != null && g.GuessedCreatorId != null
                )
                .Select(g => g.IsCorrect)
                .ToListAsync();
            return new AuthorGuessDto
            {
                State = AuthorGuessDto.Mine,
                Author = author,
                GuessCount = guesses.Count,
                CaughtCount = guesses.Count(correct => correct),
            };
        }

        var row = await context.AuthorGuesses.FirstOrDefaultAsync(g =>
            g.DailyEntryId == entry.Id && g.UserId == userId
        );
        return BuildState(entry, userId, authorId, author, members.Keys, row);
    }

    public async Task<(GuessResult result, GuessAuthorResponseDto? response)> GuessAsync(
        int groupId,
        int userId,
        GuessAuthorRequestDto dto
    )
    {
        var (check, entry, members) = await LoadGuessableAsync(groupId, userId);
        if (check != GuessResult.Success)
            return (check, null);

        var authorId = AuthorGuessRules.AuthorOf(entry!.Question)!.Value;
        var row = await context.AuthorGuesses.FirstOrDefaultAsync(g =>
            g.DailyEntryId == entry.Id && g.UserId == userId
        );
        if (row?.GuessedAt != null)
            return (GuessResult.AlreadyGuessed, null);

        int? guessedId = null;
        if (!dto.Skip)
        {
            if (dto.CreatorId is not int creatorId)
                return (GuessResult.InvalidCandidate, null);
            if (!VisibleCandidates(entry, userId, authorId, members!.Keys, row?.HintUsed == true).Contains(creatorId))
                return (GuessResult.InvalidCandidate, null);
            guessedId = creatorId;
        }

        var now = DailyClock.UtcNow(clock);
        var correct = guessedId == authorId;

        if (row == null)
        {
            row = new AuthorGuess
            {
                DailyEntryId = entry.Id,
                UserId = userId,
                GuessedCreatorId = guessedId,
                IsCorrect = correct,
                GuessedAt = now,
            };
            context.AuthorGuesses.Add(row);
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
            var updated = await context
                .AuthorGuesses.Where(g =>
                    g.DailyEntryId == entry.Id && g.UserId == userId && g.GuessedAt == null
                )
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(g => g.GuessedCreatorId, guessedId)
                        .SetProperty(g => g.IsCorrect, correct)
                        .SetProperty(g => g.GuessedAt, now)
                );
            if (updated == 0)
                return (GuessResult.AlreadyGuessed, null);
            await context.Entry(row).ReloadAsync();
        }

        var coins = correct
            ? await coinService.GrantAsync(userId, CoinReason.GuessCorrect, CoinRewards.GuessCorrect, groupId, entry.Id)
            : new CoinGrant(0, false);

        return (
            GuessResult.Success,
            new GuessAuthorResponseDto
            {
                Guess = BuildState(entry, userId, authorId, members![authorId], members.Keys, row),
                Coins = new CoinsEarnedDto
                {
                    Earned = coins.Granted,
                    CapReached = coins.CapReached,
                    Balance = await coinService.GetBalanceAsync(userId),
                },
            }
        );
    }

    public async Task<(GuessResult result, AuthorHintResponseDto? response)> BuyHintAsync(
        int groupId,
        int userId,
        bool useOwned = false
    )
    {
        var (check, entry, members) = await LoadGuessableAsync(groupId, userId);
        if (check != GuessResult.Success)
            return (check, null);

        var authorId = AuthorGuessRules.AuthorOf(entry!.Question)!.Value;
        var row = await context.AuthorGuesses.FirstOrDefaultAsync(g =>
            g.DailyEntryId == entry.Id && g.UserId == userId
        );
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
                        row = new AuthorGuess { DailyEntryId = entry.Id, UserId = userId, HintUsed = true };
                        context.AuthorGuesses.Add(row);
                        await context.SaveChangesAsync();
                    }
                    else if (
                        await context
                            .AuthorGuesses.Where(g =>
                                g.DailyEntryId == entry.Id && g.UserId == userId && !g.HintUsed
                            )
                            .ExecuteUpdateAsync(s => s.SetProperty(g => g.HintUsed, true)) == 0
                    )
                        throw new DbUpdateException("Hint already used.");

                    if (!await inventoryService.TryConsumeAsync(userId, InventoryItem.AuthorHint))
                        return (GuessResult.NoneOwned, null);
                }
                else
                {
                    var paid = await coinService.SpendAsync(
                        userId,
                        CoinReason.AuthorHint,
                        CoinRewards.AuthorHintPrice,
                        groupId,
                        entry.Id
                    );
                    if (!paid)
                        return (GuessResult.NotEnoughCoins, null);

                    if (row == null)
                    {
                        row = new AuthorGuess { DailyEntryId = entry.Id, UserId = userId, HintUsed = true };
                        context.AuthorGuesses.Add(row);
                    }
                    else
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
                row = await context.AuthorGuesses.FirstOrDefaultAsync(g =>
                    g.DailyEntryId == entry.Id && g.UserId == userId
                );
            }
        }

        return (
            GuessResult.Success,
            new AuthorHintResponseDto
            {
                Guess = BuildState(entry, userId, authorId, members![authorId], members.Keys, row),
                Balance = await coinService.GetBalanceAsync(userId),
                Inventory = await inventoryService.GetAsync(userId),
            }
        );
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    // The open entry, checked for: something to guess, the caller voted, and isn't the author.
    private async Task<(GuessResult, DailyEntry?, Dictionary<int, VoterDto>?)> LoadGuessableAsync(
        int groupId,
        int userId
    )
    {
        var entry = await context
            .DailyEntries.Include(d => d.Question)
            .Where(d => d.GroupId == groupId && d.ActivatedAt != null)
            .OrderByDescending(d => d.Date)
            .FirstOrDefaultAsync();
        if (entry == null)
            return (GuessResult.NoActiveQuestion, null, null);

        var members = await LoadMembersAsync(groupId);
        if (
            !AuthorGuessRules.IsGuessable(entry.Question, members.Keys)
            || AuthorGuessRules.AuthorOf(entry.Question) == userId
        )
            return (GuessResult.NotGuessable, null, null);

        var hasVoted = await context.Votes.AnyAsync(v => v.QuestionId == entry.QuestionId && v.UserId == userId);
        if (!hasVoted)
            return (GuessResult.MustVoteFirst, null, null);

        return (GuessResult.Success, entry, members);
    }

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
        DailyEntry entry,
        int userId,
        int authorId,
        IEnumerable<int> memberIds,
        bool hintUsed
    )
    {
        var candidates = AuthorGuessRules.Candidates(memberIds, userId);
        if (!hintUsed)
            return candidates;

        var hidden = AuthorGuessRules.HiddenByHint(entry.Id, userId, candidates.Where(id => id != authorId));
        return candidates.Where(id => !hidden.Contains(id)).ToList();
    }

    private static AuthorGuessDto BuildState(
        DailyEntry entry,
        int userId,
        int authorId,
        VoterDto author,
        IReadOnlyCollection<int> memberIds,
        AuthorGuess? row
    )
    {
        if (row?.GuessedAt != null)
            return row.GuessedCreatorId is int guessed
                ? new AuthorGuessDto
                {
                    State = AuthorGuessDto.Guessed,
                    GuessedUserId = guessed,
                    Correct = row.IsCorrect,
                    Author = author,
                }
                : new AuthorGuessDto { State = AuthorGuessDto.Skipped, Author = author };

        var hintUsed = row?.HintUsed == true;
        var wrongCount = AuthorGuessRules.Candidates(memberIds, userId).Count - 1;
        return new AuthorGuessDto
        {
            State = AuthorGuessDto.Pending,
            CandidateIds = VisibleCandidates(entry, userId, authorId, memberIds, hintUsed),
            HintUsed = hintUsed,
            HintAvailable = !hintUsed && AuthorGuessRules.HintRemovals(wrongCount) > 0,
            HintPrice = CoinRewards.AuthorHintPrice,
        };
    }
}
