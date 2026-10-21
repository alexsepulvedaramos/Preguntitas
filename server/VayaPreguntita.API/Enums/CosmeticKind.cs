namespace VayaPreguntita.API.Enums;

// The equip slot a shop cosmetic goes into (rama 42, §17.10). Stored as int — never renumber.
public enum CosmeticKind
{
    RingSkin = 0,
    Accessory = 1, // retired (rama 43, §17.15)
    NameColor = 2,
    Badge = 3, // retired (§17.15)
    Title = 4, // retired (§17.15)
    Theme = 5,
}

public enum CosmeticRarity
{
    Common = 0,
    Rare = 1,
    Epic = 2,
    Legendary = 3,
}
