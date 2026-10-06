namespace VayaPreguntita.API.Helpers;

public record StreakTier(string Key, string Name, int MinDays);

// Streak ring tiers and milestones (rama 19, §13). Mirrored on the client in
// core/constants/streak-tiers.ts — keep both in sync.
public static class StreakTiers
{
    // Below the first tier (days 1–2) there is no ring.
    public static readonly StreakTier[] All =
    [
        new("ember", "Brasa", 3),
        new("flame-small", "Llama pequeña", 7),
        new("flame-intense", "Llama intensa", 30),
        new("flame-blue", "Llama azul", 100),
        new("flame-purple", "Llama morada", 182),
        new("flame-gold", "Llama dorada", 365),
    ];

    // Extra celebrations that don't change the ring.
    public static readonly IReadOnlyDictionary<int, string> Milestones = new Dictionary<int, string>
    {
        [14] = "¡2 semanas!",
        [50] = "¡50 días!",
        [200] = "¡200 días!",
        [300] = "¡300 días!",
    };

    public static StreakTier? TierFor(int streak) =>
        All.LastOrDefault(t => streak >= t.MinDays);

    public static StreakTier? NextTier(int streak) =>
        All.FirstOrDefault(t => streak < t.MinDays);

    public static bool IsTierStart(int streak) => All.Any(t => t.MinDays == streak);

    public static bool IsMilestone(int streak) => Milestones.ContainsKey(streak);
}
