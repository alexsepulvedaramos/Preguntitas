import { QuestionType } from '../enums/question-type.enum';

// Short explanation + example shown in a tooltip next to each type when creating a
// question — the type names alone aren't obvious to a first-time user (spec §5).
export const QUESTION_TYPE_DESCRIPTIONS: Record<QuestionType, string> = {
    [QuestionType.CustomPoll]:
        'Encuesta clásica con varias opciones a elegir. Ej: "¿A qué hora quedamos para cenar?"',
    [QuestionType.Superlative]:
        'Se elige a una persona del grupo. Ej: "¿Quién es más probable que acabe en la cárcel?"',
    [QuestionType.Deathmatch]:
        'Equipo contra equipo, el grupo vota qué bando gana. Ej: "Juan y Marta VS Luis y Ana"',
    [QuestionType.Scale]:
        'Se puntúa a alguien en una escala numérica. Ej: "Del 1 al 10, ¿cómo de dramático está hoy?"',
    [QuestionType.SecretPairing]:
        'Se eligen las dos personas que mejor pareja harían.',
    [QuestionType.OpenText]:
        'Pregunta abierta: cada uno escribe su propia respuesta. Ej: "¿Mejor recuerdo del viaje?"',
};
