using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.DTOs.Shop;

// GET api/shop: the cosmetics catalog, with what the user already owns (rama 42, §17.10).
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
