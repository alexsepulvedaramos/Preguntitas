namespace VayaPreguntita.API.DTOs;

public class QuestionResultDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Creator { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }
    public int TotalVotes { get; set; }
    public List<OptionResultDto> Results { get; set; } = [];
}
