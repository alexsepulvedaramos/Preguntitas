import { User } from './user.model';

export interface Voter {
    id: number;
    username: string;
}

export interface OptionResult {
    id: number;
    displayText: string;
    targetUser?: User | null;
    voteCount: number;
    voters: Voter[];
    percentage: number;
}

export interface QuestionResult {
    id: number;
    text: string;
    creator: string;
    dateCreated: string | Date;
    totalVotes: number;
    results: OptionResult[];
}