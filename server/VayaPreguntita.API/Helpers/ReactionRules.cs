namespace VayaPreguntita.API.Helpers;

// Reactions (rama 48, §17.15 item 6): a fixed set, stored by key so the set can be redrawn
// without touching the rows. The client mirrors the keys and their order (reactions.ts).
public static class ReactionRules
{
    public const int MaxKeyLength = 16;

    // At most one reaction push per author in this window, however many come in.
    public static readonly TimeSpan PushCooldown = TimeSpan.FromMinutes(10);

    public static readonly IReadOnlyDictionary<string, string> Emojis = new Dictionary<string, string>
    {
        ["love"] = "❤️",
        ["laugh"] = "😂",
        ["skull"] = "💀",
        ["wow"] = "😮",
        ["sad"] = "😢",
        ["fire"] = "🔥",
        ["upside"] = "🙃",
    };

    public static bool IsValid(string? key) => key != null && Emojis.ContainsKey(key);
}
