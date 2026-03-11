namespace VayaPreguntita.API.DTOs;

public class OptionDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public ICollection<UserDto> AssociatedUsers { get; set; } = [];
}
