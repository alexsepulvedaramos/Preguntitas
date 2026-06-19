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
}

export interface JoinGroupRequest {
    invitationCode: string;
}

export interface TransferAdminRequest {
    newAdminId: number;
}

export interface UpdateGroupRequest {
    name: string;
    description: string;
    dailyQuestionTime: string;
}