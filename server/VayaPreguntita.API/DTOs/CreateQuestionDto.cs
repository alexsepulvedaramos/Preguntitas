using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.DTOs;

public class CreateQuestionDto
{
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }

    // Instead of a string Creator, we usually send the ID (or get it from the logged-in user token later)
    public int CreatorId { get; set; }
    public int GroupId { get; set; }

    // --- The Game Rules we added to the DB! ---
    public bool AllowNobody { get; set; }

    // For "The Superlative" blacklist
    public List<int> BlacklistedUserIds { get; set; } = [];

    // For "The Scale" limits
    public int? RangeMin { get; set; }
    public int? RangeMax { get; set; }
    public int? TargetUserId { get; set; }

    // For "The Secret Pairing" and Custom Poll selection rules
    public int? MinSelections { get; set; }
    public int? MaxSelections { get; set; }

    // For "The Deathmatch" teams
    public List<List<int>> Teams { get; set; } = [];

    public List<CreateOptionDto> Options { get; set; } = [];
}
