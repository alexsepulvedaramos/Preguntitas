import { QuestionType } from '../enums/question-type.enum';
import { TranslationKey } from '../i18n/translation.types';

// Short explanation + example shown in a tooltip next to each type when creating a
// question — the type names alone aren't obvious to a first-time user (spec §5).
// Translation keys: render with t(QUESTION_TYPE_DESCRIPTIONS[type]).
export const QUESTION_TYPE_DESCRIPTIONS: Record<QuestionType, TranslationKey> = {
    [QuestionType.CustomPoll]: 'questionTypes.customPoll.description',
    [QuestionType.Superlative]: 'questionTypes.superlative.description',
    [QuestionType.Deathmatch]: 'questionTypes.deathmatch.description',
    [QuestionType.Scale]: 'questionTypes.scale.description',
    [QuestionType.SecretPairing]: 'questionTypes.secretPairing.description',
    [QuestionType.OpenText]: 'questionTypes.openText.description',
};
