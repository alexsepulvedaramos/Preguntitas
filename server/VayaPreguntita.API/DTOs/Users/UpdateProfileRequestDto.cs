namespace VayaPreguntita.API.DTOs.Users;

public class UpdateProfileRequestDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string? FrameColor { get; set; }

    // Title to show: "auto" (highest unlocked), "none", or an unlocked tier key. null = unchanged.
    public string? TitleKey { get; set; }
}
