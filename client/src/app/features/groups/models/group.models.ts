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