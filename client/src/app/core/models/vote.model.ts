// Mirrors backend CreateVoteDto exactly — questionId comes from the route, not the body.
export interface CreateVote {
    selectedOptionIds?: number[] | null; // Custom Poll
    selectedTargetUserId?: number | null; // Superlative
    selectedTargetUserIds?: number[] | null; // Deathmatch, Secret Pairing
    numericValue?: number | null; // Scale
    freeText?: string | null; // OpenText, or a Custom Poll "Otro" answer
}