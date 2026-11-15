using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Services;

// Per-user "seen" marks (rama 56): release notes, one-off hints, monthly summary cards.
public class SeenMarksService(AppDbContext context) : ISeenMarksService
{
    public Task<Dictionary<string, string>> GetAsync(int userId) =>
        context
            .UserSeenMarks.AsNoTracking()
            .Where(m => m.UserId == userId)
            .ToDictionaryAsync(m => m.Key, m => m.Value);

    public async Task<Dictionary<string, string>?> SaveAsync(
        int userId,
        IReadOnlyDictionary<string, string> marks
    )
    {
        if (
            marks.Count > SeenMarks.MaxPerRequest
            || marks.Any(m => !SeenMarks.IsValid(m.Key, m.Value))
        )
            return null;

        try
        {
            return await MergeAsync(userId, marks);
        }
        catch (DbUpdateException)
        {
            // Two devices stored the same new key at once: the other one's row is there now.
            context.ChangeTracker.Clear();
            return await MergeAsync(userId, marks);
        }
    }

    private async Task<Dictionary<string, string>?> MergeAsync(
        int userId,
        IReadOnlyDictionary<string, string> marks
    )
    {
        var stored = await context
            .UserSeenMarks.Where(m => m.UserId == userId)
            .ToDictionaryAsync(m => m.Key);
        if (stored.Keys.Union(marks.Keys).Count() > SeenMarks.MaxPerUser)
            return null;

        foreach (var (key, value) in marks)
        {
            if (stored.TryGetValue(key, out var mark))
            {
                if (SeenMarks.Advances(key, mark.Value, value))
                    mark.Value = value;
            }
            else
            {
                mark = new UserSeenMark
                {
                    UserId = userId,
                    Key = key,
                    Value = value,
                };
                context.UserSeenMarks.Add(mark);
                stored[key] = mark;
            }
        }

        await context.SaveChangesAsync();
        return stored.ToDictionary(m => m.Key, m => m.Value.Value);
    }
}
