namespace VayaPreguntita.API.DTOs.Streaks;

// Returned with a successful vote so the client can play the streak celebration (rama 19).
public class StreakUpdateDto
{
    public int Previous { get; set; }
    public int Current { get; set; }
    public int Best { get; set; }

    // Current tier (null on days 1–2: no frame yet).
    public string? TierKey { get; set; }
    public string? TierTitle { get; set; }
    public string? TierMaterial { get; set; }

    // True when this vote reached a new tier (days 3, 7, 15, 30, 100, 182, 365).
    public bool IsTierUp { get; set; }

    // True when the tier's title was unlocked for the first time ever.
    public bool TitleUnlocked { get; set; }

    // Set on milestone days (21, 50, 200, 300), e.g. "¡3 semanas!".
    public string? MilestoneLabel { get; set; }

    // Next tier to reach (null at the top tier).
    public string? NextTierKey { get; set; }
    public string? NextTierTitle { get; set; }
    public string? NextTierMaterial { get; set; }
    public int? NextTierAt { get; set; }
    public int? DaysToNextTier { get; set; }
}
