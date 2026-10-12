namespace VayaPreguntita.API.DTOs.MonthlySummaries;

// GET api/groups/{id}/monthly-summaries — newest first, at most the last 12 months.
// Month is "YYYY-MM".
public record MonthlySummaryListItemDto(string Month, int Questions);

// GET api/groups/{id}/monthly-summaries/{YYYY-MM} (§17.12). MyMatch is the caller's own
// match only: nobody else's is ever sent.
public record MonthlySummaryDto(
    string Month,
    int Questions,
    // Group participation, 0–100: on-time answers over the questions each member could answer.
    int Participation,
    List<SummaryAwardDto> Awards,
    List<SummaryMemberDto> Members,
    SummaryMatchDto? MyMatch
);

public record SummaryPersonDto(int UserId, string Username, string? AvatarUrl);

// Key: superlative | selector | guesser | consistent | starAuthor. Value is the count
// (a percentage for "consistent"); every winner tied at the top is listed.
public record SummaryAwardDto(string Key, int Value, List<SummaryAwardWinnerDto> Winners);

public record SummaryAwardWinnerDto(SummaryPersonDto Person, string? QuestionText);

// Alphabetical by username: participation is shown, never ranked.
public record SummaryMemberDto(SummaryPersonDto Person, int Answered, int Eligible, int Percent, int BestRun);

public record SummaryMatchDto(List<SummaryPersonDto> People, int Count);
