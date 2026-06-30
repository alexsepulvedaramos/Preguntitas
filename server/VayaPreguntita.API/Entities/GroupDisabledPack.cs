namespace VayaPreguntita.API.Entities;

// Presence of a row means the pack is disabled for that group; absence means enabled.
// Pack.IsActiveByDefault remains the global master switch — effective per-group
// enablement is IsActiveByDefault && no matching row here.
public class GroupDisabledPack
{
    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;

    public int PackId { get; set; }
    public Pack Pack { get; set; } = null!;
}
