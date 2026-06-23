using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.DTOs.Questions;

public class QuestionDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public bool IsUsed { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime? DateActivated { get; set; }
    public int? CreatorId { get; set; }
    public List<OptionDto> Options { get; set; } = [];
}
