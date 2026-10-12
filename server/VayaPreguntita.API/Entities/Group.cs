// Group.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace VayaPreguntita.API.Entities;

public class Group
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string InvitationCode { get; set; } = string.Empty;
    public TimeOnly DailyQuestionTime { get; set; } = new TimeOnly(12, 0);
    public string? TimeZoneId { get; set; } = "Europe/Madrid";
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    // One-time marker: whether the default-disabled packs (Pack.DisabledByDefault) have been
    // seeded as GroupDisabledPack rows for this group. Prevents the startup backfill from
    // re-disabling a pack an admin later enabled.
    public bool DefaultPacksApplied { get; set; }

    // Shuffled selector rotation (§17.4): the round being drawn. Every member is selector
    // once per round, drawn at random among those whose LastSelectorRound is below it.
    public int SelectorRound { get; set; } = 1;

    public int CreatorId { get; set; }

    [ForeignKey(nameof(CreatorId))]
    [InverseProperty("CreatedGroups")]
    public User Creator { get; set; } = null!;

    public List<GroupMember> Members { get; set; } = [];
    public List<Question> Questions { get; set; } = [];
    public List<DailyEntry> DailyEntries { get; set; } = [];
}
