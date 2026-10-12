using VayaPreguntita.API.DTOs.Coins;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.DTOs.Streaks;

namespace VayaPreguntita.API.DTOs.Daily;

// POST daily/vote: the fresh results plus the voter's streak change (rama 19) and the
// coins it earned (rama 34).
public class VoteResponseDto
{
    public QuestionResultDto Results { get; set; } = null!;
    public StreakUpdateDto Streak { get; set; } = null!;
    public CoinsEarnedDto Coins { get; set; } = null!;
}
