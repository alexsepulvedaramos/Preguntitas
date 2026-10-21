using VayaPreguntita.API.Entities;

namespace VayaPreguntita.API.DTOs.Shop;

// The shop cosmetics other members see on someone (rama 42, §17.10): ShopCatalog keys, each
// null when nothing is equipped in that slot. The whole object is null when none is.
public class LookDto
{
    public string? RingSkin { get; set; }
    public string? NameColor { get; set; }

    public static LookDto? From(User user) => From(user.RingSkinKey, user.NameColorKey);

    public static LookDto? From(string? ringSkin, string? nameColor) =>
        ringSkin is null && nameColor is null
            ? null
            : new LookDto
            {
                RingSkin = ringSkin,
                NameColor = nameColor,
            };
}
