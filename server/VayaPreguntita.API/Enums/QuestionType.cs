namespace VayaPreguntita.API.Enums;

public enum QuestionType
{
    // Questions with predefined text options where only one can be selected.
    // Example: "What is the best fruit? (A: Banana, B: Melon, C: Grapes)"
    SingleChoice,

    // Questions with predefined text options where multiple can be selected.
    // Example: "Which programming languages do you know? (C#, JavaScript, Python, Rust)"
    MultipleChoice,

    // Questions based on a numeric range.
    // Example: "How much do you like Mondays? (Rate from 1 to 10)"
    Scale,

    // Questions where the answer is a person from the current group.
    // Example: "Who is most likely to become a millionaire?" (The Superlative)
    TargetUser,

    // Questions requiring a written response.
    // Example: "What is your biggest motivation in life?"
    FreeText,
}
