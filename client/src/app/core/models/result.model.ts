import { User } from './user.model';
import { QuestionType } from '../enums/question-type.enum';

export interface Voter {
    id: number;
    username: string;
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

// Mirrors backend QuestionResultDto.
export interface QuestionResult {
    id: number;
    text: string;
    type: QuestionType;
    dateCreated: string | Date;
    totalVotes: number;
    results: OptionResult[];
}