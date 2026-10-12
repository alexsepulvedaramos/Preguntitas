using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.DTOs.Coins;

// The items a user holds (rama 45, §17.10), sent in GET users/me and with every purchase or use.
public class InventoryDto
{
    public int FlashQuestion { get; set; }
    public int StreakFreeze { get; set; }
    public int AuthorHint { get; set; }
    public int StreakRepair { get; set; }
}

// POST api/shop/inventory: buys one item into the inventory.
public class BuyItemRequestDto
{
    public InventoryItem Item { get; set; }
}

public class BuyItemResponseDto
{
    public int Balance { get; set; }
    public InventoryDto Inventory { get; set; } = new();
}
