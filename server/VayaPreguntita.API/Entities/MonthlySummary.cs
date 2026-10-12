namespace VayaPreguntita.API.Entities;

// Monthly summary ("wrapped", §17.12): a snapshot of one group's month, built once by the
// tick after the month ends. Payload is a MonthlySummaryPayload serialized as JSONB.
public class MonthlySummary
{
    public int Id { get; set; }

    // First day of the local (UTC+2) month it covers.
    public DateOnly Month { get; set; }

    public string Payload { get; set; } = "{}";

    public DateTime CreatedAt { get; set; }

    // When the monthly_summary push went out (at the group's T, with that day's activation).
    public DateTime? NotifiedAt { get; set; }

    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;
}
