namespace VayaPreguntita.API.Entities;

public class Group
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public List<User> Users { get; set; } = [];
    public List<Question> Questions { get; set; } = [];
}
