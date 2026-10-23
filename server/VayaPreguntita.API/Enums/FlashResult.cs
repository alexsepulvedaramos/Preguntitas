namespace VayaPreguntita.API.Enums;

public enum FlashResult
{
    Success,
    GroupNotStarted,
    InvalidQuestion,
    DailyLimitReached,
    NotEnoughCoins,

    // Launching with one from the inventory (rama 45), but none is left.
    NoneOwned,

    // Voting
    NotFound,
    AlreadyVoted,
    InvalidVote,

    // Flash variants (rama 50)
    InvalidCapsuleDate,
    Closed,
    AlreadyExtended,

    // Only whoever launched a flash can extend it (developer, 2026-10-22).
    NotLauncher,
}
