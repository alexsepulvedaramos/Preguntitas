using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.DTOs.Streaks;

namespace VayaPreguntita.API.DTOs.Daily;

// POST daily/vote: the fresh results plus the voter's streak change (rama 19).
public class VoteResponseDto
{
    public QuestionResultDto Results { get; set; } = null!;
    public StreakUpdateDto Streak { get; set; } = null!;
}
