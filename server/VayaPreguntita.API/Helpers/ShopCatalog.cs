using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Helpers;

// One cosmetic for sale. Id is the ledger RefId of its purchase (with the ledger's unique
// (UserId, Reason, RefId) index that makes buying twice impossible) — never reuse or
// renumber one. A seasonal item (rama 57, §17.15) is only sold between OnSaleFrom and
// OnSaleUntil (UTC); whoever bought it keeps wearing it whenever they like.
public record CosmeticItem(
    int Id,
    string Key,
    CosmeticKind Kind,
    int Price,
    CosmeticRarity Rarity,
    DateTime? OnSaleFrom = null,
    DateTime? OnSaleUntil = null
)
{
    public bool IsSeasonal => OnSaleFrom is not null;

    public bool IsOnSale(DateTime nowUtc) =>
        (OnSaleFrom is null || nowUtc >= OnSaleFrom) && (OnSaleUntil is null || nowUtc < OnSaleUntil);
}

// The cosmetics catalog (rama 42, §17.10; rethought in rama 57, §17.15 item 8): a server
// constant, like the pack catalog. The art is pure CSS/SVG on the client — mirrored in
// core/constants/shop-catalog.ts, keep both in sync.
public static class ShopCatalog
{
    // Halloween 2026 (rama 57): one week from 27 Oct 20:00 Madrid (CET, UTC+1), when the
    // "in the shop" push goes out. Priced so a week of daily votes in one group pays for it.
    public static readonly DateTime HalloweenFrom = new(2026, 10, 27, 19, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime HalloweenUntil = HalloweenFrom.AddDays(7);

    // Frame colour key → catalog key ("purple" → "frame-purple").
    public static string FrameColorKey(string paletteKey) => $"frame-{paletteKey}";

    public static readonly CosmeticItem[] All =
    [
        // Ring finishes: drawn on top of the streak ring's material, so the tier still shows;
        // their effects grow with the tier. Priced by how much they show and move and how much
        // they change from tier to tier (§17.15 item 8).
        new(5, "ring-volt", CosmeticKind.RingSkin, 400, CosmeticRarity.Rare),
        new(4, "ring-flare", CosmeticKind.RingSkin, 400, CosmeticRarity.Rare),
        new(3, "ring-blizzard", CosmeticKind.RingSkin, 400, CosmeticRarity.Rare),
        new(7, "ring-orbit", CosmeticKind.RingSkin, 800, CosmeticRarity.Epic),
        new(8, "ring-aurora", CosmeticKind.RingSkin, 800, CosmeticRarity.Epic),
        new(1, "ring-neon", CosmeticKind.RingSkin, 1500, CosmeticRarity.Legendary),
        new(2, "ring-arcade", CosmeticKind.RingSkin, 1500, CosmeticRarity.Legendary),
        new(6, "ring-doodle", CosmeticKind.RingSkin, 1500, CosmeticRarity.Legendary),

        // Seasonal finishes.
        new(101, "ring-witch", CosmeticKind.RingSkin, 50, CosmeticRarity.Common, HalloweenFrom, HalloweenUntil),

        // Plain frame colours (free until rama 57): the colour drawn round the avatar instead
        // of the streak ring.
        new(71, FrameColorKey("purple"), CosmeticKind.FrameColor, 200, CosmeticRarity.Common),
        new(72, FrameColorKey("blue"), CosmeticKind.FrameColor, 200, CosmeticRarity.Common),
        new(73, FrameColorKey("green"), CosmeticKind.FrameColor, 200, CosmeticRarity.Common),
        new(74, FrameColorKey("yellow"), CosmeticKind.FrameColor, 200, CosmeticRarity.Common),
        new(75, FrameColorKey("orange"), CosmeticKind.FrameColor, 200, CosmeticRarity.Common),
        new(76, FrameColorKey("red"), CosmeticKind.FrameColor, 200, CosmeticRarity.Common),
        new(77, FrameColorKey("pink"), CosmeticKind.FrameColor, 200, CosmeticRarity.Common),
    ];

    // Retired cosmetics (§17.15): no longer sold or worn, and refunded to their owners by the
    // RetireBadgesAndShopTitles, RetireAvatarAccessories and ShopRethink migrations. Kept only
    // so the ledger can still name them; their ids are never reused.
    public static readonly CosmeticItem[] Retired =
    [
        // Name colours (rama 57): just coloured text, and a name can already hold emojis.
        new(21, "color-coral", CosmeticKind.NameColor, 250, CosmeticRarity.Common),
        new(22, "color-tangerine", CosmeticKind.NameColor, 250, CosmeticRarity.Common),
        new(23, "color-gold", CosmeticKind.NameColor, 250, CosmeticRarity.Common),
        new(24, "color-mint", CosmeticKind.NameColor, 250, CosmeticRarity.Common),
        new(25, "color-sky", CosmeticKind.NameColor, 250, CosmeticRarity.Common),
        new(26, "color-lavender", CosmeticKind.NameColor, 250, CosmeticRarity.Common),
        new(27, "color-pink", CosmeticKind.NameColor, 250, CosmeticRarity.Common),
        new(28, "color-lime", CosmeticKind.NameColor, 250, CosmeticRarity.Common),

        // App themes (rama 57): only the owner saw them, so they showed nothing off.
        new(61, "theme-ocean", CosmeticKind.Theme, 250, CosmeticRarity.Common),
        new(62, "theme-strawberry", CosmeticKind.Theme, 250, CosmeticRarity.Common),
        new(63, "theme-grape", CosmeticKind.Theme, 250, CosmeticRarity.Common),
        new(64, "theme-tangerine", CosmeticKind.Theme, 250, CosmeticRarity.Common),

        // Avatar accessories (rama 43): drawn over the photo, so anyone could fake one by
        // uploading a picture with the sticker already on it.
        new(11, "acc-sunglasses", CosmeticKind.Accessory, 200, CosmeticRarity.Common),
        new(12, "acc-party-hat", CosmeticKind.Accessory, 200, CosmeticRarity.Common),
        new(13, "acc-moustache", CosmeticKind.Accessory, 300, CosmeticRarity.Rare),
        new(14, "acc-headphones", CosmeticKind.Accessory, 300, CosmeticRarity.Rare),
        new(15, "acc-paper-crown", CosmeticKind.Accessory, 400, CosmeticRarity.Epic),
        new(16, "acc-halo", CosmeticKind.Accessory, 500, CosmeticRarity.Legendary),

        // Badge emoji next to the name: a name can already hold emojis.
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

        // Shop titles: a bought title didn't show off anything.
        new(51, "title-pro-gossip", CosmeticKind.Title, 200, CosmeticRarity.Common),
        new(52, "title-undecided", CosmeticKind.Title, 200, CosmeticRarity.Common),
        new(53, "title-devils-advocate", CosmeticKind.Title, 300, CosmeticRarity.Rare),
        new(54, "title-oracle", CosmeticKind.Title, 400, CosmeticRarity.Epic),
    ];

    private static readonly Dictionary<string, CosmeticItem> ByKeyMap = All.ToDictionary(i => i.Key);
    private static readonly Dictionary<int, CosmeticItem> ByIdMap = All.Concat(Retired).ToDictionary(i => i.Id);

    // Every current cosmetic, seasonal ones out of their window included (owners still wear
    // them); not the retired ones.
    public static CosmeticItem? ByKey(string? key) =>
        key is not null && ByKeyMap.TryGetValue(key, out var item) ? item : null;

    // Retired ones too, for the ledger.
    public static CosmeticItem? ById(int? id) =>
        id is int value && ByIdMap.TryGetValue(value, out var item) ? item : null;
}
