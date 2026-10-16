namespace VayaPreguntita.API.Entities;

public class UserRefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = "";

    // The token this row held before its last rotation. It stays accepted until the client
    // refreshes with the current one, so a refresh response lost on the way (app closed
    // mid-request, dropped connection) doesn't log the device out.
    public string? PreviousTokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
