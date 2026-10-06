namespace VayaPreguntita.API.DTOs.Streaks;

// Returned with a successful vote so the client can play the streak celebration (rama 19).
public class StreakUpdateDto
{
    public int Previous { get; set; }
    public int Current { get; set; }
    public int Best { get; set; }

    // Current tier (null on days 1–2: no ring yet).
    public string? TierKey { get; set; }
    public string? TierName { get; set; }

    // True when this vote reached a new tier (days 3, 7, 30, 100, 182, 365).
    public bool IsTierUp { get; set; }

    // Set on milestone days (14, 50, 200, 300), e.g. "¡2 semanas!".
    public string? MilestoneLabel { get; set; }

    // Next tier to reach (null at the top tier).
    public string? NextTierKey { get; set; }
    public string? NextTierName { get; set; }
    public int? NextTierAt { get; set; }
    public int? DaysToNextTier { get; set; }
}
