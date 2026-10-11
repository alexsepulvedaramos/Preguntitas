namespace VayaPreguntita.API.Entities;

public class Pack
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Name/description in the other languages, keyed by language code ("en", "de"; rama 24).
    // Display only: Name stays the pack's identity in the catalog sync.
    public Dictionary<string, string> NameTranslations { get; set; } = [];
    public Dictionary<string, string> DescriptionTranslations { get; set; } = [];

    // Global master switch: whether this pack exists for groups at all.
    public bool IsActiveByDefault { get; set; } = true;

    // When true, a newly created group gets this pack pre-disabled (a GroupDisabledPack row),
    // so it starts off but an admin can still enable it per group. Does not affect effective
    // enablement directly — it only drives the one-time seeding of disabled rows.
    public bool DisabledByDefault { get; set; }

    public List<QuestionTemplate> Templates { get; set; } = [];
}
