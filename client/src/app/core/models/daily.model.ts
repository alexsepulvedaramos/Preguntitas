// Daily lifecycle models. Returned/consumed by the /daily endpoints.

import { QuestionType } from '../enums/question-type.enum';
import { CreateQuestion, QuestionToVote } from './question.model';
import { QuestionResult } from './result.model';

// ── GET /daily/current — the redesigned { today, selection } shape (spec §4.7) ──

export type TodayStatus = 'voting' | 'results' | 'no_question';

// The currently-open question (today's voting/results state).
export interface TodayState {
    status: TodayStatus;
    question: QuestionToVote | null;
    userHasVoted: boolean;
    results: QuestionResult | null; // present when userHasVoted
    closesAt: string | null; // next T, ISO UTC; null when no_question
}

// The next-day question being selected (the "te toca elegir" panel).
export interface SelectionState {
    date: string; // YYYY-MM-DD — the day being selected
    activatesAt: string; // next T, ISO UTC
    selectorUserId: number;
    selectorUsername: string;
    isCurrentUserSelector: boolean;
    pendingQuestion: QuestionToVote | null; // only returned to the selector
    isAutoSelected: boolean;
}

export interface DailyStatus {
    today: TodayState;
    selection: SelectionState | null; // null before the group has started (< 2 members)
}

// ── POST /daily/select — exactly one source must be provided ──

export interface SelectQuestion {
    existingQuestionId?: number | null; // pick one from the group pool
    templateId?: number | null; // clone a base-pack template
    newQuestion?: CreateQuestion | null; // create inline
}

// ── GET /daily/selection-sources ──

export type SelectionSourceType = 'pool' | 'pack';

export interface SelectionSourceItem {
    sourceType: SelectionSourceType;
    id: number; // QuestionId when sourceType === 'pool', TemplateId when 'pack'
    text: string;
    type: QuestionType;
    options: string[]; // only populated for CustomPoll items
}

export interface SelectionSources {
    pool: SelectionSourceItem[];
    pack: SelectionSourceItem[];
}
