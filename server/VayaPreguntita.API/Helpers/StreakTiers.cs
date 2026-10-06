namespace VayaPreguntita.API.Helpers;

// Key = frame material (drives the avatar frame); Title = the unlockable title shown on
// the profile; Material = the frame's display name.
public record StreakTier(string Key, string Title, string Material, int MinDays);

// Streak tiers and milestones (rama 19, §13). Mirrored on the client in
// core/constants/streak-tiers.ts — keep both in sync.
public static class StreakTiers
{
    // Below the first tier (days 1–2) there is no frame and no title.
    public static readonly StreakTier[] All =
    [
        new("wood", "Cotilla en prácticas", "Madera", 3),
        new("stone", "Fuente anónima", "Piedra", 7),
        new("bronze", "Fuente bien informada", "Bronce", 15),
        new("silver", "Carne de tertulia", "Plata", 30),
        new("gold", "Oráculo del salseo", "Oro", 100),
        new("sapphire", "Colaborador de Sálvame", "Zafiro", 182),
        new("amethyst", "La Vieja del Visillo", "Amatista", 365),
    ];

    // Extra celebrations that don't change the frame.
    public static readonly IReadOnlyDictionary<int, string> Milestones = new Dictionary<int, string>
    {
        [21] = "¡3 semanas!",
        [50] = "¡50 días!",
        [200] = "¡200 días!",
        [300] = "¡300 días!",
    };

    public static StreakTier? TierFor(int streak) =>
        All.LastOrDefault(t => streak >= t.MinDays);

    public static StreakTier? NextTier(int streak) =>
        All.FirstOrDefault(t => streak < t.MinDays);

    public static StreakTier? ByKey(string? key) => All.FirstOrDefault(t => t.Key == key);

    public static bool IsTierStart(int streak) => All.Any(t => t.MinDays == streak);

    public static bool IsMilestone(int streak) => Milestones.ContainsKey(streak);

    // Titles unlocked forever by the user's best streak ever (any group).
    public static IEnumerable<StreakTier> UnlockedBy(int highestStreakEver) =>
        All.Where(t => highestStreakEver >= t.MinDays);

    // SelectedTitleKey: null = automatic (highest unlocked), "none" = hidden, else a tier key.
    public const string NoTitle = "none";

    public static string? DisplayedTitle(int highestStreakEver, string? selectedTitleKey)
    {
        if (selectedTitleKey == NoTitle)
            return null;
        var unlocked = UnlockedBy(highestStreakEver).ToList();
        var chosen = unlocked.FirstOrDefault(t => t.Key == selectedTitleKey);
        return (chosen ?? unlocked.LastOrDefault())?.Title;
    }
}
