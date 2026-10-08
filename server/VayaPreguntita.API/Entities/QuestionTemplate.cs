namespace VayaPreguntita.API.Entities;

using VayaPreguntita.API.Enums;

// A global, voteless question template living inside a Pack. Never voted on directly:
// it is cloned into a concrete group Question at selection/preselection time.
public class QuestionTemplate
{
    public int Id { get; set; }

    // Stable identifier from the pack catalog (Data/Packs/*.json). Never changes once
    // assigned, so wording can be edited and a template can move between packs without
    // creating a duplicate. Null only for rows not yet adopted by the catalog sync.
    public string? Key { get; set; }

    // Retired templates are never offered or auto-selected again, but the row stays so that
    // already-cloned Questions (Question.TemplateId) keep their reference.
    public bool IsRetired { get; set; }

    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }

    // Reuses the same owned type as Question (stored as JSONB).
    public QuestionMetadata Metadata { get; set; } = new();

    // Only populated for CustomPoll templates.
    public List<QuestionTemplateOption> Options { get; set; } = [];

    public int PackId { get; set; }
    public Pack Pack { get; set; } = null!;
}
