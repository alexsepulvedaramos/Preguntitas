using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Chat;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Localization;

namespace VayaPreguntita.API.Services;

public class ChatService(AppDbContext context, INotificationService notificationService) : IChatService
{
    private const int MaxMessagesPerUserPerDay = 10;

    public async Task<IEnumerable<ChatMessageDto>?> GetMessagesAsync(int groupId, int userId)
    {
        var isMember = await context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
        if (!isMember) return null;

        var entry = await GetOpenEntryAsync(groupId);
        if (entry == null) return [];

        return await BuildMessageDtosAsync(entry.Id, userId);
    }

    public async Task<(bool success, string? error, IEnumerable<ChatMessageDto>? messages)> SendMessageAsync(
        int groupId,
        int userId,
        SendChatMessageDto dto
    )
    {
        var isMember = await context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
        if (!isMember) return (false, ServerText.NotAGroupMember, null);

        var entry = await GetOpenEntryAsync(groupId);
        if (entry == null) return (false, ServerText.NoActiveQuestionInGroup, null);

        var count = await context.ChatMessages
            .CountAsync(m => m.DailyEntryId == entry.Id && m.UserId == userId);
        if (count >= MaxMessagesPerUserPerDay)
            return (false, ServerText.ChatLimitReached(MaxMessagesPerUserPerDay), null);

        var message = new ChatMessage
        {
            Body = dto.Body.Trim(),
            DailyEntryId = entry.Id,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
        };
        context.ChatMessages.Add(message);
        await context.SaveChangesAsync();

        var group = await context.Groups.FindAsync(groupId);
        if (group != null)
        {
            var sender = await context.Users.FindAsync(userId);
            _ = notificationService.SendNewMessageAsync(
                groupId, userId, group.Name, sender?.Username ?? "Alguien", dto.Body.Trim());
        }

        return (true, null, await BuildMessageDtosAsync(entry.Id, userId));
    }

    private async Task<DailyEntry?> GetOpenEntryAsync(int groupId) =>
        await context.DailyEntries
            .Where(d => d.GroupId == groupId && d.ActivatedAt != null)
            .OrderByDescending(d => d.Date)
            .FirstOrDefaultAsync();

    private async Task<IEnumerable<ChatMessageDto>> BuildMessageDtosAsync(int dailyEntryId, int currentUserId)
    {
        var messages = await context.ChatMessages
            .Where(m => m.DailyEntryId == dailyEntryId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new
            {
                Dto = new ChatMessageDto
                {
                    Id = m.Id,
                    Body = m.Body,
                    CreatedAt = m.CreatedAt,
                    UserId = m.UserId,
                    Username = m.User != null ? m.User.Username : null,
                    IsSystem = m.UserId == null,
                    IsCurrentUser = m.UserId == currentUserId,
                },
                m.StreakUsername,
                m.StreakDays,
            })
            .ToListAsync();

        // Streak messages are written in the reader's language (rama 24).
        var language = ContentTranslations.RequestLanguage;
        foreach (var m in messages.Where(m => m.StreakUsername != null && m.StreakDays != null))
            m.Dto.Body = StreakTiers.ChatMessage(language, m.StreakUsername!, m.StreakDays!.Value) ?? m.Dto.Body;

        return messages.Select(m => m.Dto);
    }
}
