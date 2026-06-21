// CreateVoteDto.cs
namespace VayaPreguntita.API.DTOs.Questions;

public class CreateVoteDto
{
    public List<int>? SelectedOptionIds { get; set; }
    public int? SelectedTargetUserId { get; set; }
    public List<int>? SelectedTargetUserIds { get; set; }
    public int? NumericValue { get; set; }
}
