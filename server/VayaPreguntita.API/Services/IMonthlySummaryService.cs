using VayaPreguntita.API.DTOs.MonthlySummaries;

namespace VayaPreguntita.API.Services;

// Monthly summary ("wrapped", §17.12): snapshots built by the tick, read by the group page.
public interface IMonthlySummaryService
{
    // Tick: builds last month's summary for up to `batchSize` groups that don't have it yet,
    // and sends the push for those whose question already opened today.
    Task BuildPendingAsync(int batchSize = MonthlySummaryService.BatchSize);

    // Tick, right after a group's daily activation: sends last month's push if it's ready
    // and hasn't gone out yet.
    Task NotifyIfReadyAsync(int groupId, string groupName);

    Task<List<MonthlySummaryListItemDto>> ListAsync(int groupId);

    // null when the month is malformed or the group has no summary for it.
    Task<MonthlySummaryDto?> GetAsync(int groupId, int userId, string month);
}
