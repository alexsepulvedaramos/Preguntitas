namespace VayaPreguntita.API.Enums;

// The equip slot a shop cosmetic goes into (rama 42, §17.10). Stored as int — never renumber.
public enum CosmeticKind
{
    RingSkin = 0,
    Accessory = 1, // retired (rama 43, §17.15)
    NameColor = 2, // retired (rama 57, §17.15)
    Badge = 3, // retired (§17.15)
    Title = 4, // retired (§17.15)
    Theme = 5, // retired (rama 57, §17.15)
    FrameColor = 6, // a plain frame colour (rama 57): User.FrameColor, owned as "frame-<key>"
}

public enum CosmeticRarity
{
    Common = 0,
    Rare = 1,
    Epic = 2,
    Legendary = 3,
}
