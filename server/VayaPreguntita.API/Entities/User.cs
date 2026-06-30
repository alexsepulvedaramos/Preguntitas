// User.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace VayaPreguntita.API.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? FrameColor { get; set; }
    public DateTime DateJoined { get; set; } = DateTime.UtcNow;
    public List<GroupMember> GroupMemberships { get; set; } = [];

    [InverseProperty("Creator")]
    public List<Question> CreatedQuestions { get; set; } = [];

    [InverseProperty("User")]
    public List<Vote> Votes { get; set; } = [];

    [InverseProperty("Creator")]
    public List<Group> CreatedGroups { get; set; } = [];


}
