namespace VayaPreguntita.API.DTOs.Users;

// Marks to store, by key (see SeenMarks). The answer is the user's full set after merging.
public class SaveSeenMarksDto
{
    public Dictionary<string, string> Marks { get; set; } = [];
}
