namespace VayaPreguntita.API.Entities;

// What a user has already seen, so it doesn't come back on another device or after logging
// in again (rama 56): the last release notes opened, the one-off hints dismissed, the last
// monthly summary opened per group. One row per key (see SeenMarks for the keys).
public class UserSeenMark
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
