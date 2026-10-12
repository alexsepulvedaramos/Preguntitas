namespace VayaPreguntita.API.DTOs.Auth;

public class RegisterRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // The device's UI language at sign-up; anything unsupported (or missing) falls back to Spanish.
    public string? PreferredLanguage { get; set; }
}
