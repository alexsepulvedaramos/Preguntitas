import { QuestionType } from '../enums/question-type.enum';

export interface Option {
    id: number;
    text: string;
}

// Mirrors backend QuestionToVoteDto.
export interface QuestionToVote {
    id: number;
    text: string;
    type: QuestionType;
    allowNobody: boolean;
    blacklistedUserIds: number[];
    minSelections?: number | null;
    maxSelections?: number | null;
    rangeMin?: number | null;
    rangeMax?: number | null;
    targetUserId?: number | null;
    allowOther: boolean;
    teams: number[][];
    options: Option[];
}

// Mirrors backend CreateQuestionDto. groupId and creatorId are derived by the
// API from the route and authenticated user, so they are not part of the body.
export interface CreateQuestion {
    text: string;
    type: QuestionType;
    allowNobody: boolean;
    blacklistedUserIds: number[];
    rangeMin?: number | null;
    rangeMax?: number | null;
    targetUserId?: number | null;
    minSelections?: number | null;
    maxSelections?: number | null;
    allowOther: boolean;
    teams: number[][];
    options: { text: string }[];
}

// Mirrors backend QuestionDto — a pool question (GET .../questions/pool).
export interface Question {
    id: number;
    text: string;
    type: QuestionType;
    isUsed: boolean;
    dateCreated: string;
    dateActivated: string | null;
    creatorId: number | null;
    options: Option[];
}
