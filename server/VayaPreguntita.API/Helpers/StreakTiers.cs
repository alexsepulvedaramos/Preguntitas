namespace VayaPreguntita.API.Helpers;

using VayaPreguntita.API.Localization;

// A player-facing text in every language the app is played in (rama 24).
public record LocalizedText(string Es, string En, string De)
{
    public string In(string language) =>
        language switch
        {
            AppLanguages.English => En,
            AppLanguages.German => De,
            _ => Es,
        };

    // In the language of the current request (Accept-Language).
    public override string ToString() => In(ContentTranslations.RequestLanguage);
}

// Key = frame material (drives the avatar frame); Titles = the unlockable title shown on
// the profile; Materials = the frame's display name. Title and Material read in the language
// of the current request.
public record StreakTier(string Key, LocalizedText Titles, LocalizedText Materials, int MinDays)
{
    public string Title => Titles.ToString();
    public string Material => Materials.ToString();
}

// Streak tiers and milestones (rama 19, §13). Mirrored on the client in
// core/constants/streak-tiers.ts — keep both in sync. Titles are adapted, not translated
// (rama 24): each language ends on its own nosiest neighbour.
public static class StreakTiers
{
    // Below the first tier (days 1–2) there is no frame and no title.
    public static readonly StreakTier[] All =
    [
        new("wood", new("Cotilla en prácticas", "Gossip in Training", "Tratsch-Azubi"), new("Madera", "Wood", "Holz"), 3),
        new("stone", new("Fuente anónima", "Anonymous Source", "Anonyme Quelle"), new("Piedra", "Stone", "Stein"), 7),
        new("bronze", new("Fuente bien informada", "Well-Informed Source", "Gut informierte Quelle"), new("Bronce", "Bronze", "Bronze"), 15),
        new("silver", new("Carne de tertulia", "Talk-Show Regular", "Talkshow-Dauergast"), new("Plata", "Silver", "Silber"), 30),
        new("gold", new("Oráculo del salseo", "Oracle of the Tea", "Orakel des Klatschs"), new("Oro", "Gold", "Gold"), 100),
        new("sapphire", new("Colaborador de Sálvame", "Tabloid Columnist", "Klatschpresse-Profi"), new("Zafiro", "Sapphire", "Saphir"), 182),
        new("amethyst", new("La Vieja del Visillo", "The Curtain Twitcher", "Die Gardinenspionin"), new("Amatista", "Amethyst", "Amethyst"), 365),
    ];

    // Extra celebrations that don't change the frame.
    public static readonly IReadOnlyDictionary<int, LocalizedText> Milestones = new Dictionary<int, LocalizedText>
    {
        [21] = new("¡3 semanas!", "3 weeks!", "3 Wochen!"),
        [50] = new("¡50 días!", "50 days!", "50 Tage!"),
        [200] = new("¡200 días!", "200 days!", "200 Tage!"),
        [300] = new("¡300 días!", "300 days!", "300 Tage!"),
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

    // SelectedTitleKey: null = automatic (highest unlocked), "none" = hidden, else a tier key
    // (checked when it is chosen).
    public const string NoTitle = "none";

    public static string? DisplayedTitle(int highestStreakEver, string? selectedTitleKey)
    {
        if (selectedTitleKey == NoTitle)
            return null;
        var unlocked = UnlockedBy(highestStreakEver).ToList();
        var chosen = unlocked.FirstOrDefault(t => t.Key == selectedTitleKey);
        return (chosen ?? unlocked.LastOrDefault())?.Title;
    }

    // The automatic chat message when a member reaches a tier or a milestone, in the reader's
    // language; null when that streak is neither.
    public static string? ChatMessage(string language, string username, int streak)
    {
        if (IsTierStart(streak))
        {
            var title = TierFor(streak)!.Titles.In(language);
            return language switch
            {
                AppLanguages.English => $"🔥 {username} is on a {streak}-day streak · {title}!",
                AppLanguages.German => $"🔥 {username} hat {streak} Tage in Folge abgestimmt · {title}!",
                _ => $"🔥 {username} lleva {streak} días seguidos · ¡{title}!",
            };
        }

        if (Milestones.TryGetValue(streak, out var label))
            return language switch
            {
                AppLanguages.English => $"🎉 {username} is on a {streak}-day streak · {label.In(language)}",
                AppLanguages.German => $"🎉 {username} hat {streak} Tage in Folge abgestimmt · {label.In(language)}",
                _ => $"🎉 {username} lleva {streak} días seguidos · {label.In(language)}",
            };

        return null;
    }
}
