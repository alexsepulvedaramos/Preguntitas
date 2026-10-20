namespace VayaPreguntita.API.DTOs.Users;

public class UpdateProfileRequestDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string? FrameColor { get; set; }

    // Title to show: "auto" (highest unlocked), "none" or an unlocked tier key. null = unchanged.
    public string? TitleKey { get; set; }

    // "es", "en" or "de". null = unchanged.
    public string? PreferredLanguage { get; set; }

    // Equipped shop cosmetics (rama 42): an owned ShopCatalog key of that kind, "" to take it
    // off, null = unchanged.
    public string? RingSkinKey { get; set; }
    public string? AccessoryKey { get; set; }
    public string? NameColorKey { get; set; }
    public string? ThemeKey { get; set; }
}
