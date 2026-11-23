namespace VayaPreguntita.API.Enums;

public enum SelectResult
{
    Success,
    NotYourTurn,
    QuestionNotFound,
    AlreadyActivated,
    InvalidQuestion,
    TemplateAlreadyUsed,

    // Only the author (or an admin) may change a question's wording or teams (§5.2).
    NotTheAuthor,

    // Someone else won the race to select first (§17.5).
    RaceLost,
}
