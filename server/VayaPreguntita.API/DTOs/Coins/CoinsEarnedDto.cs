namespace VayaPreguntita.API.DTOs.Coins;

// Coins paid by a single request, for the "+5 🪙" toast (§17.2).
public class CoinsEarnedDto
{
    public int Earned { get; set; }

    // The part of Earned that is the daily streak bonus (§17.15), shown apart in the toast.
    public int StreakBonus { get; set; }

    // Balance after paying.
    public int Balance { get; set; }

    // True when the daily cap cut a reward short (or to zero).
    public bool CapReached { get; set; }
}
