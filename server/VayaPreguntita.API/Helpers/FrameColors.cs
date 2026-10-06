namespace VayaPreguntita.API.Helpers;

// Special User.FrameColor values besides a plain hex colour (rama 19).
public static class FrameColors
{
    public const string Streak = "streak";
    public const string None = "none";

    public static bool FollowsStreak(string? frameColor) =>
        frameColor is null or Streak;
}
