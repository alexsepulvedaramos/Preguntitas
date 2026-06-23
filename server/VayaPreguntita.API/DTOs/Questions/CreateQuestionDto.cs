using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.DTOs.Questions;

public class CreateQuestionDto
{
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }

    // Superlative
    public bool AllowNobody { get; set; }
    public List<int> BlacklistedUserIds { get; set; } = [];

    // Scale
    public int? RangeMin { get; set; }
    public int? RangeMax { get; set; }
    public int? TargetUserId { get; set; }

    // SecretPairing + CustomPoll
    public int? MinSelections { get; set; }
    public int? MaxSelections { get; set; }

    // CustomPoll — let voters add their own free-text answer
    public bool AllowOther { get; set; }

    // Deathmatch
    public List<List<int>> Teams { get; set; } = [];

    // CustomPoll
    public List<CreateOptionDto> Options { get; set; } = [];
}
