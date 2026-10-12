namespace VayaPreguntita.API.Enums;

public enum FlashResult
{
    Success,
    GroupNotStarted,
    InvalidQuestion,
    DailyLimitReached,
    NotEnoughCoins,

    // Voting
    NotFound,
    AlreadyVoted,
    InvalidVote,
}
