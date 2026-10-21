using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Nudges;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Services;

// Toque / nudge (rama 43, §17.10). It only goes to a member who hasn't voted an open question
// and can actually get it: a device subscribed to push, nudges allowed in their preferences,
// the group not muted, and no other nudge in this group in the last 24 h (from anyone).
public class NudgeService(
    AppDbContext context,
    ICoinService coinService,
    IInventoryService inventoryService,
    INotificationService notificationService,
    TimeProvider clock
) : INudgeService
{
    private sealed record OpenQuestion(int QuestionId, DateTime ClosesAt);

    public async Task<List<NudgeTargetDto>?> GetTargetsAsync(int groupId, int userId, int? flashId)
    {
        var open = await FindOpenQuestionAsync(groupId, flashId);
        if (open == null)
            return null;

        var voterIds = await context
            .Votes.Where(v => v.QuestionId == open.QuestionId)
            .Select(v => v.UserId)
            .Distinct()
            .ToListAsync();
        var pendingIds = await context
            .GroupMembers.Where(m => m.GroupId == groupId && m.UserId != userId && !voterIds.Contains(m.UserId))
            .Select(m => m.UserId)
            .ToListAsync();
        if (pendingIds.Count == 0)
            return [];

        var reachable = await ReachableAsync(groupId, pendingIds);
        var since = DailyClock.UtcNow(clock) - NudgeRules.Cooldown;
        var recent = await context
            .Nudges.Where(n => n.GroupId == groupId && pendingIds.Contains(n.TargetUserId) && n.SentAt > since)
            .Select(n => n.TargetUserId)
            .Distinct()
            .ToListAsync();

        return pendingIds
            .Select(id => new NudgeTargetDto
            {
                UserId = id,
                Status =
                    !reachable.Contains(id) ? NudgeTargetDto.Unreachable
                    : recent.Contains(id) ? NudgeTargetDto.Recent
                    : NudgeTargetDto.Available,
            })
            .ToList();
    }

    public async Task<(NudgeResult Result, SendNudgeResponseDto? Response)> SendAsync(
        int groupId,
        int userId,
        SendNudgeRequestDto dto
    )
    {
        var message = string.IsNullOrWhiteSpace(dto.Message) ? null : dto.Message.Trim();
        if (message?.Length > NudgeRules.MaxMessageLength)
            return (NudgeResult.MessageTooLong, null);

        var open = await FindOpenQuestionAsync(groupId, dto.FlashId);
        if (open == null)
            return (dto.FlashId == null ? NudgeResult.NoOpenQuestion : NudgeResult.FlashNotFound, null);

        var target = dto.TargetUserId;
        if (target == userId || !await context.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == target))
            return (NudgeResult.InvalidTarget, null);
        if (await context.Votes.AnyAsync(v => v.QuestionId == open.QuestionId && v.UserId == target))
            return (NudgeResult.AlreadyVoted, null);
        if (!(await ReachableAsync(groupId, [target])).Contains(target))
            return (NudgeResult.Unreachable, null);

        // Returning before the commit rolls everything back, the charge included.
        await using var transaction = await context.Database.BeginTransactionAsync();

        // Locks the target's membership so two members nudging at once can't both pass the
        // 24-hour check.
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"GroupMembers\" WHERE \"GroupId\" = {groupId} AND \"UserId\" = {target} FOR UPDATE"
        );
        var now = DailyClock.UtcNow(clock);
        var since = now - NudgeRules.Cooldown;
        if (await context.Nudges.AnyAsync(n => n.GroupId == groupId && n.TargetUserId == target && n.SentAt > since))
            return (NudgeResult.Recent, null);

        var nudge = new Nudge
        {
            GroupId = groupId,
            QuestionId = open.QuestionId,
            SenderUserId = userId,
            TargetUserId = target,
            Message = message,
            SentAt = now,
        };
        context.Nudges.Add(nudge);
        await context.SaveChangesAsync();

        if (dto.UseOwned)
        {
            if (!await inventoryService.TryConsumeAsync(userId, InventoryItem.Nudge))
                return (NudgeResult.NoneOwned, null);
        }
        else if (!await coinService.SpendAsync(userId, CoinReason.Nudge, CoinRewards.NudgePrice, groupId, nudge.Id))
            return (NudgeResult.NotEnoughCoins, null);

        await transaction.CommitAsync();

        var groupName = await context.Groups.Where(g => g.Id == groupId).Select(g => g.Name).FirstAsync();
        var sender = await context.Users.Where(u => u.Id == userId).Select(u => u.Username).FirstAsync();
        _ = notificationService.SendNudgeAsync(groupId, target, groupName, sender, message, open.ClosesAt - now);

        return (
            NudgeResult.Success,
            new SendNudgeResponseDto
            {
                Balance = await coinService.GetBalanceAsync(userId),
                Inventory = await inventoryService.GetAsync(userId),
            }
        );
    }

    // Today's open question (the latest activated entry, closing at the next T) or an open flash.
    // Never an anonymous flash (rama 50): its targets would tell who hasn't voted.
    private async Task<OpenQuestion?> FindOpenQuestionAsync(int groupId, int? flashId)
    {
        if (flashId is int id)
        {
            var nowUtc = DailyClock.UtcNow(clock);
            return await context
                .FlashEntries.Where(f => f.Id == id && f.GroupId == groupId && f.ClosesAt > nowUtc && !f.IsAnonymous)
                .Select(f => new OpenQuestion(f.QuestionId, f.ClosesAt))
                .FirstOrDefaultAsync();
        }

        var entry = await context
            .DailyEntries.Where(d => d.GroupId == groupId && d.ActivatedAt != null)
            .OrderByDescending(d => d.Date)
            .Select(d => new { d.QuestionId, d.Date, d.Group.DailyQuestionTime })
            .FirstOrDefaultAsync();
        return entry == null
            ? null
            : new OpenQuestion(entry.QuestionId, DailyClock.ToUtc(entry.Date.AddDays(1), entry.DailyQuestionTime));
    }

    // Members a nudge can reach: the group not muted, at least one push device, and no
    // preference row turning nudges off (no row = the defaults, nudges on).
    private async Task<HashSet<int>> ReachableAsync(int groupId, List<int> userIds) =>
        await context
            .GroupMembers.Where(m =>
                m.GroupId == groupId
                && userIds.Contains(m.UserId)
                && !m.NotificationsMuted
                && context.DevicePushSubscriptions.Any(s => s.UserId == m.UserId)
                && !context.NotificationPreferences.Any(p => p.UserId == m.UserId && !p.Nudge)
            )
            .Select(m => m.UserId)
            .ToHashSetAsync();
}
