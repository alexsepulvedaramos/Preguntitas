// DTOs/Daily/DailyStatusDto.cs
// Lo que devuelve GET /daily/current — el frontend decide qué renderizar con esto

using VayaPreguntita.API.DTOs.Questions;

namespace VayaPreguntita.API.DTOs.Daily;

public class DailyStatusDto
{
    // "voting" | "results" | "pending_selection" | "no_question"
    public string Status { get; set; } = string.Empty;

    // A quién le toca elegir hoy
    public int SelectorUserId { get; set; }
    public string SelectorUsername { get; set; } = string.Empty;
    public bool IsCurrentUserSelector { get; set; }

    // La pregunta (null si status es "no_question")
    public QuestionToVoteDto? Question { get; set; }

    // Solo cuando status es "results"
    public QuestionResultDto? Results { get; set; }
}
