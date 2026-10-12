using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Coins;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Services;

// Coin wallet and ledger (§17.2): the balance lives in User.Coins, every change is also a
// CoinTransaction row, and both are written in the same DB transaction.
public class CoinService(AppDbContext context, TimeProvider clock) : ICoinService
{
    public const int PageSize = 20;

    private static readonly CoinReason[] CappedReasons = Enum.GetValues<CoinReason>()
        .Where(CoinRewards.IsCapped)
        .ToArray();

    public async Task<CoinGrant> GrantAsync(
        int userId,
        CoinReason reason,
        int amount,
        int? groupId,
        int? refId
    )
    {
        if (amount <= 0)
            return new CoinGrant(0, false);

        var ownsTransaction = context.Database.CurrentTransaction == null;
        await using var transaction = ownsTransaction
            ? await context.Database.BeginTransactionAsync()
            : null;

        // Locks the user's row so concurrent grants (votes in two groups at once) can't
        // both read the same "earned today" and overshoot the cap.
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"Users\" WHERE \"Id\" = {userId} FOR UPDATE"
        );

        var alreadyPaid = await context.CoinTransactions.AnyAsync(t =>
            t.UserId == userId && t.Reason == reason && t.RefId == refId
        );
        if (alreadyPaid)
            return new CoinGrant(0, false);

        var granted = amount;
        var capReached = false;
        if (CoinRewards.IsCapped(reason))
        {
            var room = CoinRewards.DailyCap - await EarnedTodayCappedAsync(userId);
            if (room < amount)
            {
                granted = Math.Max(0, room);
                capReached = true;
            }
        }

        if (granted > 0)
        {
            context.CoinTransactions.Add(
                new CoinTransaction
                {
                    UserId = userId,
                    Amount = granted,
                    Reason = reason,
                    GroupId = groupId,
                    RefId = refId,
                    CreatedAt = DailyClock.UtcNow(clock),
                }
            );
            await context.SaveChangesAsync();
            await context
                .Users.Where(u => u.Id == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Coins, u => u.Coins + granted));
        }

        if (transaction != null)
            await transaction.CommitAsync();
        return new CoinGrant(granted, capReached);
    }

    public async Task<int> GetBalanceAsync(int userId) =>
        await context.Users.Where(u => u.Id == userId).Select(u => u.Coins).FirstOrDefaultAsync();

    public async Task<CoinHistoryPageDto> GetHistoryAsync(int userId, int? before)
    {
        var query = context.CoinTransactions.Where(t => t.UserId == userId);
        if (before.HasValue)
            query = query.Where(t => t.Id < before.Value);

        var rows = await query
            .OrderByDescending(t => t.Id)
            .Take(PageSize + 1)
            .Select(t => new CoinTransactionDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Reason = t.Reason,
                GroupId = t.GroupId,
                GroupName = t.Group != null ? t.Group.Name : null,
                CreatedAt = t.CreatedAt,
            })
            .ToListAsync();

        var hasMore = rows.Count > PageSize;
        if (hasMore)
            rows.RemoveAt(PageSize);

        return new CoinHistoryPageDto
        {
            Items = rows,
            NextBefore = hasMore ? rows[^1].Id : null,
        };
    }

    // Capped coins earned since the start of today's local (UTC+2) day, across all groups.
    private async Task<int> EarnedTodayCappedAsync(int userId)
    {
        var dayStartUtc = DailyClock.ToUtc(DailyClock.Today(clock), TimeOnly.MinValue);
        return await context
            .CoinTransactions.Where(t =>
                t.UserId == userId
                && t.CreatedAt >= dayStartUtc
                && t.Amount > 0
                && CappedReasons.Contains(t.Reason)
            )
            .SumAsync(t => t.Amount);
    }
}
