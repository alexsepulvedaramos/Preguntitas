namespace VayaPreguntita.API.DTOs.Streaks;

// The current user's streak in a group, surfaced on GET daily/current.
public class MyStreakDto
{
    public int Current { get; set; }
    public int Best { get; set; }

    // Length of a streak that was lost and not acknowledged yet — drives the
    // "Has perdido tu racha…" notice. null when there's nothing to show.
    public int? LostStreak { get; set; }
}
