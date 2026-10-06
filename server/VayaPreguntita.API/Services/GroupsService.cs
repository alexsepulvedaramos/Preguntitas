using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Groups;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Services;

public class GroupsService(
    AppDbContext context,
    IMapper mapper,
    IDailyService dailyService,
    IStreakService streakService
) : IGroupsService
{
    public async Task<bool> IsUserInGroupAsync(int userId, int groupId)
    {
        return await context.GroupMembers.AnyAsync(gm =>
            gm.GroupId == groupId && gm.UserId == userId);
    }

    public async Task<bool> IsUserAdminAsync(int userId, int groupId)
    {
        return await context.GroupMembers.AnyAsync(gm =>
            gm.GroupId == groupId && gm.UserId == userId && gm.IsAdmin);
    }

    public async Task<IEnumerable<GroupResponse>> GetUserGroupsAsync(int userId)
    {
        var groups = await context.Groups
            .Where(g => g.Members.Any(m => m.UserId == userId))
            .Include(g => g.Creator)
            .ToListAsync();

        if (groups.Count == 0)
            return [];

        var groupIds = groups.Select(g => g.Id).ToList();

        // Latest activated entry per group (the currently open question)
        var openEntries = await context.DailyEntries
            .Where(d => groupIds.Contains(d.GroupId) && d.ActivatedAt != null)
            .GroupBy(d => d.GroupId)
            .Select(g => g.OrderByDescending(d => d.Date).First())
            .ToListAsync();

        var openQuestionIds = openEntries.Select(e => e.QuestionId).Distinct().ToList();

        // Which open questions has this user already voted on?
        var votedQuestionIds = openQuestionIds.Count > 0
            ? await context.Votes
                .Where(v => openQuestionIds.Contains(v.QuestionId) && v.UserId == userId)
                .Select(v => v.QuestionId)
                .ToListAsync()
            : new List<int>();

        // Pending (not yet activated) entries where this user is the selector
        var selectorGroupIds = await context.DailyEntries
            .Where(d => groupIds.Contains(d.GroupId) && d.ActivatedAt == null && d.SelectorUserId == userId)
            .Select(d => d.GroupId)
            .ToListAsync();

        var openByGroup = openEntries.ToDictionary(e => e.GroupId);
        var votedSet = new HashSet<int>(votedQuestionIds);
        var selectorSet = new HashSet<int>(selectorGroupIds);

        return groups.Select(g =>
        {
            var dto = mapper.Map<GroupResponse>(g);
            var openEntry = openByGroup.GetValueOrDefault(g.Id);

            dto.DailyStatus = openEntry switch
            {
                null => "no_question",
                _ when !votedSet.Contains(openEntry.QuestionId) => "voting",
                _ when selectorSet.Contains(g.Id) => "selector",
                _ => "results",
            };

            return dto;
        });
    }

    public async Task<GroupResponse> CreateGroupAsync(CreateGroupRequest request, int userId)
    {
        var invitationCode = GenerateRandomCode(6);

        var newGroup = new Group
        {
            CreatorId = userId,
            Name = request.Name,
            Description = request.Description,
            DailyQuestionTime = request.DailyQuestionTime,
            InvitationCode = invitationCode,
            Members = [new GroupMember { UserId = userId, JoinedAt = DateTime.UtcNow, IsAdmin = true }],
        };

        context.Groups.Add(newGroup);
        await context.SaveChangesAsync();

        // Pre-disable any default-off packs (Pack.DisabledByDefault) for the new group; an admin
        // can still enable them from settings.
        await PackSeeder.ApplyDefaultDisabledPacksAsync(context, newGroup.Id);

        await context.Entry(newGroup).Reference(g => g.Creator).LoadAsync();

        return mapper.Map<GroupResponse>(newGroup);
    }

    public async Task<GroupResponse?> GetGroupAsync(int groupId)
    {
        var group = await context
            .Groups.Include(g => g.Creator)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        return group == null ? null : mapper.Map<GroupResponse>(group);
    }

    public async Task<GroupResponse?> UpdateGroupAsync(UpdateGroupRequest request, int groupId)
    {
        var group = await context
            .Groups.Include(g => g.Creator)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
            return null;

        var timeChanged = group.DailyQuestionTime != request.DailyQuestionTime;

        group.Name = request.Name;
        group.Description = request.Description;
        group.DailyQuestionTime = request.DailyQuestionTime;

        context.Groups.Update(group);
        await context.SaveChangesAsync();

        var response = mapper.Map<GroupResponse>(group);

        // If the time changed but today's question has already activated, the new time only
        // takes effect next cycle (§4.8) — today's slot is already consumed, so we neither
        // re-anchor nor close it early. Surface that so the UI can inform the admin.
        // "Activated today" is measured off ActivatedAt within the local (UTC+2) day, not the
        // entry's Date, so the bootstrap entry (dated one day earlier, §4.1) still counts.
        if (timeChanged)
        {
            var today = DailyClock.Today();
            var todayStartUtc = DailyClock.ToUtc(today, TimeOnly.MinValue);
            var tomorrowStartUtc = DailyClock.ToUtc(today.AddDays(1), TimeOnly.MinValue);

            response.DailyTimeChangeAppliesFromTomorrow = await context.DailyEntries.AnyAsync(d =>
                d.GroupId == groupId
                && d.ActivatedAt != null
                && d.ActivatedAt >= todayStartUtc
                && d.ActivatedAt < tomorrowStartUtc
            );
        }

        return response;
    }

    public async Task<bool> TransferAdminAsync(int groupId, int newAdminId)
    {
        var members = await context.GroupMembers
            .Where(gm => gm.GroupId == groupId && (gm.IsAdmin || gm.UserId == newAdminId))
            .ToListAsync();

        var newAdmin = members.FirstOrDefault(m => m.UserId == newAdminId);
        if (newAdmin == null) return false;

        foreach (var m in members) m.IsAdmin = false;
        newAdmin.IsAdmin = true;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool found, bool alreadyMember)> JoinGroupAsync(int userId, string invitationCode)
    {
        var group = await context
            .Groups.Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.InvitationCode == invitationCode);

        if (group == null)
            return (false, false);

        if (group.Members.Any(m => m.UserId == userId))
            return (true, true);

        var userExists = await context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
            return (false, false);

        group.Members.Add(new GroupMember { UserId = userId, JoinedAt = DateTime.UtcNow, IsAdmin = false });
        await context.SaveChangesAsync();

        // A member who left and comes back gets their streak rebuilt from vote history
        // (cycles spent outside count as missed, so the record survives but the streak doesn't).
        await streakService.RecalculateMemberAsync(group.Id, userId);

        // Seed the group's first DailyEntry once it reaches 2 members. Bootstrap exception to
        // the "daily time is sacred" rule (§4.1): the very first question activates
        // immediately so members aren't left waiting up to 24h with nothing to do, but it
        // still closes at the next real T (today's T if it hasn't passed yet, otherwise
        // tomorrow's) — not a full 24h later — so the normal cadence resumes at the very
        // next T instead of skipping an entire cycle. It's dated one day *before* that T so
        // it doesn't collide with the regularly-queued entry, which takes the real T date;
        // ClosesAt/ActivatesAt (Date+1@T) and CalculateSelector both work unmodified off that.
        if (group.Members.Count == 2)
        {
            var today = DailyClock.Today();
            var hasEntries = await context.DailyEntries.AnyAsync(d => d.GroupId == group.Id);

            if (!hasEntries)
            {
                var nextActivation =
                    DailyClock.TimeOfDay() < group.DailyQuestionTime ? today : today.AddDays(1);
                var bootstrapDate = nextActivation.AddDays(-1);

                await dailyService.PreselectForGroupAsync(
                    group.Id,
                    bootstrapDate,
                    activateImmediately: true
                );
                await dailyService.PreselectForGroupAsync(group.Id, nextActivation);
            }
        }

        return (true, false);
    }

    public async Task<IEnumerable<GroupMemberDto>> GetGroupMembersAsync(int groupId, int currentUserId)
    {
        var members = await context
            .GroupMembers.Where(gm => gm.GroupId == groupId)
            .OrderBy(gm => gm.JoinedAt)
            .ProjectTo<GroupMemberDto>(mapper.ConfigurationProvider)
            .ToListAsync();

        var streaks = await streakService.GetEffectiveStreaksAsync(groupId);
        var crownHolder = await streakService.GetCrownHolderAsync(groupId);
        var memberIds = members.Select(m => m.Id).ToList();
        var titles = await context
            .Users.Where(u => memberIds.Contains(u.Id))
            .Select(u => new { u.Id, u.HighestStreakEver, u.SelectedTitleKey })
            .ToDictionaryAsync(u => u.Id);
        foreach (var m in members)
        {
            m.IsCurrentUser = m.Id == currentUserId;
            m.CurrentStreak = streaks.GetValueOrDefault(m.Id);
            m.HasCrown = m.Id == crownHolder;
            if (titles.TryGetValue(m.Id, out var t))
                m.Title = StreakTiers.DisplayedTitle(t.HighestStreakEver, t.SelectedTitleKey);
        }

        return members;
    }

    public async Task<bool> LeaveGroupAsync(int userId, int groupId)
    {
        var group = await context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null) return false;

        var member = group.Members.FirstOrDefault(m => m.UserId == userId);
        if (member == null) return false;

        if (group.Members.Count == 1)
        {
            context.Groups.Remove(group);
            await context.SaveChangesAsync();
            return true;
        }

        bool wasAdmin = member.IsAdmin;
        group.Members.Remove(member);

        if (wasAdmin)
        {
            var newAdmin = group.Members.OrderBy(m => m.JoinedAt).First();
            newAdmin.IsAdmin = true;
        }

        context.Groups.Update(group);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> KickMemberAsync(int groupId, int targetUserId)
    {
        var group = await context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null) return false;

        var member = group.Members.FirstOrDefault(m => m.UserId == targetUserId);
        if (member == null) return false;

        // Admin cannot be kicked — use transfer-admin first
        if (member.IsAdmin) return false;

        group.Members.Remove(member);
        context.Groups.Update(group);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<GroupResponse?> RegenerateInviteCodeAsync(int groupId)
    {
        var group = await context.Groups
            .Include(g => g.Creator)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null) return null;

        group.InvitationCode = GenerateRandomCode(6);
        context.Groups.Update(group);
        await context.SaveChangesAsync();
        return mapper.Map<GroupResponse>(group);
    }

    public async Task<InvitePreviewDto?> GetInvitePreviewAsync(string invitationCode)
    {
        var group = await context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.InvitationCode == invitationCode);

        if (group == null) return null;

        return new InvitePreviewDto(group.Id, group.Name, group.Description ?? string.Empty, group.Members.Count);
    }

    private static string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(
            Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray()
        );
    }
}
