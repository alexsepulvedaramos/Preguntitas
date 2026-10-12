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

    // ==========================================
    // STREAK FREEZE & REPAIR (rama 35, §17.9)
    // ==========================================
    // Freezes held in this group (0–StreakFreezeRules.MaxHeld). The close-of-cycle tick
    // spends one when the member misses an active cycle.
    public int StreakFreezes { get; set; } = 0;

    // Local month (yyyyMM) the free monthly freeze was last granted for. Granted lazily.
    public int LastFreeFreezeMonth { get; set; } = 0;

    // The closed entry a freeze covered, until the "your freeze saved your streak" notice
    // is dismissed.
    public int? FreezeUsedEntryId { get; set; }

    // Who last gave this member a freeze, until the gift notice is dismissed.
    public int? FreezeGiftFromUserId { get; set; }

    // A streak lost without a freeze: its length and the missed entry. It can be repaired
    // while that entry is the last closed one, at most once per month (LastRepairMonth, yyyyMM).
    public int? LostStreakValue { get; set; }
    public int? LostStreakEntryId { get; set; }
    public int LastRepairMonth { get; set; } = 0;
}
