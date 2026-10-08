namespace VayaPreguntita.API.Helpers;

using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;

// Shared "almost never repeat" reuse policy (rama 15 / spec §6.4): a pack template should
// only ever be reused by a group once there is truly nothing unused left — no unused pool
// question and no never-cloned template in any pack enabled for that group. Used by both the
// pack-templates browse list and the daily-selection/preselection paths so they agree on what
// counts as "available".
public static class TemplateReusePolicy
{
    public static async Task<HashSet<int>> GetEnabledPackIdsAsync(AppDbContext context, int groupId)
    {
        var disabledPackIds = await context
            .GroupDisabledPacks.Where(gdp => gdp.GroupId == groupId)
            .Select(gdp => gdp.PackId)
            .ToHashSetAsync();

        return await context
            .Packs.Where(p => p.IsActiveByDefault && !disabledPackIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToHashSetAsync();
    }

    // Templates already cloned at least once in this group, regardless of when.
    public static async Task<HashSet<int>> GetUsedTemplateIdsAsync(AppDbContext context, int groupId)
    {
        return await context
            .Questions.Where(q => q.GroupId == groupId && q.TemplateId != null)
            .Select(q => q.TemplateId!.Value)
            .ToHashSetAsync();
    }

    // True when there is no unused pool question AND no available (never-used, enabled-pack)
    // template left — i.e. reuse is the only option remaining.
    public static async Task<bool> IsGroupExhaustedAsync(
        AppDbContext context,
        int groupId,
        HashSet<int> enabledPackIds,
        HashSet<int> usedTemplateIds
    )
    {
        var hasUnusedPool = await context.Questions.AnyAsync(q => q.GroupId == groupId && !q.IsUsed);
        if (hasUnusedPool)
            return false;

        var hasAvailableTemplate = await context.QuestionTemplates.AnyAsync(t =>
            !t.IsRetired && enabledPackIds.Contains(t.PackId) && !usedTemplateIds.Contains(t.Id)
        );

        return !hasAvailableTemplate;
    }
}
