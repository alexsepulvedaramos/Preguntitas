// Sources the day's selector can pick a question from.
// Returned by GET /api/groups/{groupId}/daily/selection-sources.

import { QuestionType } from '../enums/question-type.enum';

export type SelectionSourceType = 'pool' | 'pack';

export interface SelectionSourceItem {
    sourceType: SelectionSourceType;
    id: number; // QuestionId when sourceType === 'pool', TemplateId when 'pack'
    text: string;
    type: QuestionType;
}

export interface SelectionSources {
    pool: SelectionSourceItem[];
    pack: SelectionSourceItem[];
}
