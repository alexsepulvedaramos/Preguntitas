using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Chat;
using VayaPreguntita.API.DTOs.Reactions;
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

    private static readonly (string Emoji, string Glyph)[] LegacyEmojiGlyphs = [("🔥 ", "streak"), ("🎉 ", "celebrate")];

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

        // Streak messages are written in the reader's language (rama 24), with their glyph.
        var language = ContentTranslations.RequestLanguage;
        foreach (var m in messages.Where(m => m.StreakUsername != null && m.StreakDays != null))
        {
            m.Dto.Body = StreakTiers.ChatMessage(language, m.StreakUsername!, m.StreakDays!.Value) ?? m.Dto.Body;
            m.Dto.Glyph = StreakTiers.ChatGlyph(m.StreakDays!.Value);
        }

        // Older system messages were stored with a leading emoji: show its glyph instead.
        foreach (var m in messages.Where(m => m.Dto.IsSystem && m.Dto.Glyph == null))
        {
            foreach (var (emoji, glyph) in LegacyEmojiGlyphs)
            {
                if (!m.Dto.Body.StartsWith(emoji, StringComparison.Ordinal)) continue;
                m.Dto.Body = m.Dto.Body[emoji.Length..];
                m.Dto.Glyph = glyph;
                break;
            }
        }

        // Reactions (rama 48), oldest first.
        var messageIds = messages.Select(m => m.Dto.Id).ToList();
        var reactions = await context
            .Reactions.Where(r => r.ChatMessageId != null && messageIds.Contains(r.ChatMessageId.Value))
            .OrderBy(r => r.CreatedAt)
            .Select(r => new
            {
                MessageId = r.ChatMessageId!.Value,
                Dto = new ReactionDto { Emoji = r.Emoji, UserId = r.UserId, Username = r.User.Username },
            })
            .ToListAsync();
        foreach (var m in messages)
            m.Dto.Reactions = reactions.Where(r => r.MessageId == m.Dto.Id).Select(r => r.Dto).ToList();

        return messages.Select(m => m.Dto);
    }
}
