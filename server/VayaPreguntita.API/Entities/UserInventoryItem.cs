using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Entities;

// How many of a game item a user holds (rama 45, §17.10). Global: not tied to a group.
public class UserInventoryItem
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public InventoryItem Item { get; set; }

    // Never below zero (check constraint); a row at 0 is kept and reused.
    public int Quantity { get; set; }
}
