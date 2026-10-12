import { QuestionType } from '../../core/enums/question-type.enum';
import { QuestionToVote } from '../../core/models/question.model';
import { OptionResult, QuestionResult, Voter } from '../../core/models/result.model';
import { StreakUpdate } from '../../core/models/streak.model';
import { FRAME_STREAK, STREAK_TIERS, StreakTierKey, nextStreakTier, streakTierFor } from '../../core/constants/streak-tiers';
import { TranslationKey } from '../../core/i18n/translation.types';
import { GroupMember } from '../groups/models/group.models';

// Everything the /demo walkthrough (rama 25) shows: the fictional group and its scripted
// votes. Its copy lives in the dictionaries under `demo.*` (rama 23); the demo page fills
// in the translated texts. Nothing here touches the API.

export const DEMO_GROUP_ID = 0;
export const DEMO_VISITOR_ID = 99;
export const DEMO_NICKNAME_MAX_LENGTH = 20;
// The group's daily time, shown in the countdown and the selection step.
export const DEMO_DAILY_HOUR = 21;

interface DemoFriend {
  id: number;
  username: string;
  // Streak before the visitor's first vote and after the "two days later" jump.
  streakBefore: number;
  streakAfter: number;
  // Who this friend votes for today (DEMO_VISITOR_ID = the visitor).
  votesFor: number;
}

// Eva holds the crown until she forgets to vote; then it passes to the visitor, whose
// 3-day streak is the group's best. Three of the four vote for the visitor and Gus
// throws his vote at Carlos.
const FRIENDS: readonly DemoFriend[] = [
  { id: 1, username: 'Ana', streakBefore: 3, streakAfter: 2, votesFor: DEMO_VISITOR_ID },
  { id: 2, username: 'Carlos', streakBefore: 0, streakAfter: 1, votesFor: DEMO_VISITOR_ID },
  { id: 3, username: 'Eva', streakBefore: 7, streakAfter: 0, votesFor: DEMO_VISITOR_ID },
  { id: 4, username: 'Gus', streakBefore: 1, streakAfter: 2, votesFor: 2 },
];

// Eva's streak the day she missed (7 + the two days she still voted).
export const DEMO_LOST_STREAK = { username: 'Eva', days: 9 };
export const DEMO_VISITOR_STREAK_AFTER = 3;

const avatarFor = (seed: string) =>
  `https://api.dicebear.com/9.x/notionists-neutral/svg?seed=${encodeURIComponent(seed)}`;

function member(
  id: number,
  username: string,
  avatarUrl: string | null,
  streak: number,
  hasCrown: boolean,
  isCurrentUser = false
): GroupMember {
  return {
    id,
    username,
    avatarUrl,
    frameColor: FRAME_STREAK,
    joinedAt: '2026-01-01T00:00:00Z',
    isAdmin: id === 1,
    isCurrentUser,
    notificationsMuted: false,
    currentStreak: streak,
    hasCrown,
    title: null,
  };
}

// The group as it is when the visitor arrives (phase 'before') and after the jump.
export function demoMembers(nickname: string, phase: 'before' | 'after'): GroupMember[] {
  const friends = FRIENDS.map((f) => {
    const streak = phase === 'before' ? f.streakBefore : f.streakAfter;
    return member(f.id, f.username, avatarFor(f.username), streak, false);
  });
  const visitorStreak = phase === 'before' ? 0 : DEMO_VISITOR_STREAK_AFTER;
  const all = [...friends, member(DEMO_VISITOR_ID, nickname, null, visitorStreak, false, true)];

  // Crown = the group's best current streak (rama 19).
  const best = Math.max(...all.map((m) => m.currentStreak));
  const crowned = all.find((m) => m.currentStreak === best);
  if (crowned && best > 0) crowned.hasCrown = true;
  return all;
}

// Text left empty: the page sets it from `demo.question` in the UI language.
export const DEMO_QUESTION: QuestionToVote = {
  id: 1,
  text: '',
  type: QuestionType.Superlative,
  allowNobody: false,
  blacklistedUserIds: [],
  allowOther: false,
  teams: [],
  options: [],
};

// Today's results once the visitor has voted for `visitorVote` (a member id).
export function demoResult(question: QuestionToVote, members: GroupMember[], visitorVote: number): QuestionResult {
  const byId = new Map(members.map((m) => [m.id, m]));
  const ballots = [
    ...FRIENDS.map((f) => ({ voter: f.id, target: f.votesFor })),
    { voter: DEMO_VISITOR_ID, target: visitorVote },
  ];

  const toVoter = (id: number): Voter => {
    const m = byId.get(id)!;
    return { id: m.id, username: m.username, avatarUrl: m.avatarUrl, frameColor: m.frameColor };
  };

  const targets = [...new Set(ballots.map((b) => b.target))];
  const results: OptionResult[] = targets
    .map((targetId) => {
      const target = byId.get(targetId)!;
      const voters = ballots.filter((b) => b.target === targetId).map((b) => toVoter(b.voter));
      return {
        id: targetId,
        displayText: target.username,
        targetUser: {
          id: target.id,
          username: target.username,
          email: '',
          avatarUrl: target.avatarUrl,
          frameColor: target.frameColor,
        },
        teamMembers: [],
        voteCount: voters.length,
        voters,
        percentage: Math.round((voters.length / ballots.length) * 100),
      };
    })
    .sort((a, b) => b.voteCount - a.voteCount);

  return {
    id: question.id,
    text: question.text,
    type: question.type,
    dateCreated: new Date(),
    totalVotes: ballots.length,
    results,
    freeTextResponses: [],
  };
}

// How many friends voted for the visitor (the punchline of the results step).
export const DEMO_FRIENDS_VOTING_VISITOR = FRIENDS.filter((f) => f.votesFor === DEMO_VISITOR_ID).length;
export const DEMO_FRIEND_COUNT = FRIENDS.length;

export interface DemoPick {
  id: number;
  text: TranslationKey;
  type: QuestionType;
}

// Pack questions offered in the "Mañana eliges tú" step (real Base pack templates).
export const DEMO_PICKS: readonly DemoPick[] = [
  { id: 1, text: 'demo.select.pick1', type: QuestionType.Superlative },
  { id: 2, text: 'demo.select.pick2', type: QuestionType.CustomPoll },
  { id: 3, text: 'demo.select.pick3', type: QuestionType.Scale },
];

// The vote that takes the visitor's streak from 2 to 3: first frame and first title.
export function demoStreakUpdate(t: (key: TranslationKey) => string): StreakUpdate {
  const current = DEMO_VISITOR_STREAK_AFTER;
  const tier = streakTierFor(current)!;
  const next = nextStreakTier(current);
  return {
    previous: current - 1,
    current,
    best: current,
    tierKey: tier.key,
    tierTitle: t(tier.titleKey),
    isTierUp: true,
    titleUnlocked: true,
    milestoneLabel: null,
    nextTierKey: next?.key ?? null,
    nextTierTitle: next ? t(next.titleKey) : null,
    nextTierAt: next?.minDays ?? null,
    daysToNextTier: next ? next.minDays - current : null,
  };
}

// The higher frames teased on the last step (the page labels them with the material).
const TEASED_TIERS: readonly StreakTierKey[] = ['stone', 'silver', 'gold', 'amethyst'];
export const DEMO_TEASED_FRAMES = STREAK_TIERS.filter((t) => TEASED_TIERS.includes(t.key)).map((t) => ({
  key: t.key,
  avatarUrl: avatarFor(t.key),
  streak: t.minDays,
  titleKey: t.titleKey,
}));

export type DemoStep = 'intro' | 'vote' | 'results' | 'select' | 'jump' | 'final';
export const DEMO_STEPS: readonly DemoStep[] = ['intro', 'vote', 'results', 'select', 'jump', 'final'];
