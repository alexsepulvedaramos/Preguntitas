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
    voters: VoterDto[];
    percentage: number;
}

export interface QuestionResultDto {
    id: number;
    text: string;
    creator: string;
    dateCreated: string | Date;
    totalVotes: number;
    results: OptionResultDto[];
}