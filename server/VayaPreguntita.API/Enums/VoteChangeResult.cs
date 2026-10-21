namespace VayaPreguntita.API.Enums;

// "Change my vote" (rama 43, §17.10).
public enum VoteChangeResult
{
    Success,
    NoOpenQuestion,
    FlashNotFound,
    FlashClosed,
    NotVoted,
    AlreadyChanged,
    InvalidVote,
    NotEnoughCoins,
    NoneOwned,
}
