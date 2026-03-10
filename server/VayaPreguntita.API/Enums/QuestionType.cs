namespace VayaPreguntita.API.Enums;

public enum QuestionType
{
    // Uses SelectedOptionId (e.g., standard custom polls, true/false)
    SingleChoice,

    // Uses SelectedOptionId (Will require logic to allow multiple Vote rows per user)
    MultipleChoice,

    // Uses NumericValue (e.g., rate from 1 to 10)
    Scale,

    // Uses SelectedTargetUserId (e.g., "Who in the group is most likely to...")
    TargetUser,

    // Uses FreeText (Open opinions)
    FreeText,
}
