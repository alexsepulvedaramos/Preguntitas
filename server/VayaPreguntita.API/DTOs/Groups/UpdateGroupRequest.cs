namespace VayaPreguntita.API.DTOs.Groups;

public class UpdateGroupRequest
{
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public required TimeOnly DailyQuestionTime { get; set; }
}
