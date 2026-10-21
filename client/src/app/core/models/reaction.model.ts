// Reactions (rama 48): one member's emoji on a chat message or a player-written answer.
// Mirrors backend ReactionDto; the keys and their order mirror ReactionRules.Emojis.
export const REACTION_EMOJIS = {
  love: '❤️',
  laugh: '😂',
  skull: '💀',
  wow: '😮',
  fire: '🔥',
  upside: '🙃',
} as const;

export type ReactionKey = keyof typeof REACTION_EMOJIS;

export const REACTION_KEYS = Object.keys(REACTION_EMOJIS) as ReactionKey[];

export interface Reaction {
  emoji: ReactionKey;
  userId: number;
  username: string;
}

// What a reaction points at: a chat message, or the Vote row carrying an answer's text.
export interface ReactionTarget {
  kind: 'messages' | 'answers';
  id: number;
}
