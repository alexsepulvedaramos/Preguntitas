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

    public int CreatorId { get; set; }

    [ForeignKey(nameof(CreatorId))]
    [InverseProperty("CreatedGroups")]
    public User Creator { get; set; } = null!;

    public List<GroupMember> Members { get; set; } = [];
    public List<Question> Questions { get; set; } = [];
    public List<DailyEntry> DailyEntries { get; set; } = [];
}
