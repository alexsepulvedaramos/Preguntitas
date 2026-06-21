// DTOs/Groups/GroupMemberDto.cs
namespace VayaPreguntita.API.DTOs.Groups;

public class GroupMemberDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}
