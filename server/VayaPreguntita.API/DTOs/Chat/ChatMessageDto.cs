using VayaPreguntita.API.DTOs.Reactions;

namespace VayaPreguntita.API.DTOs.Chat;

public class ChatMessageDto
{
    public int Id { get; set; }
    public string Body { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    // null for system messages (streak milestones).
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public bool IsSystem { get; set; }
    // System messages only: the app glyph drawn before the text ("streak", "celebrate").
    public string? Glyph { get; set; }
    public bool IsCurrentUser { get; set; }
    // Rama 48: who reacted with what (never on system messages).
    public List<ReactionDto> Reactions { get; set; } = [];
}
