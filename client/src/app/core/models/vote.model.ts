// Maps to CreateVoteDto.cs
export interface CreateVoteDto {
    questionId: number;
    guessedCreatorId?: number | null;
    selectedOptionIds?: number[] | null;
    selectedTargetUserId?: number | null;
    numericValue?: number | null;
    freeText?: string | null;
}