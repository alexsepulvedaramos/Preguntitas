using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.DTOs;

public class QuestionToVoteDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public bool AllowNobody { get; set; }
    public int MaxSelections { get; set; }
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; }

    public List<OptionDto> Options { get; set; } = [];
}
