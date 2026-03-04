using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Entities;

public class Question
{
    public int Id { get; set; }

    public required string Text { get; set; }

    public string Creator { get; set; } = null!;

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    public DateTime? DateAsked { get; set; }

    public List<Answer> Answers { get; set; } = [];
    public QuestionType Type { get; set; } = QuestionType.FreeText;
}
