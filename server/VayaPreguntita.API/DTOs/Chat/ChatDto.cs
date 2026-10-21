namespace VayaPreguntita.API.DTOs.Chat;

// The open question's chat (rama 49): until you've voted it, it is locked and only says how
// many messages there are, so the chat can't spoil the answers.
public class ChatDto
{
    public bool Locked { get; set; }
    public int MessageCount { get; set; }
    // Empty while locked.
    public List<ChatMessageDto> Messages { get; set; } = [];
}
