export enum QuestionType {
    // Classic poll with predefined options.
    // Example: "What time are we meeting for dinner?"
    CustomPoll = 0,

    // Group poll where the answer is a person from the current group.
    // Example: "Who is most likely to end up in jail?"
    Superlative = 1,

    // Team vs Team question with static participants.
    // Example: "Deathmatch: [Juan & Marta] vs [Luis & Ana]. Who wins?"
    Deathmatch = 2,

    // Numeric range rating.
    // Example: "From 1 to 10, how crazy is Aguacate today?"
    Scale = 3,

    // Matchmaking question with exactly two selections.
    // Example: "Which two people in the group would make the best couple?"
    SecretPairing = 4,
}
