using VayaPreguntita.API.DTOs.Streaks;

namespace VayaPreguntita.API.DTOs.Coins;

// POST api/shop/purchase (§17.2). Rama 35 sells the group items: a streak freeze (for
// yourself, or a gift when TargetUserId is another member) and a streak repair.
public class PurchaseRequestDto
{
    public const string StreakFreeze = "streakFreeze";
    public const string StreakRepair = "streakRepair";

    public string Item { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public int? TargetUserId { get; set; }

    // Uses one from the buyer's inventory instead of paying (rama 45).
    public bool UseOwned { get; set; }
}

public class PurchaseResponseDto
{
    public int Balance { get; set; }

    // The buyer's streak in that group after the purchase.
    public MyStreakDto MyStreak { get; set; } = new();

    public InventoryDto Inventory { get; set; } = new();
}
