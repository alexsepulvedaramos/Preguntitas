// Helpers/TemplateCloner.cs
namespace VayaPreguntita.API.Helpers;

using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;

public static class TemplateCloner
{
    // Clones a global QuestionTemplate into a concrete group Question (votes live here).
    // Dynamic base-pack fields (Scale target, Deathmatch teams) are auto-resolved from
    // the membership snapshot passed in (see spec §6.3). The returned Question is NOT
    // tracked — the caller adds it to the context.
    public static Question CloneToGroup(
        QuestionTemplate template,
        int groupId,
        IReadOnlyList<int> activeMemberIds
    )
    {
        var metadata = new QuestionMetadata
        {
            AllowNobody = template.Metadata.AllowNobody,
            BlacklistedUserIds = [.. template.Metadata.BlacklistedUserIds],
            RangeMin = template.Metadata.RangeMin,
            RangeMax = template.Metadata.RangeMax,
            TargetUserId = template.Metadata.TargetUserId,
            MinSelections = template.Metadata.MinSelections,
            MaxSelections = template.Metadata.MaxSelections,
            Teams = template.Metadata.Teams.Select(t => t.ToList()).ToList(),
        };

        // Auto-resolution (§6.3) — resolved at instantiation time.
        if (template.Type == QuestionType.Scale && activeMemberIds.Count > 0)
        {
            metadata.TargetUserId = activeMemberIds[Random.Shared.Next(activeMemberIds.Count)];
        }
        else if (template.Type == QuestionType.Deathmatch && activeMemberIds.Count >= 2)
        {
            metadata.Teams = SplitIntoTwoTeams(activeMemberIds);
        }

        var question = new Question
        {
            Text = template.Text,
            Type = template.Type,
            Source = QuestionSource.Pack,
            CreatorId = null,
            TemplateId = template.Id,
            GroupId = groupId,
            IsUsed = false,
            DateCreated = DateTime.UtcNow,
            Metadata = metadata,
        };

        if (template.Type == QuestionType.CustomPoll && template.Options.Count > 0)
        {
            question.Options = template.Options.Select(o => new Option { Text = o.Text }).ToList();
        }

        return question;
    }

    // Random balanced split of the members into two teams.
    private static List<List<int>> SplitIntoTwoTeams(IReadOnlyList<int> memberIds)
    {
        var shuffled = memberIds.OrderBy(_ => Random.Shared.Next()).ToList();
        var half = (shuffled.Count + 1) / 2; // larger half first when odd
        return [shuffled.Take(half).ToList(), shuffled.Skip(half).ToList()];
    }
}
