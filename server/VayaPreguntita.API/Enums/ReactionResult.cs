namespace VayaPreguntita.API.Enums;

public enum ReactionResult
{
    Success,
    // No such message or answer in this group, or an answer that isn't player-written text.
    NotFound,
    InvalidEmoji,
    OwnContent,
    // An answer whose results the caller can't see yet (they haven't voted, §17.7).
    Locked,
}
