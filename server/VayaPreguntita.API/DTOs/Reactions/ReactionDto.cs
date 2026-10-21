namespace VayaPreguntita.API.DTOs.Reactions;

// One member's reaction on a message or answer; the client groups them by emoji.
public class ReactionDto
{
    // A ReactionRules.Emojis key.
    public string Emoji { get; set; } = null!;
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
}

// PUT body: a ReactionRules.Emojis key, or null to remove yours.
public class SetReactionDto
{
    public string? Emoji { get; set; }
}
