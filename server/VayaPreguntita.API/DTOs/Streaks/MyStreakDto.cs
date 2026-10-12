namespace VayaPreguntita.API.DTOs.Streaks;

// The current user's streak in a group, surfaced on GET daily/current.
public class MyStreakDto
{
    public int Current { get; set; }
    public int Best { get; set; }

    // Length of a streak that was lost and not acknowledged yet — drives the
    // "Has perdido tu racha…" notice. null when there's nothing to show.
    public int? LostStreak { get; set; }

    // ── Streak freeze & repair (rama 35, §17.9) ──
    public int Freezes { get; set; }
    public int MaxFreezes { get; set; }

    // The streak a freeze just saved — drives "Tu congelación salvó tu racha…". null: no notice.
    public int? FreezeSavedStreak { get; set; }

    // Username of whoever last gave you a freeze, until the notice is dismissed.
    public string? FreezeGiftFrom { get; set; }

    // The streak a repair would restore; null when no repair is available right now.
    public int? RepairableStreak { get; set; }

    public int FreezePrice { get; set; }
    public int RepairPrice { get; set; }
}
