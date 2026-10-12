using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Helpers;

// Shop prices of the inventory items (rama 45, §17.10) — the same as paying in place — and the
// ledger reason a purchase is recorded under.
public static class InventoryItems
{
    public static int Price(InventoryItem item) =>
        item switch
        {
            InventoryItem.FlashQuestion => CoinRewards.FlashQuestionPrice,
            InventoryItem.StreakFreeze => StreakFreezeRules.FreezePrice,
            InventoryItem.AuthorHint => CoinRewards.AuthorHintPrice,
            InventoryItem.StreakRepair => StreakFreezeRules.RepairPrice,
            _ => throw new ArgumentOutOfRangeException(nameof(item)),
        };

    public static CoinReason Reason(InventoryItem item) =>
        item switch
        {
            InventoryItem.FlashQuestion => CoinReason.FlashQuestion,
            InventoryItem.StreakFreeze => CoinReason.StreakFreeze,
            InventoryItem.AuthorHint => CoinReason.AuthorHint,
            InventoryItem.StreakRepair => CoinReason.StreakRepair,
            _ => throw new ArgumentOutOfRangeException(nameof(item)),
        };
}
