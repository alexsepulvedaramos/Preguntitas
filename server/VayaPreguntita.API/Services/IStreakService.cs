using VayaPreguntita.API.DTOs.Streaks;
using VayaPreguntita.API.Entities;

namespace VayaPreguntita.API.Services;

// Per-group voting streaks (rama 19, §13). The unit is the daily cycle: a streak grows
// with each activated daily question the member votes on and resets only when an
// activated question closes without their vote. Cycles with no active question
// (group below 2 members) leave it frozen.
public interface IStreakService
{
    // Called right after a vote is stored. Returns the before/after streak for the celebration.
    Task<StreakUpdateDto> RegisterVoteAsync(int groupId, int userId, DailyEntry openEntry);

    // userId → effective current streak, for every member of the group.
    Task<Dictionary<int, int>> GetEffectiveStreaksAsync(int groupId);

    Task<MyStreakDto> GetMyStreakAsync(int groupId, int userId);

    // Acknowledges the "lost streak" notice so it isn't shown again.
    Task DismissLostStreakAsync(int groupId, int userId);

    Task<MemberStatsDto?> GetMemberStatsAsync(int groupId, int userId);

    // The user's own stats in each of their groups (profile card).
    Task<List<MemberStatsDto>> GetUserGroupStatsAsync(int userId);

    // Highest current streak across the user's groups (ring outside a group).
    Task<int> GetHighestStreakAsync(int userId);

    // Rebuilds a member's streak from vote history (rejoin after leaving).
    Task RecalculateMemberAsync(int groupId, int userId);

    // Startup backfill: computes streaks from history for members that were never
    // computed, and applies the one-time switch to the streak frame.
    Task BackfillAsync();

    // Background tick: "streak in danger" pushes before the cycle closes.
    Task SendDangerRemindersAsync();
}
