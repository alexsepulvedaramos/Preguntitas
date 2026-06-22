export interface CreateVote {
    questionId: number;
    guessedCreatorId?: number | null;
    selectedOptionIds?: number[] | null;
    selectedTargetUserId?: number | null; // Superlative
    selectedTargetUserIds?: number[] | null; // Deathmatch, Secret Pairing
    numericValue?: number | null;
    freeText?: string | null;
}