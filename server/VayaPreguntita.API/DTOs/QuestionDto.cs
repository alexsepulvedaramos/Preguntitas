using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.DTOs;

public class QuestionDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;

    // The Creator field is secret till we get the option, so we won't include it in the QuestionDto for now
    public DateTime? DateAsked { get; set; }
    public QuestionType Type { get; set; }
    public List<OptionDto> Options { get; set; } = [];
}
