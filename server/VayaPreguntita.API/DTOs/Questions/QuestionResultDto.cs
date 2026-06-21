// QuestionResultDto.cs
using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.DTOs.Questions;

public class QuestionResultDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public DateTime DateCreated { get; set; }
    public int TotalVotes { get; set; }
    public List<OptionResultDto> Results { get; set; } = [];
}
