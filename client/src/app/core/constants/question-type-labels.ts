import { QuestionType } from '../enums/question-type.enum';
import { TranslationKey } from '../i18n/translation.types';

// Translation keys for each type's name — render with t(QUESTION_TYPE_LABELS[type]).
export const QUESTION_TYPE_LABELS: Record<QuestionType, TranslationKey> = {
    [QuestionType.CustomPoll]: 'questionTypes.customPoll.label',
    [QuestionType.Superlative]: 'questionTypes.superlative.label',
    [QuestionType.Deathmatch]: 'questionTypes.deathmatch.label',
    [QuestionType.Scale]: 'questionTypes.scale.label',
    [QuestionType.SecretPairing]: 'questionTypes.secretPairing.label',
    [QuestionType.OpenText]: 'questionTypes.openText.label',
};
