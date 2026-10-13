namespace VayaPreguntita.API.DTOs.Streaks;

// Member detail card (rama 19): a member's streak and stats inside one group.
public class MemberStatsDto
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;

    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? FrameColor { get; set; }

    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
    public bool HasCrown { get; set; }
    public string? Title { get; set; }

    // Shop cosmetics (rama 42) and the join date behind the seniority frame.
    public Shop.LookDto? Look { get; set; }
    public DateTime JoinedAt { get; set; }

    // Distinct questions the member voted on in this group.
    public int TotalVotes { get; set; }

    // Active daily questions since the member joined (the open one counts only once voted).
    public int ActiveQuestions { get; set; }

    // TotalVotes ÷ ActiveQuestions, 0–100.
    public int ParticipationPercent { get; set; }

    public int TimesSelector { get; set; }
    public int QuestionsCreated { get; set; }

    // "Aciertos adivinando X/Y" (§17.6): guesses made in the group (skips excluded) and how
    // many were right.
    public int GuessesMade { get; set; }
    public int GuessesCorrect { get; set; }
}
