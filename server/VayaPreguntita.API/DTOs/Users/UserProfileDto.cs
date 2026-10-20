namespace VayaPreguntita.API.DTOs.Users;

public class UserProfileDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? FrameColor { get; set; }

    // Highest current streak across all the user's groups — drives the ring outside a group.
    public int HighestStreak { get; set; }

    // Titles (rama 19): unlocked forever by the best streak ever reached.
    public int HighestStreakEver { get; set; }
    public string? SelectedTitleKey { get; set; }
    public string PreferredLanguage { get; set; } = string.Empty;
    public string? Title { get; set; }

    // Coin wallet balance (rama 34, §17.2).
    public int Coins { get; set; }

    // Game items held (rama 45, §17.10).
    public Coins.InventoryDto Inventory { get; set; } = new();
    public List<Streaks.TitleOptionDto> UnlockedTitles { get; set; } = [];

    // Shop cosmetics (rama 42, §17.10): what's equipped, the personal app theme, and every
    // owned key.
    public Shop.LookDto? Look { get; set; }
    public string? ThemeKey { get; set; }
    public List<string> OwnedCosmetics { get; set; } = [];

    // When the user joined their oldest current group: seniority frame outside a group.
    public DateTime? FirstJoinedAt { get; set; }
}
