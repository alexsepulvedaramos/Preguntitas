using VayaPreguntita.API.DTOs.Chat;

namespace VayaPreguntita.API.Services;

public interface IChatService
{
    Task<IEnumerable<ChatMessageDto>?> GetMessagesAsync(int groupId, int userId);
    Task<(bool success, string? error, IEnumerable<ChatMessageDto>? messages)> SendMessageAsync(
        int groupId,
        int userId,
        SendChatMessageDto dto
    );
}
