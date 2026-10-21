namespace VayaPreguntita.API.Entities;

// Reaction (rama 48, §17.15 item 6): one emoji from ReactionEmojis on a chat message or on a
// text a player wrote in the results (an Open Text answer or a poll's "Otro" — a Vote row
// carrying FreeText). Exactly one of ChatMessageId / VoteId is set. One per person and target,
// like WhatsApp: picking another emoji replaces it.
public class Reaction
{
    public int Id { get; set; }

    public int? ChatMessageId { get; set; }
    public ChatMessage? ChatMessage { get; set; }

    public int? VoteId { get; set; }
    public Vote? Vote { get; set; }

    // Who reacted.
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Who wrote the message or answer: the push goes to them, and the "one push every
    // ReactionRules.PushCooldown" rule is read by it.
    public int AuthorUserId { get; set; }
    public User Author { get; set; } = null!;

    // A ReactionEmojis key ("fire"), not the emoji itself.
    public string Emoji { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    // When this reaction pushed its author; null when it didn't (cooldown, or a changed emoji).
    public DateTime? NotifiedAt { get; set; }
}
