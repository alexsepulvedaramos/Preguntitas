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
}
