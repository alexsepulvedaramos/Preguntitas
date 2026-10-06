// DTOs/Groups/GroupMemberDto.cs
namespace VayaPreguntita.API.DTOs.Groups;

public class GroupMemberDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? FrameColor { get; set; }
    public DateTime JoinedAt { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsCurrentUser { get; set; }
    public bool NotificationsMuted { get; set; }

    // Effective voting streak in this group (rama 19).
    public int CurrentStreak { get; set; }

    // Holds the group's best current streak (ties: whoever reached it first).
    public bool HasCrown { get; set; }

    // The title the member chose to show (null when none is unlocked or it's hidden).
    public string? Title { get; set; }
}
