namespace VayaPreguntita.API.Services;

public interface ISeenMarksService
{
    Task<Dictionary<string, string>> GetAsync(int userId);

    // Merges the marks into the user's (each only moves forward) and returns the full set.
    // Null when a key or value isn't one of the known kinds, or there are too many.
    Task<Dictionary<string, string>?> SaveAsync(
        int userId,
        IReadOnlyDictionary<string, string> marks
    );
}
