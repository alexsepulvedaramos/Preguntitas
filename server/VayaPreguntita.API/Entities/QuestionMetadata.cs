namespace VayaPreguntita.API.Entities;

public class QuestionMetadata
{
    // The Superlative
    public bool AllowNobody { get; set; }
    public List<int> BlacklistedUserIds { get; set; } = [];

    // The Scale — always rated 1–10 (§5.3); the bounds stay stored so clients read them as data.
    public const int ScaleMin = 1;
    public const int ScaleMax = 10;

    public int? RangeMin { get; set; }
    public int? RangeMax { get; set; }
    public int? TargetUserId { get; set; }

    // The Secret Pairing
    public int MinSelections { get; set; } = 1;
    public int MaxSelections { get; set; } = 1;

    // The Custom Poll — allow voters to add their own free-text answer ("Otro")
    public bool AllowOther { get; set; }

    // The Deathmatch
    public List<List<int>> Teams { get; set; } = [];
}
