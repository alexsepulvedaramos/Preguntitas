namespace VayaPreguntita.API.DTOs;

public class AnswerDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Creator { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
}
