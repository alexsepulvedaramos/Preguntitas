namespace VayaPreguntita.API.Enums;

public enum SelectResult
{
    Success,
    NotYourTurn,
    QuestionNotFound,
    AlreadyActivated,
    InvalidQuestion,
    TemplateAlreadyUsed,

    // Someone else won the race to select first (§17.5).
    RaceLost,
}
