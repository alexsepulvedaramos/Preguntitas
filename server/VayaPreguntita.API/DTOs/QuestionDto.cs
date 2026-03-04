using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.DTOs;

public class QuestionDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Creator { get; set; } = string.Empty;
    public DateTime? DateAsked { get; set; }
    public QuestionType Type { get; set; }
    public List<AnswerDto> Answers { get; set; } = new();
}
