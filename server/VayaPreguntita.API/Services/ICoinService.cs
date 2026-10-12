using VayaPreguntita.API.DTOs.Coins;
using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Services;

// Granted = coins actually paid (0 when already paid for that RefId or capped out).
public readonly record struct CoinGrant(int Granted, bool CapReached);

public interface ICoinService
{
    // Pays a reward once per (user, reason, refId), applying the daily cap to capped reasons.
    Task<CoinGrant> GrantAsync(int userId, CoinReason reason, int amount, int? groupId, int? refId);

    Task<int> GetBalanceAsync(int userId);

    // Newest first, PageSize rows per page; `before` is the NextBefore of the previous page.
    Task<CoinHistoryPageDto> GetHistoryAsync(int userId, int? before);
}
