namespace VayaPreguntita.API.Enums;

// Outcome of buying a streak freeze (for yourself or as a gift) or a streak repair (§17.9).
public enum StreakPurchaseResult
{
    Success,
    NotEnoughCoins,
    FreezeLimitReached,
    NotRepairable,
    RepairLimitReached,
    InvalidTarget,
}
