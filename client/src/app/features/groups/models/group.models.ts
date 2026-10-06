export interface CreateGroupRequest {
    name: string;
    description: string;
    dailyQuestionTime: string;
}

export interface GroupResponse {
    id: number;
    name: string;
    description: string;
    invitationCode: string;
    creatorUsername: string;
    dailyQuestionTime: string;
    dailyStatus?: 'voting' | 'selector' | 'results' | 'no_question';
    // Set only on the update response: the daily time changed but today's question had
    // already activated, so the new time takes effect from the next cycle (§4.8).
    dailyTimeChangeAppliesFromTomorrow?: boolean;
}

export interface GroupMember {
    id: number;
    username: string;
    avatarUrl: string | null;
    frameColor: string | null;
    joinedAt: string;
    isAdmin: boolean;
    isCurrentUser: boolean;
    notificationsMuted: boolean;
    currentStreak: number; // effective voting streak in this group (rama 19)
}

export interface JoinGroupRequest {
    invitationCode: string;
}

export interface InvitePreview {
    id: number;
    name: string;
    description: string;
    memberCount: number;
}

export interface TransferAdminRequest {
    newAdminId: number;
}

export interface UpdateGroupRequest {
    name: string;
    description: string;
    dailyQuestionTime: string;
}