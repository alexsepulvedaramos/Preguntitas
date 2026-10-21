import { GlyphName } from '../../shared/components/glyph/glyphs';
import { Reaction } from './reaction.model';

export interface ChatMessage {
  id: number;
  body: string;
  createdAt: string;
  userId: number | null; // null for system messages
  username: string | null;
  isSystem: boolean;
  glyph?: GlyphName | null; // system messages: the app glyph drawn before the text
  isCurrentUser: boolean;
  reactions: Reaction[]; // rama 48; never on system messages
}
