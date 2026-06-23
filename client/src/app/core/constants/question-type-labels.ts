import { QuestionType } from '../enums/question-type.enum';

export const QUESTION_TYPE_LABELS: Record<QuestionType, string> = {
    [QuestionType.CustomPoll]: 'Encuesta',
    [QuestionType.Superlative]: 'Superlativo',
    [QuestionType.Deathmatch]: 'Deathmatch',
    [QuestionType.Scale]: 'Escala',
    [QuestionType.SecretPairing]: 'Pareja secreta',
    [QuestionType.OpenText]: 'Respuesta abierta',
};
