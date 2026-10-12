// Daily lifecycle models. Returned/consumed by the /daily endpoints.

import { QuestionType } from '../enums/question-type.enum';
import { CreateQuestion, QuestionToVote } from './question.model';
import { QuestionResult, Voter } from './result.model';
import { MyStreak } from './streak.model';
import { CoinsEarned } from './coin.model';

// ── GET /daily/current — the redesigned { today, selection } shape (spec §4.7) ──

export type TodayStatus = 'voting' | 'results' | 'no_question';

// The currently-open question (today's voting/results state).
export interface TodayState {
    status: TodayStatus;
    question: QuestionToVote | null;
    userHasVoted: boolean;
    results: QuestionResult | null; // present when userHasVoted
    closesAt: string | null; // next T, ISO UTC; null when no_question
    voterIds: number[]; // members who have voted (never their answers)
    memberCount: number;
    guess: AuthorGuess | null; // once you have voted, when the question is guessable (§17.6)
}

// "Guess the author" (§17.6). pending → guessed | skipped; "mine" when you wrote it.
export type AuthorGuessState = 'pending' | 'guessed' | 'skipped' | 'mine';

export interface AuthorGuess {
    state: AuthorGuessState;
    candidateIds: number[]; // pending: who you can pick (fewer after the hint)
    hintAvailable: boolean;
    hintUsed: boolean;
    hintPrice: number;
    guessedUserId: number | null; // guessed
    correct: boolean | null; // guessed
    author: Voter | null; // guessed, skipped and mine
    caughtCount: number; // mine: "Te han pillado X de Y"
    guessCount: number;
}

export interface GuessAuthorRequest {
    creatorId?: number;
    skip?: boolean;
}

export interface GuessAuthorResponse {
    guess: AuthorGuess;
    coins: CoinsEarned;
}

export interface AuthorHintResponse {
    guess: AuthorGuess;
    balance: number;
}

// The next-day question being selected (the "te toca elegir" panel).
export interface SelectionState {
    date: string; // YYYY-MM-DD — the day being selected
    activatesAt: string; // next T, ISO UTC
    selectorUserId: number | null; // only sent to the selector themself (§17.6)
    selectorUsername: string | null;
    isCurrentUserSelector: boolean;
    pendingQuestion: QuestionToVote | null; // only returned to the selector
    isAutoSelected: boolean; // still the automatic pick: not picked, created or confirmed yet
}

export interface DailyStatus {
    today: TodayState;
    // null before the group has started (< 2 members), and briefly after T until the next
    // selector is drawn (§17.4)
    selection: SelectionState | null;
    myStreak: MyStreak; // the current user's streak in this group (rama 19)
}

// ── POST /daily/select — exactly one source must be provided ──

export interface SelectQuestion {
    existingQuestionId?: number | null; // pick one from the group pool
    templateId?: number | null; // clone a base-pack template
    newQuestion?: CreateQuestion | null; // create inline
    teamsOverride?: number[][] | null; // companion to existingQuestionId: update DM teams in place
    overwriteQuestionId?: number | null; // companion to newQuestion: update existing question content
}

// ── Unified shape the picker dialog maps both Question and PackTemplate pages into,
// so the selection/team-assignment logic doesn't care which paginated source an item
// came from (see select-question-dialog.component.ts). ──

export type SelectionSourceType = 'pool' | 'pack';

export interface SelectionSourceItem {
    sourceType: SelectionSourceType;
    id: number; // QuestionId when sourceType === 'pool', TemplateId when 'pack'
    text: string;
    type: QuestionType;
    options: string[]; // only populated for CustomPoll items
    teams: number[][]; // only populated for Deathmatch items
    isMine?: boolean; // only populated for pool items
}
