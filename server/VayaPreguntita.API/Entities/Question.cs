namespace VayaPreguntita.API.Entities;

using VayaPreguntita.API.Enums;

public class Question
{
    // ==========================================
    // 1. CORE DATA
    // ==========================================
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime? DateAsked { get; set; }

    // ==========================================
    // 2. GAME MECHANICS & RULES
    // ==========================================
    // public bool AllowNobody { get; set; } = false; // Can users choose "Nobody" as an option?
    public int MaxSelections { get; set; } = 1;
    public List<int> BlacklistedUserIds { get; set; } = [];
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; }

    // ==========================================
    // 3. FOREIGN KEYS & NAVIGATION PROPERTIES
    // ==========================================

    // Who created it?
    public int CreatorId { get; set; }
    public User Creator { get; set; } = null!;

    // Which group is this for?
    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;

    // ==========================================
    // 4. RELATIONSHIPS
    // ==========================================
    public List<Option> Options { get; set; } = [];
    public List<Vote> Votes { get; set; } = [];
}
