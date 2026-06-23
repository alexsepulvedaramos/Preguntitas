import { QuestionType } from '../enums/question-type.enum';

// Badge tone per question type — gives each type a consistent visual identity across the
// voting and results screens. Reuses the --chart-1..6 results palette (rama 7, spec §13 row 7).
export const QUESTION_TYPE_BADGE_CLASS: Record<QuestionType, string> = {
  [QuestionType.CustomPoll]: 'bg-chart-1/15 text-chart-1',
  [QuestionType.Superlative]: 'bg-chart-2/15 text-chart-2',
  [QuestionType.Deathmatch]: 'bg-chart-3/15 text-chart-3',
  [QuestionType.SecretPairing]: 'bg-chart-4/15 text-chart-4',
  [QuestionType.Scale]: 'bg-chart-5/15 text-chart-5',
  [QuestionType.OpenText]: 'bg-chart-6/15 text-chart-6',
};
