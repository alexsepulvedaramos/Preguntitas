using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.DTOs.Shop;

// GET api/shop: the cosmetics catalog, with what the user already owns (rama 42, §17.10).
// Seasonal items appear in the week before their window (coming soon), while on sale, and
// afterwards only to their owners (rama 57).
public class ShopCatalogDto
{
    public List<CosmeticDto> Cosmetics { get; set; } = [];
}

public class CosmeticDto
{
    public string Key { get; set; } = string.Empty;
    public CosmeticKind Kind { get; set; }
    public int Price { get; set; }
    public CosmeticRarity Rarity { get; set; }
    public bool Owned { get; set; }

    // Seasonal items (rama 57): the sale window (UTC), for the countdowns. null for the
    // permanent catalog.
    public DateTime? OnSaleFrom { get; set; }
    public DateTime? OnSaleUntil { get; set; }

    // false for a seasonal item not on sale: coming soon, or listed because you own it.
    public bool OnSale { get; set; } = true;
}

// POST api/shop/cosmetics
public class BuyCosmeticRequestDto
{
    public string Key { get; set; } = string.Empty;
}

public class BuyCosmeticResponseDto
{
    public int Balance { get; set; }
    public List<string> OwnedCosmetics { get; set; } = [];
}
