using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.DTOs.Questions;

public class QuestionToVoteDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public bool AllowNobody { get; set; }
    public List<int> BlacklistedUserIds { get; set; } = [];
    public int? MinSelections { get; set; }
    public int? MaxSelections { get; set; }
    public int? RangeMin { get; set; }
    public int? RangeMax { get; set; }
    public int? TargetUserId { get; set; }
    public bool AllowOther { get; set; }
    public List<List<int>> Teams { get; set; } = [];

    public List<OptionDto> Options { get; set; } = [];
}
