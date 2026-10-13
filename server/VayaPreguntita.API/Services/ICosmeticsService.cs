using VayaPreguntita.API.DTOs.Shop;
using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Services;

// Shop cosmetics (rama 42, §17.10): bought once, kept forever, equipped from the shop.
public interface ICosmeticsService
{
    Task<ShopCatalogDto> GetCatalogAsync(int userId);

    Task<List<string>> GetOwnedKeysAsync(int userId);

    // Charges the item's price and records it as owned, in one transaction.
    Task<CosmeticPurchaseResult> BuyAsync(int userId, string key);

    // Whether the key is a catalog item of that kind that the user owns.
    Task<bool> OwnsAsync(int userId, string key, CosmeticKind kind);
}
