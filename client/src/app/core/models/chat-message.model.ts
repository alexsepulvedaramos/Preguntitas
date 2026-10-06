export interface ChatMessage {
  id: number;
  body: string;
  createdAt: string;
  userId: number | null; // null for system messages
  username: string | null;
  isSystem: boolean;
  isCurrentUser: boolean;
}
