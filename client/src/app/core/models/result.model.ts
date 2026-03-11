import { UserDto } from './user.model';

export interface VoterDto {
    id: number;
    username: string;
}

export interface OptionResultDto {
    id: number;
    displayText: string;
    targetUser?: UserDto | null;
    voteCount: number;
    percentage: number;
    voters: VoterDto[];
}

export interface QuestionResultDto {
    id: number;
    text: string;
    creator: string;
    dateCreated: Date | string;
    totalVotes: number;
    results: OptionResultDto[];
}