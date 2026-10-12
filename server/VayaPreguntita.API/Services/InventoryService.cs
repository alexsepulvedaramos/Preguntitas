using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Coins;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Services;

// Item inventory (rama 45, §17.10): one row per (user, item) with the count held. Buying
// writes a normal ledger row with no group; using one writes nothing to the ledger.
public class InventoryService(AppDbContext context, ICoinService coinService) : IInventoryService
{
    public async Task<InventoryDto> GetAsync(int userId)
    {
        var counts = await context
            .UserInventory.Where(i => i.UserId == userId)
            .ToDictionaryAsync(i => i.Item, i => i.Quantity);
        return new InventoryDto
        {
            FlashQuestion = counts.GetValueOrDefault(InventoryItem.FlashQuestion),
            StreakFreeze = counts.GetValueOrDefault(InventoryItem.StreakFreeze),
            AuthorHint = counts.GetValueOrDefault(InventoryItem.AuthorHint),
            StreakRepair = counts.GetValueOrDefault(InventoryItem.StreakRepair),
        };
    }

    public async Task<bool> BuyAsync(int userId, InventoryItem item)
    {
        var ownsTransaction = context.Database.CurrentTransaction == null;
        await using var transaction = ownsTransaction
            ? await context.Database.BeginTransactionAsync()
            : null;

        // No group and no RefId: the ledger's unique index treats nulls as distinct, so
        // buying the same item again is never mistaken for a retry.
        if (!await coinService.SpendAsync(userId, InventoryItems.Reason(item), InventoryItems.Price(item), null, null))
            return false;

        // Upsert, so two purchases at once both land.
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "UserInventory" ("UserId", "Item", "Quantity") VALUES ({userId}, {(int)item}, 1)
            ON CONFLICT ("UserId", "Item") DO UPDATE SET "Quantity" = "UserInventory"."Quantity" + 1
            """
        );

        if (transaction != null)
            await transaction.CommitAsync();
        return true;
    }

    public async Task<bool> TryConsumeAsync(int userId, InventoryItem item) =>
        // Conditional update: concurrent uses can never take the count below zero.
        await context
            .UserInventory.Where(i => i.UserId == userId && i.Item == item && i.Quantity > 0)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.Quantity, i => i.Quantity - 1)) > 0;
}
