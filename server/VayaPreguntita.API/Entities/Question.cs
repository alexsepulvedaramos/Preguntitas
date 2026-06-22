namespace VayaPreguntita.API.Entities;

using VayaPreguntita.API.Enums;

public class Question
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public QuestionSource Source { get; set; } = QuestionSource.UserCreated; // nuevo
    public bool IsUsed { get; set; } = false; // nuevo
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime? DateActivated { get; set; } // antes DateAsked

    public QuestionMetadata Metadata { get; set; } = new();

    public int? CreatorId { get; set; } // null => came from a pack (no human author)
    public User? Creator { get; set; }

    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;

    public List<Option> Options { get; set; } = [];
    public List<Vote> Votes { get; set; } = [];
}
