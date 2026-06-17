import { QuestionType } from '../enums/question-type.enum';
import { User } from './user.model';

export interface Option {
    id: number;
    text: string;
    associatedUsers: User[];
}

export interface QuestionToVote {
    id: number;
    text: string;
    type: QuestionType;
    allowNobody: boolean;
    maxSelections: number;
    minValue?: number | null;
    maxValue?: number | null;
    options: Option[];
}

export interface CreateQuestion {
    text: string;
    type: QuestionType;
    creatorId: number;
    groupId: number;
    maxSelections: number;
    minValue?: number | null;
    maxValue?: number | null;
    blacklistedUserIds: number[];
    options: { text: string }[];
}