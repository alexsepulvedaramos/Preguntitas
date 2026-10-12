// Monthly summary ("wrapped", spec §17.12). Month is "YYYY-MM".

export interface MonthlySummaryListItem {
  month: string;
  questions: number;
}

export interface SummaryPerson {
  userId: number;
  username: string;
  avatarUrl: string | null;
}

export type SummaryAwardKey = 'superlative' | 'selector' | 'guesser' | 'consistent' | 'starAuthor';

// Titles, not positions: every member tied at the top is a winner. `value` is a count
// (a percentage for "consistent"); questionText is set for the star author.
export interface SummaryAward {
  key: SummaryAwardKey;
  value: number;
  winners: { person: SummaryPerson; questionText: string | null }[];
}

export interface SummaryMember {
  person: SummaryPerson;
  answered: number;
  eligible: number;
  percent: number;
  bestRun: number;
}

export interface MonthlySummary {
  month: string;
  questions: number;
  participation: number; // 0–100
  awards: SummaryAward[];
  members: SummaryMember[]; // alphabetical
  myMatch: { people: SummaryPerson[]; count: number } | null;
}
