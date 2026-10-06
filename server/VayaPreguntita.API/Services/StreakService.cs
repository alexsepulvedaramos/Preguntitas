using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Notifications;
using VayaPreguntita.API.DTOs.Streaks;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Services;

public class StreakService(
    AppDbContext context,
    INotificationService notificationService,
    ILogger<StreakService> logger
) : IStreakService
{
    // Losing a 1-day streak isn't worth a notice.
    private const int MinLostStreakToNotify = 2;

    // Only members at or above the first ring get the "streak in danger" push.
    private const int MinStreakForDangerPush = 3;

    private sealed record EntryRow(int Id, int QuestionId, DateOnly Date, DateTime ActivatedAt, int SelectorUserId);

    // ==========================================
    // VOTE
    // ==========================================
    public async Task<StreakUpdateDto> RegisterVoteAsync(int groupId, int userId, DailyEntry openEntry)
    {
        var member = await context
            .GroupMembers.Include(m => m.User)
            .FirstAsync(m => m.GroupId == groupId && m.UserId == userId);

        // Defensive: the vote endpoint already rejects a second vote on the same question.
        if (member.LastStreakEntryId == openEntry.Id)
            return BuildUpdate(member.CurrentStreak, member.CurrentStreak, member.BestStreak);

        var previousEntryId = await GetPreviousActivatedEntryIdAsync(groupId, openEntry.Date);
        var previous = Effective(member, openEntry.Id, previousEntryId);
        var current = previous + 1;

        member.CurrentStreak = current;
        member.BestStreak = Math.Max(member.BestStreak, current);
        member.LastStreakEntryId = openEntry.Id;

        if (current >= StreakTiers.All[0].MinDays)
            ApplyStreakFrameOnce(member.User);

        var chatBody = BuildMilestoneChatMessage(member.User.Username, current);
        if (chatBody != null)
            context.ChatMessages.Add(
                new ChatMessage
                {
                    Body = chatBody,
                    DailyEntryId = openEntry.Id,
                    UserId = null,
                    CreatedAt = DateTime.UtcNow,
                }
            );

        await context.SaveChangesAsync();
        return BuildUpdate(previous, current, member.BestStreak);
    }

    // ==========================================
    // READS
    // ==========================================
    public async Task<Dictionary<int, int>> GetEffectiveStreaksAsync(int groupId)
    {
        var (openId, previousId) = await GetLastTwoActivatedEntryIdsAsync(groupId);
        var members = await context.GroupMembers.Where(m => m.GroupId == groupId).ToListAsync();
        return members.ToDictionary(m => m.UserId, m => Effective(m, openId, previousId));
    }

    public async Task<MyStreakDto> GetMyStreakAsync(int groupId, int userId)
    {
        var member = await context.GroupMembers.FirstOrDefaultAsync(m =>
            m.GroupId == groupId && m.UserId == userId
        );
        if (member == null)
            return new MyStreakDto();

        var (openId, previousId) = await GetLastTwoActivatedEntryIdsAsync(groupId);
        var current = Effective(member, openId, previousId);

        return new MyStreakDto
        {
            Current = current,
            Best = member.BestStreak,
            // The stored counter keeps the lost value until the notice is dismissed.
            LostStreak =
                current == 0 && member.CurrentStreak >= MinLostStreakToNotify
                    ? member.CurrentStreak
                    : null,
        };
    }

    public async Task DismissLostStreakAsync(int groupId, int userId)
    {
        var member = await context.GroupMembers.FirstOrDefaultAsync(m =>
            m.GroupId == groupId && m.UserId == userId
        );
        if (member == null)
            return;

        var (openId, previousId) = await GetLastTwoActivatedEntryIdsAsync(groupId);
        if (Effective(member, openId, previousId) > 0)
            return; // not lost — nothing to acknowledge

        member.CurrentStreak = 0;
        member.LastStreakEntryId = null;
        await context.SaveChangesAsync();
    }

    public async Task<MemberStatsDto?> GetMemberStatsAsync(int groupId, int userId)
    {
        var member = await context
            .GroupMembers.Include(m => m.User)
            .Include(m => m.Group)
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);
        if (member == null)
            return null;

        var entries = await LoadActivatedEntriesAsync(groupId);
        var votedQuestionIds = await context
            .Votes.Where(v => v.UserId == userId && v.Question.GroupId == groupId)
            .Select(v => v.QuestionId)
            .Distinct()
            .ToListAsync();
        var voted = votedQuestionIds.ToHashSet();

        // An entry counts towards participation if its cycle was still open when the member
        // joined. The open entry counts only once voted, so not having voted yet today
        // doesn't lower the percentage.
        int active = 0, votedActive = 0;
        for (var i = 0; i < entries.Count; i++)
        {
            var hasVoted = voted.Contains(entries[i].QuestionId);
            var isOpen = i == entries.Count - 1;
            var counts = isOpen ? hasVoted : entries[i + 1].ActivatedAt > member.JoinedAt;
            if (!counts)
                continue;
            active++;
            if (hasVoted)
                votedActive++;
        }

        var openId = entries.Count > 0 ? entries[^1].Id : (int?)null;
        var previousId = entries.Count > 1 ? entries[^2].Id : (int?)null;

        return new MemberStatsDto
        {
            GroupId = groupId,
            GroupName = member.Group.Name,
            UserId = userId,
            Username = member.User.Username,
            AvatarUrl = member.User.AvatarUrl,
            FrameColor = member.User.FrameColor,
            CurrentStreak = Effective(member, openId, previousId),
            BestStreak = member.BestStreak,
            TotalVotes = voted.Count,
            ActiveQuestions = active,
            ParticipationPercent = active == 0 ? 0 : (int)Math.Round(100.0 * votedActive / active),
            TimesSelector = entries.Count(e => e.SelectorUserId == userId),
            QuestionsCreated = await context.Questions.CountAsync(q =>
                q.GroupId == groupId && q.CreatorId == userId
            ),
        };
    }

    public async Task<List<MemberStatsDto>> GetUserGroupStatsAsync(int userId)
    {
        var groupIds = await context
            .GroupMembers.Where(m => m.UserId == userId)
            .OrderBy(m => m.JoinedAt)
            .Select(m => m.GroupId)
            .ToListAsync();

        var stats = new List<MemberStatsDto>();
        foreach (var groupId in groupIds)
        {
            var s = await GetMemberStatsAsync(groupId, userId);
            if (s != null)
                stats.Add(s);
        }
        return stats;
    }

    public async Task<int> GetHighestStreakAsync(int userId)
    {
        var memberships = await context.GroupMembers.Where(m => m.UserId == userId).ToListAsync();

        var highest = 0;
        foreach (var member in memberships.Where(m => m.CurrentStreak > highest))
        {
            var (openId, previousId) = await GetLastTwoActivatedEntryIdsAsync(member.GroupId);
            highest = Math.Max(highest, Effective(member, openId, previousId));
        }
        return highest;
    }

    // ==========================================
    // RECALCULATION / BACKFILL
    // ==========================================
    public async Task RecalculateMemberAsync(int groupId, int userId)
    {
        var member = await context.GroupMembers.FirstOrDefaultAsync(m =>
            m.GroupId == groupId && m.UserId == userId
        );
        if (member == null)
            return;

        var entries = await LoadActivatedEntriesAsync(groupId);
        var votedQuestionIds = (
            await context
                .Votes.Where(v => v.UserId == userId && v.Question.GroupId == groupId)
                .Select(v => v.QuestionId)
                .Distinct()
                .ToListAsync()
        ).ToHashSet();

        ApplyHistory(member, entries, votedQuestionIds);
        await context.SaveChangesAsync();
    }

    public async Task BackfillAsync()
    {
        // Members never computed: no streak ever recorded. Cheap to redo for members
        // without votes, so this needs no separate "already backfilled" marker.
        var pending = await context
            .GroupMembers.Where(m => m.BestStreak == 0 && m.LastStreakEntryId == null)
            .ToListAsync();

        foreach (var group in pending.GroupBy(m => m.GroupId))
        {
            var entries = await LoadActivatedEntriesAsync(group.Key);
            if (entries.Count == 0)
                continue;

            var userIds = group.Select(m => m.UserId).ToList();
            var votes = await context
                .Votes.Where(v => v.Question.GroupId == group.Key && userIds.Contains(v.UserId))
                .Select(v => new { v.UserId, v.QuestionId })
                .Distinct()
                .ToListAsync();
            var votesByUser = votes
                .GroupBy(v => v.UserId)
                .ToDictionary(g => g.Key, g => g.Select(v => v.QuestionId).ToHashSet());

            foreach (var member in group)
                ApplyHistory(member, entries, votesByUser.GetValueOrDefault(member.UserId) ?? []);
        }
        await context.SaveChangesAsync();

        // One-time switch to the streak frame for anyone already at ≥ 3 days.
        var candidates = await context
            .GroupMembers.Include(m => m.User)
            .Where(m => !m.User.StreakFrameAutoApplied && m.CurrentStreak >= StreakTiers.All[0].MinDays)
            .ToListAsync();

        foreach (var member in candidates)
        {
            if (member.User.StreakFrameAutoApplied)
                continue;
            var (openId, previousId) = await GetLastTwoActivatedEntryIdsAsync(member.GroupId);
            if (Effective(member, openId, previousId) >= StreakTiers.All[0].MinDays)
                ApplyStreakFrameOnce(member.User);
        }
        await context.SaveChangesAsync();

        logger.LogInformation(
            "Streak backfill: {Members} members computed, {Frames} frame candidates checked",
            pending.Count,
            candidates.Count
        );
    }

    // ==========================================
    // "STREAK IN DANGER" PUSH
    // ==========================================
    public async Task SendDangerRemindersAsync()
    {
        var nowUtc = DateTime.UtcNow;
        var maxLead = TimeSpan.FromHours(NotificationPreferencesDto.AllowedStreakDangerHours.Max());

        // Open entry per group = latest activated one. Only groups whose cycle closes
        // within the largest allowed lead time are candidates.
        var openEntries = await context
            .DailyEntries.Where(d => d.ActivatedAt != null)
            .GroupBy(d => d.GroupId)
            .Select(g => g.OrderByDescending(d => d.Date).First())
            .ToListAsync();
        var groupIds = openEntries.Select(e => e.GroupId).ToList();
        var groups = await context.Groups.Where(g => groupIds.Contains(g.Id)).ToDictionaryAsync(g => g.Id);

        foreach (var open in openEntries)
        {
            var group = groups[open.GroupId];
            var closesAt = DailyClock.ToUtc(open.Date.AddDays(1), group.DailyQuestionTime);
            if (nowUtc >= closesAt || closesAt - nowUtc > maxLead)
                continue;

            var previousId = await GetPreviousActivatedEntryIdAsync(open.GroupId, open.Date);
            if (previousId == null)
                continue;

            // Alive but not yet extended today: last counted entry is the previous one.
            var members = await context
                .GroupMembers.Where(m =>
                    m.GroupId == open.GroupId
                    && !m.NotificationsMuted
                    && m.LastStreakEntryId == previousId
                    && m.CurrentStreak >= MinStreakForDangerPush
                    && m.StreakDangerNotifiedEntryId != open.Id
                )
                .ToListAsync();
            if (members.Count == 0)
                continue;

            var userIds = members.Select(m => m.UserId).ToList();
            var prefs = await context
                .NotificationPreferences.Where(p => userIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId);

            foreach (var member in members)
            {
                var pref = prefs.GetValueOrDefault(member.UserId);
                if (pref is { StreakDanger: false })
                    continue;

                var lead = TimeSpan.FromHours(pref?.StreakDangerHoursBefore ?? 3);
                if (closesAt - nowUtc > lead)
                    continue;

                member.StreakDangerNotifiedEntryId = open.Id;
                await context.SaveChangesAsync();

                _ = notificationService.SendStreakDangerAsync(
                    open.GroupId,
                    member.UserId,
                    group.Name,
                    member.CurrentStreak,
                    group.DailyQuestionTime
                );
            }
        }
    }

    // ==========================================
    // HELPERS
    // ==========================================

    // A stored streak is alive only while its last counted entry is the open entry or
    // the one activated right before it; anything older means an active question closed
    // without the member's vote. Not-activated cycles never create entries, so frozen
    // days are skipped naturally.
    private static int Effective(GroupMember member, int? openId, int? previousId) =>
        member.LastStreakEntryId is int last && (last == openId || last == previousId)
            ? member.CurrentStreak
            : 0;

    // Walks the group's activated entries in order. The open entry can still be voted
    // on, so an unvoted open entry doesn't reset the streak.
    private static void ApplyHistory(GroupMember member, List<EntryRow> entries, HashSet<int> votedQuestionIds)
    {
        int current = 0, best = 0;
        int? last = null;

        for (var i = 0; i < entries.Count; i++)
        {
            if (votedQuestionIds.Contains(entries[i].QuestionId))
            {
                current++;
                best = Math.Max(best, current);
                last = entries[i].Id;
            }
            else if (i < entries.Count - 1)
            {
                current = 0;
                last = null;
            }
        }

        member.CurrentStreak = current;
        member.BestStreak = Math.Max(member.BestStreak, best);
        member.LastStreakEntryId = last;
    }

    private static void ApplyStreakFrameOnce(User user)
    {
        if (user.StreakFrameAutoApplied)
            return;
        user.StreakFrameAutoApplied = true;
        user.FrameColor = FrameColors.Streak;
    }

    private static string? BuildMilestoneChatMessage(string username, int streak)
    {
        if (StreakTiers.IsTierStart(streak))
            return $"🔥 {username} lleva {streak} días seguidos · ¡{StreakTiers.TierFor(streak)!.Name}!";
        if (StreakTiers.Milestones.TryGetValue(streak, out var label))
            return $"🎉 {username} lleva {streak} días seguidos · {label}";
        return null;
    }

    private static StreakUpdateDto BuildUpdate(int previous, int current, int best)
    {
        var tier = StreakTiers.TierFor(current);
        var next = StreakTiers.NextTier(current);
        return new StreakUpdateDto
        {
            Previous = previous,
            Current = current,
            Best = best,
            TierKey = tier?.Key,
            TierName = tier?.Name,
            IsTierUp = current > previous && StreakTiers.IsTierStart(current),
            MilestoneLabel = current > previous ? StreakTiers.Milestones.GetValueOrDefault(current) : null,
            NextTierKey = next?.Key,
            NextTierName = next?.Name,
            NextTierAt = next?.MinDays,
            DaysToNextTier = next == null ? null : next.MinDays - current,
        };
    }

    private async Task<List<EntryRow>> LoadActivatedEntriesAsync(int groupId) =>
        await context
            .DailyEntries.Where(d => d.GroupId == groupId && d.ActivatedAt != null)
            .OrderBy(d => d.Date)
            .Select(d => new EntryRow(d.Id, d.QuestionId, d.Date, d.ActivatedAt!.Value, d.SelectorUserId))
            .ToListAsync();

    private async Task<(int? openId, int? previousId)> GetLastTwoActivatedEntryIdsAsync(int groupId)
    {
        var ids = await context
            .DailyEntries.Where(d => d.GroupId == groupId && d.ActivatedAt != null)
            .OrderByDescending(d => d.Date)
            .Select(d => d.Id)
            .Take(2)
            .ToListAsync();
        return (ids.Count > 0 ? ids[0] : null, ids.Count > 1 ? ids[1] : null);
    }

    private async Task<int?> GetPreviousActivatedEntryIdAsync(int groupId, DateOnly openDate) =>
        await context
            .DailyEntries.Where(d => d.GroupId == groupId && d.ActivatedAt != null && d.Date < openDate)
            .OrderByDescending(d => d.Date)
            .Select(d => (int?)d.Id)
            .FirstOrDefaultAsync();
}
