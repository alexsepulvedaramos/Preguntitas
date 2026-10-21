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
    isMine: boolean; // the API never names the creator (guess the author, §17.6)
    options: Option[];
    teams: number[][]; // only populated for Deathmatch items
}

// Mirrors backend QuestionPageDto — cursor-based paginated response.
export interface QuestionPage {
    items: Question[];
    hasMore: boolean;
}

// Mirrors backend HistoryEntryDto — one row in the paginated history list.
export interface HistoryEntry {
    date: string; // YYYY-MM-DD
    questionText: string;
    type: QuestionType;
    totalVotes: number;
    authorUsername: string | null; // member-written questions only (rama 36)
    myGuessCorrect: boolean | null; // null when you didn't guess
    flashId?: number | null; // a closed flash question (rama 44, marked with a bolt); authorUsername = who launched it
    isAnonymous?: boolean; // anonymous flash (rama 50): no authorUsername
    capsuleOpensAt?: string | null; // time capsule (rama 50): when its results open
}

// Mirrors backend HistoryPageDto — cursor-based paginated response.
export interface HistoryPage {
    items: HistoryEntry[];
    hasMore: boolean;
}
