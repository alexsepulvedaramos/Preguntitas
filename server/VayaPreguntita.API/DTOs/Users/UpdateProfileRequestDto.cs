namespace VayaPreguntita.API.DTOs.Users;

public class UpdateProfileRequestDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string? FrameColor { get; set; }
}
