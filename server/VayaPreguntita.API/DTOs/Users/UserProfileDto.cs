namespace VayaPreguntita.API.DTOs.Users;

public class UserProfileDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? FrameColor { get; set; }

    // Highest current streak across all the user's groups — drives the ring outside a group.
    public int HighestStreak { get; set; }
}
