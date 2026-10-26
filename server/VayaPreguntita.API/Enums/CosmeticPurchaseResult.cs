namespace VayaPreguntita.API.Enums;

public enum CosmeticPurchaseResult
{
    Success,
    UnknownItem,
    AlreadyOwned,
    NotEnoughCoins,
    // A seasonal item outside its sale window (rama 57).
    NotOnSale,
}
