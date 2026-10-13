using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.DTOs.Coins;

// One ledger row in GET users/me/coins/history (§17.2).
public class CoinTransactionDto
{
    public int Id { get; set; }
    public int Amount { get; set; }
    public CoinReason Reason { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }

    // The cosmetic bought, for Reason = Cosmetic (rama 42): a ShopCatalog key.
    public string? ItemKey { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CoinHistoryPageDto
{
    public List<CoinTransactionDto> Items { get; set; } = [];

    // Pass as ?before= to load the next (older) page; null when there are no more rows.
    public int? NextBefore { get; set; }
}
