namespace VayaPreguntita.API.Helpers;

// User.FrameColor values: a palette colour key (§14 visual rules — the client draws each with a
// light and a dark variant) or one of the special frames below (rama 19).
public static class FrameColors
{
    public const string Streak = "streak";
    public const string None = "none";

    // Seniority frame (rama 42, §17.10): free, by time in the group (outside one: the oldest).
    public const string Seniority = "seniority";

    public static bool FollowsStreak(string? frameColor) =>
        frameColor is null or Streak;

    public static readonly IReadOnlyList<string> PaletteKeys =
        ["purple", "blue", "green", "yellow", "orange", "red", "pink"];

    // The hex values the profile offered before the palette keys; old clients may still send
    // them, and the AddLastSeenAndFrameKeys migration converted the stored ones the same way.
    public static readonly IReadOnlyDictionary<string, string> LegacyHex = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["#8b5cf6"] = "purple",
        ["#3b82f6"] = "blue",
        ["#22c55e"] = "green",
        ["#eab308"] = "yellow",
        ["#f97316"] = "orange",
        ["#ef4444"] = "red",
        ["#ec4899"] = "pink",
    };
}
