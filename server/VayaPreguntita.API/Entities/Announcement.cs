namespace VayaPreguntita.API.Entities;

// A one-off push to every player that has gone out (rama 57): the row is written before the
// push is sent, so a restart or a second instance never sends it twice. Key names the
// announcement, e.g. "seasonal:ring-witch".
public class Announcement
{
    public string Key { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
