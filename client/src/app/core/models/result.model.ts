import { User } from './user.model';
import { QuestionType } from '../enums/question-type.enum';
import { QuestionToVote } from './question.model';
import { Reaction } from './reaction.model';

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
    voteId: number; // what a reaction points at (rama 48)
    userId: number;
    username: string;
    avatarUrl: string | null;
    frameColor: string | null;
    text: string;
    reactions: Reaction[];
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
    author?: Voter | null; // history detail: who wrote it, once closed (rama 36)
    // History gating (rama 39): results stay empty until you vote; a closed question also
    // carries what's needed to vote it late from the history.
    locked?: boolean;
    lateVoteQuestion?: QuestionToVote | null;
    // Flash with anonymous votes (rama 50): no voters and no names on others' answers (yours
    // keeps your id).
    anonymousVotes?: boolean;
    // Time capsule (rama 50): sealed until capsuleOpensAt, then results stay empty for everyone.
    capsuleOpensAt?: string | null;
    sealed?: boolean;
    // Scale only — present when type === Scale to enable full-range distribution display.
    rangeMin?: number | null;
    rangeMax?: number | null;
}