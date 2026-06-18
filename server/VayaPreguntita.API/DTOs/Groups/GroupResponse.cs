namespace VayaPreguntita.API.DTOs.Groups;

public class GroupResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string InvitationCode { get; set; }
    public TimeOnly DailyQuestionTime { get; set; }
}
