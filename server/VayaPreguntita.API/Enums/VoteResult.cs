namespace VayaPreguntita.API.Enums;

public enum VoteResult
{
    Success,
    NoActiveQuestion,
    AlreadyVoted,
    InvalidPayload,

    // Late vote from the history (§17.7)
    QuestionNotFound,
    QuestionNotClosed,
}
