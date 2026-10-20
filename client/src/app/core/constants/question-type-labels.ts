import { QuestionType } from '../enums/question-type.enum';
import { TranslationKey } from '../i18n/translation.types';
import { GlyphName } from '../../shared/components/glyph/glyphs';

// Translation keys for each type's name — render with t(QUESTION_TYPE_LABELS[type]).
export const QUESTION_TYPE_LABELS: Record<QuestionType, TranslationKey> = {
    [QuestionType.CustomPoll]: 'questionTypes.customPoll.label',
    [QuestionType.Superlative]: 'questionTypes.superlative.label',
    [QuestionType.Deathmatch]: 'questionTypes.deathmatch.label',
    [QuestionType.Scale]: 'questionTypes.scale.label',
    [QuestionType.SecretPairing]: 'questionTypes.secretPairing.label',
    [QuestionType.OpenText]: 'questionTypes.openText.label',
};

// Each type's glyph (shared/components/glyph/glyphs.ts) — shown on the type chip and the
// type picker so a type is recognisable without a colour of its own.
export const QUESTION_TYPE_GLYPH: Record<QuestionType, GlyphName> = {
    [QuestionType.CustomPoll]: 'poll',
    [QuestionType.Superlative]: 'crown',
    [QuestionType.Deathmatch]: 'versus',
    [QuestionType.Scale]: 'ruler',
    [QuestionType.SecretPairing]: 'match',
    [QuestionType.OpenText]: 'write',
};
