namespace VayaPreguntita.API.Entities;

// Template-level poll option. Mirrors Option, which hangs off a concrete Question.
public class QuestionTemplateOption
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int QuestionTemplateId { get; set; }
    public QuestionTemplate QuestionTemplate { get; set; } = null!;
}
