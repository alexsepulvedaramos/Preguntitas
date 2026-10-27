namespace VayaPreguntita.API.Entities;

// The guess on a daily entry: "¿Quién la escribió?" (§17.6) or "¿Quién la eligió?" (§17.17).
// One row per member and daily entry. A Vote can span several rows (multi-select polls,
// secret pairings), so the guess lives in its own table.
public class AuthorGuess : GuessRow
{
    public int DailyEntryId { get; set; }
    public DailyEntry DailyEntry { get; set; } = null!;
}
