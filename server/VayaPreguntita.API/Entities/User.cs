// User.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace VayaPreguntita.API.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    // A hex colour, FrameColors.Streak (ring follows the streak tier) or FrameColors.None.
    // null is treated as Streak.
    public string? FrameColor { get; set; } = Helpers.FrameColors.Streak;

    // Set the first time the user reaches a 3-day streak, when a fixed frame colour is
    // switched to Streak once (rama 19). Later choices are always respected.
    public bool StreakFrameAutoApplied { get; set; } = false;

    // Best streak ever reached in any group. Titles unlocked by it stay unlocked forever,
    // even if the user leaves that group (rama 19).
    public int HighestStreakEver { get; set; } = 0;

    // Title shown on the profile: null = automatic (highest unlocked), "none" = hidden,
    // otherwise an unlocked tier key.
    public string? SelectedTitleKey { get; set; }
    public DateTime DateJoined { get; set; } = DateTime.UtcNow;
    public List<GroupMember> GroupMemberships { get; set; } = [];

    [InverseProperty("Creator")]
    public List<Question> CreatedQuestions { get; set; } = [];

    [InverseProperty("User")]
    public List<Vote> Votes { get; set; } = [];

    [InverseProperty("Creator")]
    public List<Group> CreatedGroups { get; set; } = [];


}
