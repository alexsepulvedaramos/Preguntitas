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

    // Equipped ring finish (rama 42, rama 57): an owned ShopCatalog key, "" to take it off,
    // null = unchanged. Wearing one puts the frame back on the streak ring.
    public string? RingSkinKey { get; set; }

    // Equipped plate (rama 60): an owned plate key, "" to take it off, null = unchanged.
    // Independent of the ring and the frame colour.
    public string? PlateKey { get; set; }
}
