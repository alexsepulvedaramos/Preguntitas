namespace VayaPreguntita.API.Entities;

public class Option
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public Dictionary<string, string> TextTranslations { get; set; } = [];
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
}
