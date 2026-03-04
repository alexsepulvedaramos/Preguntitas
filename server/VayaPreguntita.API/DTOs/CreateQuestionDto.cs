using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.DTOs;

public class CreateQuestionDto
{
    public string Text { get; set; } = string.Empty;
    public string Creator { get; set; } = string.Empty;
    public QuestionType Type { get; set; }

    public List<CreateAnswerDto> Answers { get; set; } = [];
}
