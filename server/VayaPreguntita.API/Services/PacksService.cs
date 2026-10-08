using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Packs;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Services;

public class PacksService(AppDbContext context) : IPacksService
{
    public async Task<List<PackDto>> GetPacksForGroupAsync(int groupId)
    {
        var disabledPackIds = await context
            .GroupDisabledPacks.Where(gdp => gdp.GroupId == groupId)
            .Select(gdp => gdp.PackId)
            .ToHashSetAsync();

        return await context
            .Packs.Select(p => new PackDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Enabled = p.IsActiveByDefault && !disabledPackIds.Contains(p.Id),
            })
            .ToListAsync();
    }

    public async Task<(bool success, string? error)> SetPackEnabledAsync(
        int groupId,
        int packId,
        bool enabled
    )
    {
        var pack = await context.Packs.FindAsync(packId);
        if (pack == null)
            return (false, "not_found");

        var existingDisable = await context.GroupDisabledPacks.FirstOrDefaultAsync(gdp =>
            gdp.GroupId == groupId && gdp.PackId == packId
        );

        if (enabled)
        {
            if (existingDisable != null)
                context.GroupDisabledPacks.Remove(existingDisable);
        }
        else if (existingDisable == null)
        {
            // Reject if disabling this pack would leave the group with zero enabled packs
            // (§6.1 invariant: there must always be a question available).
            var disabledPackIds = await context
                .GroupDisabledPacks.Where(gdp => gdp.GroupId == groupId)
                .Select(gdp => gdp.PackId)
                .ToHashSetAsync();
            disabledPackIds.Add(packId);

            var stillEnabled = await context.Packs.AnyAsync(p =>
                p.IsActiveByDefault && !disabledPackIds.Contains(p.Id)
            );
            if (!stillEnabled)
                return (false, "would_leave_zero_enabled");

            context.GroupDisabledPacks.Add(new GroupDisabledPack { GroupId = groupId, PackId = packId });
        }

        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<PackTemplatePageDto> GetTemplatesAsync(
        int groupId,
        QuestionType? type = null,
        int? packId = null,
        int? before = null,
        int pageSize = 12
    )
    {
        var enabledPackIds = await TemplateReusePolicy.GetEnabledPackIdsAsync(context, groupId);
        var usedTemplateIds = await TemplateReusePolicy.GetUsedTemplateIdsAsync(context, groupId);
        var exhausted = await TemplateReusePolicy.IsGroupExhaustedAsync(
            context,
            groupId,
            enabledPackIds,
            usedTemplateIds
        );

        var query = context.QuestionTemplates.Include(t => t.Options).Where(t => !t.IsRetired && enabledPackIds.Contains(t.PackId));

        if (type != null)
            query = query.Where(t => t.Type == type);
        if (packId != null)
            query = query.Where(t => t.PackId == packId);

        // While unused templates remain, only those are browsable. Once the group is
        // exhausted, every enabled-pack template becomes browsable again (§6.4).
        if (!exhausted)
            query = query.Where(t => !usedTemplateIds.Contains(t.Id));

        List<QuestionTemplate> ordered;
        if (exhausted)
        {
            // Least-recently-used first, so the longest-unseen repeats surface before fresher ones.
            var lastUsedByTemplate = await context
                .DailyEntries.Where(d =>
                    d.GroupId == groupId && d.ActivatedAt != null && d.Question.TemplateId != null
                )
                .GroupBy(d => d.Question.TemplateId!.Value)
                .Select(g => new { TemplateId = g.Key, LastUsed = g.Max(d => d.ActivatedAt) })
                .ToDictionaryAsync(x => x.TemplateId, x => x.LastUsed);

            var all = await query.ToListAsync();
            ordered = all
                .OrderBy(t => lastUsedByTemplate.TryGetValue(t.Id, out var lastUsed) ? lastUsed : DateTime.MinValue)
                .ThenBy(t => t.Id)
                .ToList();
        }
        else
        {
            ordered = await query.OrderBy(t => t.Id).ToListAsync();
        }

        var startIndex = 0;
        if (before != null)
        {
            var cursorIndex = ordered.FindIndex(t => t.Id == before);
            startIndex = cursorIndex >= 0 ? cursorIndex + 1 : 0;
        }

        var pageItems = ordered.Skip(startIndex).Take(pageSize + 1).ToList();
        var hasMore = pageItems.Count > pageSize;
        if (hasMore)
            pageItems.RemoveAt(pageItems.Count - 1);

        var items = pageItems
            .Select(t => new PackTemplateDto
            {
                Id = t.Id,
                Text = t.Text,
                Type = t.Type,
                PackId = t.PackId,
                Options = t.Options.Select(o => o.Text).ToList(),
            })
            .ToList();

        return new PackTemplatePageDto { Items = items, HasMore = hasMore };
    }
}
