using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Helpers;

// One cosmetic for sale. Id is the ledger RefId of its purchase (with the ledger's unique
// (UserId, Reason, RefId) index that makes buying twice impossible) — never reuse or
// renumber one. Name is set only for titles, the one cosmetic whose text the server renders.
public record CosmeticItem(
    int Id,
    string Key,
    CosmeticKind Kind,
    int Price,
    CosmeticRarity Rarity,
    LocalizedText? Name = null
);

// The cosmetics catalog (rama 42, §17.10): a server constant, like the pack catalog. The
// art is pure CSS/SVG on the client — mirrored in core/constants/shop-catalog.ts, keep both
// in sync.
public static class ShopCatalog
{
    public static readonly CosmeticItem[] All =
    [
        // Ring skins: restyle the streak ring, still encoding the tier.
        new(1, "ring-neon", CosmeticKind.RingSkin, 400, CosmeticRarity.Common),
        new(2, "ring-pixel", CosmeticKind.RingSkin, 600, CosmeticRarity.Rare),
        new(3, "ring-ice", CosmeticKind.RingSkin, 800, CosmeticRarity.Epic),
        new(4, "ring-flames", CosmeticKind.RingSkin, 1000, CosmeticRarity.Legendary),

        // Avatar accessories: over the image, never over the ring.
        new(11, "acc-sunglasses", CosmeticKind.Accessory, 200, CosmeticRarity.Common),
        new(12, "acc-party-hat", CosmeticKind.Accessory, 200, CosmeticRarity.Common),
        new(13, "acc-moustache", CosmeticKind.Accessory, 300, CosmeticRarity.Rare),
        new(14, "acc-headphones", CosmeticKind.Accessory, 300, CosmeticRarity.Rare),
        new(15, "acc-paper-crown", CosmeticKind.Accessory, 400, CosmeticRarity.Epic),
        new(16, "acc-halo", CosmeticKind.Accessory, 500, CosmeticRarity.Legendary),

        // Name colours: a curated palette with light/dark variants (WCAG AA).
        new(21, "color-coral", CosmeticKind.NameColor, 250, CosmeticRarity.Common),
        new(22, "color-tangerine", CosmeticKind.NameColor, 250, CosmeticRarity.Common),
        new(23, "color-gold", CosmeticKind.NameColor, 250, CosmeticRarity.Common),
        new(24, "color-mint", CosmeticKind.NameColor, 250, CosmeticRarity.Common),
        new(25, "color-sky", CosmeticKind.NameColor, 250, CosmeticRarity.Common),
        new(26, "color-lavender", CosmeticKind.NameColor, 250, CosmeticRarity.Common),
        new(27, "color-pink", CosmeticKind.NameColor, 250, CosmeticRarity.Common),
        new(28, "color-lime", CosmeticKind.NameColor, 250, CosmeticRarity.Common),

        // Badge emoji next to the name.
        new(31, "badge-fire", CosmeticKind.Badge, 150, CosmeticRarity.Common),
        new(32, "badge-eyes", CosmeticKind.Badge, 150, CosmeticRarity.Common),
        new(33, "badge-popcorn", CosmeticKind.Badge, 150, CosmeticRarity.Common),
        new(34, "badge-teapot", CosmeticKind.Badge, 150, CosmeticRarity.Common),
        new(35, "badge-frog", CosmeticKind.Badge, 150, CosmeticRarity.Common),
        new(36, "badge-coffee", CosmeticKind.Badge, 150, CosmeticRarity.Common),
        new(37, "badge-chili", CosmeticKind.Badge, 150, CosmeticRarity.Common),
        new(38, "badge-unicorn", CosmeticKind.Badge, 150, CosmeticRarity.Common),
        new(39, "badge-nails", CosmeticKind.Badge, 150, CosmeticRarity.Common),
        new(40, "badge-clown", CosmeticKind.Badge, 150, CosmeticRarity.Common),
        new(41, "badge-goat", CosmeticKind.Badge, 150, CosmeticRarity.Common),
        new(42, "badge-brain", CosmeticKind.Badge, 150, CosmeticRarity.Common),

        // Shop titles: shown in place of the streak title (User.SelectedTitleKey). Adapted,
        // not translated, like the streak titles (rama 24).
        new(51, "title-pro-gossip", CosmeticKind.Title, 200, CosmeticRarity.Common,
            new("Cotilla Profesional", "Professional Busybody", "Berufs-Tratschtante")),
        new(52, "title-undecided", CosmeticKind.Title, 200, CosmeticRarity.Common,
            new("Voto Indeciso", "Eternal Fence-Sitter", "Ewig Unentschlossen")),
        new(53, "title-devils-advocate", CosmeticKind.Title, 300, CosmeticRarity.Rare,
            new("Abogado del Diablo", "Devil's Advocate", "Advocatus Diaboli")),
        new(54, "title-oracle", CosmeticKind.Title, 400, CosmeticRarity.Epic,
            new("El Oráculo", "The Oracle", "Das Orakel")),

        // App themes: a full personal palette (light and dark). Only the owner sees it.
        new(61, "theme-ocean", CosmeticKind.Theme, 250, CosmeticRarity.Common),
        new(62, "theme-strawberry", CosmeticKind.Theme, 250, CosmeticRarity.Common),
        new(63, "theme-grape", CosmeticKind.Theme, 250, CosmeticRarity.Common),
        new(64, "theme-tangerine", CosmeticKind.Theme, 250, CosmeticRarity.Common),
    ];

    private static readonly Dictionary<string, CosmeticItem> ByKeyMap = All.ToDictionary(i => i.Key);
    private static readonly Dictionary<int, CosmeticItem> ByIdMap = All.ToDictionary(i => i.Id);

    public static CosmeticItem? ByKey(string? key) =>
        key is not null && ByKeyMap.TryGetValue(key, out var item) ? item : null;

    public static CosmeticItem? ById(int? id) =>
        id is int value && ByIdMap.TryGetValue(value, out var item) ? item : null;

    public static bool IsShopTitle(string? key) => ByKey(key)?.Kind == CosmeticKind.Title;
}
