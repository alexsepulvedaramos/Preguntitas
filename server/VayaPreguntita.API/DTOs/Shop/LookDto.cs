using VayaPreguntita.API.Entities;

namespace VayaPreguntita.API.DTOs.Shop;

// The shop cosmetics other members see on someone (rama 42, §17.10): ShopCatalog keys, each
// null when nothing is equipped in that slot. The whole object is null when none is.
public class LookDto
{
    public string? RingSkin { get; set; }
    public string? Accessory { get; set; }
    public string? NameColor { get; set; }

    public static LookDto? From(User user) => From(user.RingSkinKey, user.AccessoryKey, user.NameColorKey);

    public static LookDto? From(string? ringSkin, string? accessory, string? nameColor) =>
        ringSkin is null && accessory is null && nameColor is null
            ? null
            : new LookDto
            {
                RingSkin = ringSkin,
                Accessory = accessory,
                NameColor = nameColor,
            };
}
