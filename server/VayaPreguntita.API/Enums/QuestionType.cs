namespace VayaPreguntita.API.Enums;

public enum QuestionType
{
    // Classic poll with predefined options.
    // Example: "What time are we meeting for dinner?"
    CustomPoll,

    // Group poll where the answer is a person from the current group.
    // Example: "Who is most likely to end up in jail?"
    Superlative,

    // Team vs Team question with static participants.
    // Example: "Deathmatch: [Juan & Marta] vs [Luis & Ana]. Who wins?"
    Deathmatch,

    // Numeric range rating.
    // Example: "From 1 to 10, how crazy is Aguacate today?"
    Scale,

    // Matchmaking question with exactly two selections.
    // Example: "Which two people in the group would make the best couple?"
    SecretPairing,

    // Open-ended question: every member types a free-text answer.
    // Example: "What's your favourite memory from this trip?"
    OpenText,
}
