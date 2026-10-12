using VayaPreguntita.API.Entities;

namespace VayaPreguntita.API.Helpers;

// Streak freeze and repair (rama 35, §17.9).
public static class StreakFreezeRules
{
    public const int MaxHeld = 2;
    public const int FreezePrice = 100;
    public const int RepairPrice = 300;

    // Losing a 1-day streak isn't worth a notice, nor a repair.
    public const int MinLostStreak = 2;

    // Local month as yyyyMM — the unit of the free freeze and the repair limit.
    public static int MonthKey(DateOnly localDay) => localDay.Year * 100 + localDay.Month;

    // One free freeze per month per group, granted lazily: whenever freezes are read or
    // evaluated. Holding the max already forfeits that month's free one. Returns whether
    // the member changed.
    public static bool GrantMonthlyFreeze(GroupMember member, int monthKey)
    {
        if (member.LastFreeFreezeMonth >= monthKey)
            return false;
        member.LastFreeFreezeMonth = monthKey;
        if (member.StreakFreezes < MaxHeld)
            member.StreakFreezes++;
        return true;
    }

    // What the member will hold once this month's free freeze is granted, without granting it.
    public static int FreezesIncludingMonthly(GroupMember member, int monthKey) =>
        member.LastFreeFreezeMonth < monthKey
            ? Math.Min(MaxHeld, member.StreakFreezes + 1)
            : member.StreakFreezes;
}
