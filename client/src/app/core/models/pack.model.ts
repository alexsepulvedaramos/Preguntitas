import { QuestionType } from '../enums/question-type.enum';

// Mirrors backend PackDto — a pack with its effective enablement for the requesting group.
export interface Pack {
    id: number;
    name: string;
    description: string;
    enabled: boolean;
}

// Mirrors backend PackTemplateDto.
export interface PackTemplate {
    id: number;
    text: string;
    type: QuestionType;
    packId: number;
    options: string[]; // only populated for CustomPoll templates
}

// Mirrors backend PackTemplatePageDto — cursor-based paginated response.
export interface PackTemplatePage {
    items: PackTemplate[];
    hasMore: boolean;
}
