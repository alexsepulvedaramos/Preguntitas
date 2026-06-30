using VayaPreguntita.API.DTOs.Packs;
using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Services;

public interface IPacksService
{
    Task<List<PackDto>> GetPacksForGroupAsync(int groupId);

    // Returns (success, error). error is "not_found" or "would_leave_zero_enabled" on failure.
    Task<(bool success, string? error)> SetPackEnabledAsync(int groupId, int packId, bool enabled);

    Task<PackTemplatePageDto> GetTemplatesAsync(
        int groupId,
        QuestionType? type = null,
        int? packId = null,
        int? before = null,
        int pageSize = 12
    );
}
