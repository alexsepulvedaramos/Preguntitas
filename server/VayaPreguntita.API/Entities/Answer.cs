namespace VayaPreguntita.API.Entities;

public class Answer
{
    public int Id { get; set; }
    public int QuestionId { get; set; }

    public Question Question { get; set; } = null!;

    public required string Text { get; set; }

    public string? Creator { get; set; }

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}
