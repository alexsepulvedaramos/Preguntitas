using VayaPreguntita.API.DTOs.Coins;
using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Services;

// Game items bought ahead in the shop and used later in a group (rama 45, §17.10).
public interface IInventoryService
{
    Task<InventoryDto> GetAsync(int userId);

    // Charges the item's price and adds one. Returns false, changing nothing, when coins are short.
    Task<bool> BuyAsync(int userId, InventoryItem item);

    // Takes one away. Returns false, changing nothing, when the user holds none. Joins the
    // caller's transaction, so a failure later in it gives the item back.
    Task<bool> TryConsumeAsync(int userId, InventoryItem item);
}
