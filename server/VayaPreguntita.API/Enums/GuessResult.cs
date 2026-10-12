namespace VayaPreguntita.API.Enums;

// Outcome of POST daily/guess and daily/guess/hint (§17.6).
public enum GuessResult
{
    Success,
    NoActiveQuestion,
    NotGuessable,
    MustVoteFirst,
    AlreadyGuessed,
    InvalidCandidate,
    NotEnoughCoins,
    HintNotAvailable,
}
