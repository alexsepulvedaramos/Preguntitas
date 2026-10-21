namespace VayaPreguntita.API.Enums;

// Toque / nudge (rama 43, §17.10).
public enum NudgeResult
{
    Success,
    NoOpenQuestion,
    FlashNotFound,
    InvalidTarget,
    AlreadyVoted,
    MessageTooLong,
    // Can't receive it: notifications off (no device, or nudges disabled) or the group muted.
    Unreachable,
    // Already nudged in this group in the last 24 h.
    Recent,
    NotEnoughCoins,
    NoneOwned,
}
