using VayaPreguntita.API.Entities;

namespace VayaPreguntita.API.DTOs.Shop;

// The shop cosmetics other members see on someone (rama 42, §17.10): ShopCatalog keys, each
// null when nothing is equipped in that slot. The whole object is null when none is. Since
// rama 57 only the ring finish is left (name colours were retired).
public class LookDto
{
    public string? RingSkin { get; set; }

    public static LookDto? From(User user) => From(user.RingSkinKey);

    public static LookDto? From(string? ringSkin) =>
        ringSkin is null ? null : new LookDto { RingSkin = ringSkin };
}
