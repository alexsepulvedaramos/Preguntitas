export interface ChatMessage {
  id: number;
  body: string;
  createdAt: string;
  userId: number;
  username: string;
  isCurrentUser: boolean;
}
