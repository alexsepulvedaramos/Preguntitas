using VayaPreguntita.API.DTOs.Chat;

namespace VayaPreguntita.API.Services;

public interface IChatService
{
    // null = not a member. Locked (no messages, only the count) until you've voted the open
    // question (rama 49).
    Task<ChatDto?> GetMessagesAsync(int groupId, int userId);
    Task<(bool success, string? error, IEnumerable<ChatMessageDto>? messages)> SendMessageAsync(
        int groupId,
        int userId,
        SendChatMessageDto dto
    );
}
