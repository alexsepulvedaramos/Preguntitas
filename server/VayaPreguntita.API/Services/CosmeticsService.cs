using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Shop;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Services;

public class CosmeticsService(AppDbContext context, ICoinService coinService, TimeProvider clock)
    : ICosmeticsService
{
    public async Task<ShopCatalogDto> GetCatalogAsync(int userId)
    {
        var owned = (await GetOwnedKeysAsync(userId)).ToHashSet();
        var now = DailyClock.UtcNow(clock);
        return new ShopCatalogDto
        {
            Cosmetics = ShopCatalog
                .All.Where(i => i.IsOnSale(now) || owned.Contains(i.Key))
                .Select(i => new CosmeticDto
                {
                    Key = i.Key,
                    Kind = i.Kind,
                    Price = i.Price,
                    Rarity = i.Rarity,
                    Owned = owned.Contains(i.Key),
                    OnSaleUntil = i.OnSaleUntil,
                    OnSale = i.IsOnSale(now),
                })
                .ToList(),
        };
    }

    public async Task<List<string>> GetOwnedKeysAsync(int userId) =>
        await context
            .UserItems.Where(i => i.UserId == userId)
            .OrderBy(i => i.PurchasedAt)
            .Select(i => i.ItemKey)
            .ToListAsync();

    public async Task<CosmeticPurchaseResult> BuyAsync(int userId, string key)
    {
        var item = ShopCatalog.ByKey(key);
        if (item is null)
            return CosmeticPurchaseResult.UnknownItem;
        if (!item.IsOnSale(DailyClock.UtcNow(clock)))
            return CosmeticPurchaseResult.NotOnSale;
        if (await context.UserItems.AnyAsync(i => i.UserId == userId && i.ItemKey == key))
            return CosmeticPurchaseResult.AlreadyOwned;

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // RefId = the item id: the ledger's unique (UserId, Reason, RefId) index stops a
            // second purchase racing this one, and the ledger can name the item.
            if (!await coinService.SpendAsync(userId, CoinReason.Cosmetic, item.Price, null, item.Id))
                return CosmeticPurchaseResult.NotEnoughCoins;

            context.UserItems.Add(
                new UserItem
                {
                    UserId = userId,
                    ItemKey = key,
                    PurchasedAt = DailyClock.UtcNow(clock),
                }
            );
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return CosmeticPurchaseResult.Success;
        }
        catch (DbUpdateException)
        {
            // A simultaneous purchase of the same item won; the rollback refunds this one.
            context.ChangeTracker.Clear();
            return CosmeticPurchaseResult.AlreadyOwned;
        }
    }

    public async Task<bool> OwnsAsync(int userId, string key, CosmeticKind kind) =>
        ShopCatalog.ByKey(key)?.Kind == kind
        && await context.UserItems.AnyAsync(i => i.UserId == userId && i.ItemKey == key);
}
