// Data/PackSeeder.cs
namespace VayaPreguntita.API.Data;

using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;

// Startup sync of the global question packs (spec §6) from the embedded catalog
// (Data/Packs/*.json and its translations, see PackCatalog and Data/Packs/README.md).
//
// Templates are matched by their stable `Key`, so wording can change and a template can move
// between packs without creating a duplicate. Rows are never deleted — that would orphan the
// Question.TemplateId FKs of clones already living in real groups. A template the catalog marks
// `retired` simply stops being offered (QuestionTemplate.IsRetired).
//
// Rows seeded before the catalog existed have no Key. The first sync adopts them by matching
// (pack, text) — or the entry's `legacyPack` / `legacyText` hints — and stamps the Key.
// There is only one database (no separate dev/production), so every sync is idempotent and
// never touches anything the catalog does not mention.
public static class PackSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await SyncCatalogAsync(context, PackCatalog.Load());
        await SyncCloneTranslationsAsync(context);

        // One-time backfill: apply default-disabled packs to groups created before this feature.
        // (New groups are handled at creation time — see GroupsService.CreateGroupAsync.)
        var pendingGroupIds = await context
            .Groups.Where(g => !g.DefaultPacksApplied)
            .Select(g => g.Id)
            .ToListAsync();

        foreach (var groupId in pendingGroupIds)
            await ApplyDefaultDisabledPacksAsync(context, groupId);
    }

    public sealed record SyncSummary(int Created, int Adopted, int Updated);

    // Brings the packs and templates in the database in line with the catalog. Idempotent:
    // a second run over the same catalog changes nothing.
    public static async Task<SyncSummary> SyncCatalogAsync(
        AppDbContext context,
        IReadOnlyList<PackFile> catalog
    )
    {
        var packsByName = await context.Packs.ToDictionaryAsync(p => p.Name);
        foreach (var file in catalog)
        {
            if (!packsByName.TryGetValue(file.Name, out var pack))
            {
                pack = new Pack { Name = file.Name, IsActiveByDefault = true };
                context.Packs.Add(pack);
                packsByName[file.Name] = pack;
            }

            // Keep scalar config in sync with the catalog.
            pack.Description = file.Description;
            pack.DisabledByDefault = file.DisabledByDefault;
            pack.NameTranslations = file.Translations.ToDictionary(kv => kv.Key, kv => kv.Value.Name);
            pack.DescriptionTranslations = file.Translations.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.Description
            );
        }

        // Templates need the pack ids, so persist new packs first.
        await context.SaveChangesAsync();

        var templates = await context.QuestionTemplates.Include(t => t.Options).ToListAsync();
        var byKey = templates.Where(t => t.Key != null).ToDictionary(t => t.Key!);
        var unadopted = templates
            .Where(t => t.Key == null)
            .GroupBy(t => (t.PackId, t.Text))
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.Id).First());

        var created = 0;
        var adopted = 0;

        foreach (var file in catalog)
        {
            var pack = packsByName[file.Name];
            foreach (var entry in file.Templates)
            {
                if (!byKey.TryGetValue(entry.Key, out var template))
                {
                    var legacyPackId = entry.LegacyPack is null
                        ? pack.Id
                        : packsByName[entry.LegacyPack].Id;

                    if (unadopted.Remove((legacyPackId, entry.LegacyText ?? entry.Text), out template))
                    {
                        adopted++;
                    }
                    else
                    {
                        template = new QuestionTemplate { Type = entry.Type };
                        context.QuestionTemplates.Add(template);
                        created++;
                    }

                    byKey[entry.Key] = template;
                }

                if (template.Type != entry.Type)
                    throw new InvalidOperationException(
                        $"Template '{entry.Key}' is {template.Type} in the database but {entry.Type} in the catalog. "
                            + "A type cannot change in place: add a new entry (new key) and retire this one."
                    );

                template.Key = entry.Key;
                template.PackId = pack.Id;
                template.Text = entry.Text;
                template.TextTranslations = entry.Translations.ToDictionary(kv => kv.Key, kv => kv.Value.Text);
                template.IsRetired = entry.Retired;
                SyncMetadata(template.Metadata, entry.ToMetadata());
                SyncOptions(context, template, entry);
            }
        }

        context.ChangeTracker.DetectChanges();
        var updated = context
            .ChangeTracker.Entries<QuestionTemplate>()
            .Count(e => e.State == EntityState.Modified);

        await context.SaveChangesAsync();
        return new SyncSummary(created, adopted, updated);
    }

    // Only the fields a catalog entry can define; per-group fields (teams, target) are left alone.
    private static void SyncMetadata(QuestionMetadata current, QuestionMetadata wanted)
    {
        current.MinSelections = wanted.MinSelections;
        current.MaxSelections = wanted.MaxSelections;
        current.AllowOther = wanted.AllowOther;
        current.AllowNobody = wanted.AllowNobody;
        current.RangeMin = wanted.RangeMin;
        current.RangeMax = wanted.RangeMax;
    }

    // Options are copied into each clone (TemplateCloner), so replacing them is safe.
    private static void SyncOptions(AppDbContext context, QuestionTemplate template, TemplateEntry entry)
    {
        var wanted = entry.Options ?? [];
        var current = template.Options.OrderBy(o => o.Id).Select(o => o.Text);
        if (!current.SequenceEqual(wanted))
        {
            context.RemoveRange(template.Options.ToList());
            template.Options.Clear();
            foreach (var text in wanted)
                template.Options.Add(new QuestionTemplateOption { Text = text });
        }

        // Translated options line up with the Spanish ones by position (PackCatalog checks the
        // counts). New rows all have Id 0, so the stable sort keeps their insertion order.
        var options = template.Options.OrderBy(o => o.Id).ToList();
        for (var i = 0; i < options.Count; i++)
        {
            var index = i;
            options[i].TextTranslations = entry
                .Translations.Where(kv => kv.Value.Options?.Count > index)
                .ToDictionary(kv => kv.Key, kv => kv.Value.Options![index]);
        }
    }

    // Pack clones nobody has reworded mirror their template's translations, so a translation
    // added or fixed later also reaches questions already waiting in a group or in its history.
    // A clone whose text no longer matches its template (the selector edited it, or the Spanish
    // wording changed since) is left as it is. Returns the number of clones updated.
    public static async Task<int> SyncCloneTranslationsAsync(AppDbContext context)
    {
        var clones = await context
            .Questions.Include(q => q.Options)
            .Include(q => q.Template!)
                .ThenInclude(t => t.Options)
            .Where(q => q.Source == QuestionSource.Pack && q.TemplateId != null)
            .ToListAsync();

        foreach (var clone in clones)
        {
            var template = clone.Template!;
            var cloneOptions = clone.Options.OrderBy(o => o.Id).ToList();
            var templateOptions = template.Options.OrderBy(o => o.Id).ToList();
            if (
                clone.Text != template.Text
                || !cloneOptions.Select(o => o.Text).SequenceEqual(templateOptions.Select(o => o.Text))
            )
                continue;

            clone.TextTranslations = new(template.TextTranslations);
            for (var i = 0; i < cloneOptions.Count; i++)
                cloneOptions[i].TextTranslations = new(templateOptions[i].TextTranslations);
        }

        context.ChangeTracker.DetectChanges();
        var updated = context.ChangeTracker.Entries<Question>().Count(e => e.State == EntityState.Modified);
        await context.SaveChangesAsync();
        return updated;
    }

    // Seeds a GroupDisabledPack row for every Pack.DisabledByDefault pack the group doesn't
    // already have one for, then marks the group as initialized. Idempotent and admin-safe:
    // once DefaultPacksApplied is set, this never re-disables a pack the admin re-enabled.
    public static async Task ApplyDefaultDisabledPacksAsync(AppDbContext context, int groupId)
    {
        var group = await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
        if (group is null)
            return;

        var defaultOffPackIds = await context
            .Packs.Where(p => p.DisabledByDefault)
            .Select(p => p.Id)
            .ToListAsync();

        var alreadyDisabled = await context
            .GroupDisabledPacks.Where(gdp => gdp.GroupId == groupId)
            .Select(gdp => gdp.PackId)
            .ToHashSetAsync();

        foreach (var packId in defaultOffPackIds.Where(id => !alreadyDisabled.Contains(id)))
            context.GroupDisabledPacks.Add(new GroupDisabledPack { GroupId = groupId, PackId = packId });

        group.DefaultPacksApplied = true;
        await context.SaveChangesAsync();
    }
}
