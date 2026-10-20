namespace VayaPreguntita.API.DTOs.Groups;

public class GroupResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string InvitationCode { get; set; }
    public required string CreatorUsername { get; set; }
    public TimeOnly DailyQuestionTime { get; set; }

    // "voting" | "selector" | "results" | "no_question"
    public string DailyStatus { get; set; } = "no_question";

    // The caller's current streak in this group (0 = none). Only filled on the group
    // list, so the "Vota ahora" pill can say what's at stake.
    public int MyStreak { get; set; }

    // Group list only: something happened since the caller last opened the group — others'
    // chat messages, a new question still to vote, new votes on a question they answered, or
    // their turn to pick (§16 unread-activity dot).
    public bool HasNewActivity { get; set; }

    // Set only on the update response: true when DailyQuestionTime changed but today's
    // question had already activated, so the new time only takes effect next cycle (§4.8).
    // Lets the settings UI tell the admin "the change applies from tomorrow".
    public bool DailyTimeChangeAppliesFromTomorrow { get; set; }
}
