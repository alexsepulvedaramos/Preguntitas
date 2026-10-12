using VayaPreguntita.API.DTOs.Coins;

namespace VayaPreguntita.API.DTOs.Daily;

// POST daily/select and daily/select/confirm. Coins is set only when the caller won the
// race to select (§17.5), for the "+10 🪙" toast.
public class SelectResponseDto
{
    public CoinsEarnedDto? Coins { get; set; }
}
