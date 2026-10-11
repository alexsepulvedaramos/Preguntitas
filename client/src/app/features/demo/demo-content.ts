import { QuestionType } from '../../core/enums/question-type.enum';
import { QuestionToVote } from '../../core/models/question.model';
import { OptionResult, QuestionResult, Voter } from '../../core/models/result.model';
import { StreakUpdate } from '../../core/models/streak.model';
import { FRAME_STREAK, STREAK_TIERS, StreakTierKey, nextStreakTier, streakTierFor } from '../../core/constants/streak-tiers';
import { GroupMember } from '../groups/models/group.models';

// Everything the /demo walkthrough (rama 25) shows: the fictional group, its scripted
// votes and every piece of copy, kept in one place so ramas 23–24 can translate it.
// Nothing here touches the API.

export const DEMO_GROUP_ID = 0;
export const DEMO_GROUP_NAME = 'Los de siempre';
export const DEMO_VISITOR_ID = 99;
export const DEMO_DEFAULT_NICKNAME = 'Tú';
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

export const DEMO_QUESTION: QuestionToVote = {
  id: 1,
  text: '¿Quién intentaría abrazar a un zombie en un apocalipsis?',
  type: QuestionType.Superlative,
  allowNobody: false,
  blacklistedUserIds: [],
  allowOther: false,
  teams: [],
  options: [],
};

// Today's results once the visitor has voted for `visitorVote` (a member id).
export function demoResult(members: GroupMember[], visitorVote: number): QuestionResult {
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
    id: DEMO_QUESTION.id,
    text: DEMO_QUESTION.text,
    type: DEMO_QUESTION.type,
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
  text: string;
  type: QuestionType;
  pack: string;
}

// Pack questions offered in the "Mañana eliges tú" step (real Base pack templates).
export const DEMO_PICKS: readonly DemoPick[] = [
  { id: 1, text: '¿Quién se perdería incluso con el GPS en la mano?', type: QuestionType.Superlative, pack: 'Base' },
  { id: 2, text: 'Elige tu superpoder inútil:', type: QuestionType.CustomPoll, pack: 'Base' },
  { id: 3, text: 'Del 1 al 10, ¿qué nota le das a la pizza con piña?', type: QuestionType.Scale, pack: 'Base' },
];

// The vote that takes the visitor's streak from 2 to 3: first frame and first title.
export function demoStreakUpdate(): StreakUpdate {
  const current = DEMO_VISITOR_STREAK_AFTER;
  const tier = streakTierFor(current)!;
  const next = nextStreakTier(current);
  return {
    previous: current - 1,
    current,
    best: current,
    tierKey: tier.key,
    tierTitle: tier.title,
    tierMaterial: tier.material,
    isTierUp: true,
    titleUnlocked: true,
    milestoneLabel: null,
    nextTierKey: next?.key ?? null,
    nextTierTitle: next?.title ?? null,
    nextTierMaterial: next?.material ?? null,
    nextTierAt: next?.minDays ?? null,
    daysToNextTier: next ? next.minDays - current : null,
  };
}

// The higher frames teased on the last step.
const TEASED_TIERS: readonly StreakTierKey[] = ['stone', 'silver', 'gold', 'amethyst'];
export const DEMO_TEASED_FRAMES = STREAK_TIERS.filter((t) => TEASED_TIERS.includes(t.key)).map((t) => ({
  key: t.key,
  username: t.material,
  avatarUrl: avatarFor(t.key),
  streak: t.minDays,
  label: `${t.material} · ${t.minDays} días`,
  title: t.title,
}));

export type DemoStep = 'intro' | 'vote' | 'results' | 'select' | 'jump' | 'final';
export const DEMO_STEPS: readonly DemoStep[] = ['intro', 'vote', 'results', 'select', 'jump', 'final'];

export const DEMO_COPY = {
  badge: 'Demo',
  exit: 'Salir',
  intro: {
    title: `Este es «${DEMO_GROUP_NAME}»`,
    text: 'Un grupo de prueba con cuatro amigos de mentira. En menos de un minuto vas a jugar un día entero: votar, ver quién ha votado a quién y elegir la pregunta de mañana.',
    nicknameLabel: '¿Cómo te llamamos?',
    nicknamePlaceholder: 'Tu nombre o apodo',
    nicknameHint: 'Opcional. Solo se usa en esta demo y no se guarda.',
    start: 'Empezar',
  },
  vote: {
    hint: 'Ha salido la pregunta de hoy y el grupo tiene 24 horas para votar. Vota a quien quieras, también a ti.',
  },
  results: {
    votedLabel: (voted: number, total: number) => `Han votado ${voted} de ${total} miembros`,
    punchline: (count: number, of: number) =>
      count === of ? `Tus ${of} amigos te han votado a ti. 👀` : `${count} de tus ${of} amigos te han votado a ti. 👀`,
    hint: 'Los resultados no son secretos: toca una barra para ver quién ha votado a quién.',
    next: 'Siguiente',
  },
  select: {
    title: 'Mañana eliges tú',
    text: 'Cada día le toca a una persona del grupo, por turnos, elegir la pregunta del día siguiente: de los packs o una que escribáis vosotros. Si nadie elige, sale una de los packs.',
    picked: `¡Elegida! Sale mañana a las ${DEMO_DAILY_HOUR}:00 para todo el grupo. Hasta entonces la puedes cambiar.`,
    next: 'Siguiente',
  },
  jump: {
    title: 'Dos días después…',
    days: ['Día 1', 'Día 2', 'Día 3'],
    voted: 'Votaste',
    text: 'Has votado tres días seguidos. Tu racha sube cada día que votas y, al tercero, tu avatar estrena marco.',
    cta: 'Ver mi racha',
  },
  final: {
    title: 'La corona es tuya',
    text: `Llevas la racha más larga del grupo. ${DEMO_LOST_STREAK.username} se despistó ayer y perdió una racha de ${DEMO_LOST_STREAK.days} días.`,
    groupTitle: `Miembros de «${DEMO_GROUP_NAME}»`,
    you: 'tú',
    teaseTitle: 'Y esto es solo el principio',
    teaseText: 'Cuanto más dure tu racha, mejor es el marco. Cada uno desbloquea un título para siempre.',
    ctaTitle: '¿Lo jugamos de verdad?',
    ctaGuestText: 'Crea tu cuenta, monta tu grupo y comparte el enlace. En cuanto seáis dos, sale la primera pregunta.',
    ctaUserText: 'Crea un grupo y comparte el enlace. En cuanto seáis dos, sale la primera pregunta.',
    register: 'Crear cuenta gratis',
    login: 'Ya tengo cuenta',
    groups: 'Ir a mis grupos',
    replay: 'Volver a empezar',
  },
};
