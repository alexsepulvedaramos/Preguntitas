namespace VayaPreguntita.API.Entities;

public class GroupMember
{
    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsAdmin { get; set; } = false;
    public bool NotificationsMuted { get; set; } = false;

    // Shuffled selector rotation (§17.4): the last Group.SelectorRound this member was the
    // selector in. Members below the group's current round are still due a turn in it.
    public int LastSelectorRound { get; set; } = 0;

    // ==========================================
    // VOTING STREAK (rama 19) — per group, counted in daily cycles (§13)
    // ==========================================
    // CurrentStreak is only valid while LastStreakEntryId is the open entry or the
    // previously activated one — StreakService derives the effective value from that.
    public int CurrentStreak { get; set; } = 0;
    public int BestStreak { get; set; } = 0;
    public int? LastStreakEntryId { get; set; }

    // The open entry a "streak in danger" push was already sent for (dedupe).
    public int? StreakDangerNotifiedEntryId { get; set; }
}
