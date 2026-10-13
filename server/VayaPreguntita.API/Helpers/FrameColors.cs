namespace VayaPreguntita.API.Helpers;

// Special User.FrameColor values besides a plain hex colour (rama 19).
public static class FrameColors
{
    public const string Streak = "streak";
    public const string None = "none";

    // Seniority frame (rama 42, §17.10): free, by time in the group (outside one: the oldest).
    public const string Seniority = "seniority";

    public static bool FollowsStreak(string? frameColor) =>
        frameColor is null or Streak;
}
