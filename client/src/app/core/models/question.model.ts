import { QuestionType } from '../enums/question-type.enum';
import { UserDto } from './user.model';

export interface OptionDto {
    id: number;
    text: string;
    associatedUsers: UserDto[];
}

export interface QuestionToVoteDto {
    id: number;
    text: string;
    type: QuestionType;
    allowNobody: boolean;
    maxSelections: number;
    minValue?: number | null;
    maxValue?: number | null;
    options: OptionDto[];
}

export interface CreateQuestionDto {
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