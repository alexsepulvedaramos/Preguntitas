namespace VayaPreguntita.API.DTOs.Chat;

public class ChatMessageDto
{
    public int Id { get; set; }
    public string Body { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public bool IsCurrentUser { get; set; }
}
