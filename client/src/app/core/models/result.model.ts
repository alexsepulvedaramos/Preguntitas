import { User } from './user.model';
import { QuestionType } from '../enums/question-type.enum';

export interface Voter {
    id: number;
    username: string;
    avatarUrl?: string | null;
    frameColor?: string | null;
}

export interface OptionResult {
    id: number;
    displayText: string;
    targetUser?: User | null;
    teamMembers: User[];
    voteCount: number;
    voters: Voter[];
    percentage: number;
}

export interface FreeTextResponse {
    userId: number;
    username: string;
    avatarUrl: string | null;
    frameColor: string | null;
    text: string;
}

// Mirrors backend QuestionResultDto.
export interface QuestionResult {
    id: number;
    text: string;
    type: QuestionType;
    dateCreated: string | Date;
    totalVotes: number;
    results: OptionResult[];
    freeTextResponses: FreeTextResponse[];
    // Scale only — present when type === Scale to enable full-range distribution display.
    rangeMin?: number | null;
    rangeMax?: number | null;
}