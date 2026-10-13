namespace VayaPreguntita.API.Entities;

// A shop cosmetic the user owns, bought once and kept forever (rama 42, §17.10). ItemKey is
// a ShopCatalog key.
public class UserItem
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string ItemKey { get; set; } = string.Empty;
    public DateTime PurchasedAt { get; set; }
}
