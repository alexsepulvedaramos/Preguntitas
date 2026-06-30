namespace VayaPreguntita.API.Entities;

public class Pack
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Global master switch: whether this pack exists for groups at all.
    public bool IsActiveByDefault { get; set; } = true;

    // When true, a newly created group gets this pack pre-disabled (a GroupDisabledPack row),
    // so it starts off but an admin can still enable it per group. Does not affect effective
    // enablement directly — it only drives the one-time seeding of disabled rows.
    public bool DisabledByDefault { get; set; }

    public List<QuestionTemplate> Templates { get; set; } = [];
}
