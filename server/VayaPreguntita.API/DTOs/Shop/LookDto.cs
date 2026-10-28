using VayaPreguntita.API.Entities;

namespace VayaPreguntita.API.DTOs.Shop;

// The shop cosmetics other members see on someone (rama 42, §17.10): ShopCatalog keys, each
// null when nothing is equipped in that slot. The whole object is null when none is. Since
// rama 57 the ring finish, since rama 60 the plate too.
public class LookDto
{
    public string? RingSkin { get; set; }
    public string? Plate { get; set; }

    public static LookDto? From(User user) => From(user.RingSkinKey, user.PlateKey);

    public static LookDto? From(string? ringSkin, string? plate) =>
        ringSkin is null && plate is null ? null : new LookDto { RingSkin = ringSkin, Plate = plate };
}
