namespace VayaPreguntita.API.Entities;

public class QuestionMetadata
{
    // The Superlative
    public bool AllowNobody { get; set; }
    public List<int> BlacklistedUserIds { get; set; } = new();

    // The Scale
    public int? RangeMin { get; set; }
    public int? RangeMax { get; set; }
    public int? TargetUserId { get; set; }

    // The Secret Pairing
    public int MinSelections { get; set; } = 1;
    public int MaxSelections { get; set; } = 1;

    // The Deathmatch
    public List<Team> Teams { get; set; } = new();
}

public class Team
{
    public List<int> MemberIds { get; set; } = new();
}
