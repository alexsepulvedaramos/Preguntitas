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
}
