using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Helpers;

// Checks a vote against its question and turns it into Vote rows. Shared by the daily vote,
// the late vote from the history (§17.7) and the flash vote (§17.11).
public static class VotePayload
{
    // Max length of an OpenText answer or a CustomPoll "Otro" free-text answer.
    private const int FreeTextMaxLength = 280;

    // Total CustomPoll selections = picked options + an optional "Otro" free-text answer,
    // which must land within [MinSelections, MaxSelections] and be at least 1.
    private static bool IsPollSelectionCountValid(CreateVoteDto dto, Question question)
    {
        var optionCount = dto.SelectedOptionIds?.Count ?? 0;
        var hasFreeText =
            question.Metadata.AllowOther && !string.IsNullOrWhiteSpace(dto.FreeText);
        var total = optionCount + (hasFreeText ? 1 : 0);

        return total >= 1
            && total >= question.Metadata.MinSelections
            && total <= question.Metadata.MaxSelections;
    }

    public static string? Validate(
        CreateVoteDto dto,
        Question question,
        ISet<int> memberIds
    )
    {
        return question.Type switch
        {
            QuestionType.CustomPoll
                when !question.Metadata.AllowOther && !string.IsNullOrWhiteSpace(dto.FreeText) =>
                "This poll does not allow a free-text answer.",

            QuestionType.CustomPoll when (dto.FreeText?.Length ?? 0) > FreeTextMaxLength =>
                "Free-text answer is too long.",

            QuestionType.CustomPoll
                when (dto.SelectedOptionIds?.Any(id => !question.Options.Any(o => o.Id == id))
                    ?? false) => "One or more selected options are invalid.",

            QuestionType.CustomPoll
                when dto.SelectedOptionIds != null
                    && dto.SelectedOptionIds.Distinct().Count() != dto.SelectedOptionIds.Count =>
                "Selected options must be unique.",

            QuestionType.CustomPoll when !IsPollSelectionCountValid(dto, question) =>
                "Number of selected options is out of the allowed range.",

            QuestionType.OpenText when string.IsNullOrWhiteSpace(dto.FreeText) =>
                "Must provide a free-text answer.",

            QuestionType.OpenText when dto.FreeText!.Length > FreeTextMaxLength =>
                "Free-text answer is too long.",

            QuestionType.Superlative when dto.SelectedTargetUserId == null =>
                "Must select a target user.",

            QuestionType.Superlative
                when dto.SelectedTargetUserId == 0 && !question.Metadata.AllowNobody =>
                "\"Nobody\" is not allowed for this question.",

            QuestionType.Superlative
                when dto.SelectedTargetUserId != 0
                    && question.Metadata.BlacklistedUserIds.Contains(
                        dto.SelectedTargetUserId!.Value
                    ) => "Selected user is blacklisted.",

            QuestionType.Superlative
                when dto.SelectedTargetUserId != 0
                    && !memberIds.Contains(dto.SelectedTargetUserId!.Value) =>
                "Selected user is not a group member.",

            QuestionType.Scale when dto.NumericValue == null => "Must provide a numeric value.",

            QuestionType.Scale
                when dto.NumericValue < (question.Metadata.RangeMin ?? QuestionMetadata.ScaleMin)
                    || dto.NumericValue > (question.Metadata.RangeMax ?? QuestionMetadata.ScaleMax) =>
                "Numeric value out of range.",

            QuestionType.SecretPairing
                when dto.SelectedTargetUserIds == null
                    || dto.SelectedTargetUserIds.Distinct().Count() != 2 =>
                "Must select exactly 2 distinct users.",

            QuestionType.SecretPairing when !dto.SelectedTargetUserIds!.All(memberIds.Contains) =>
                "Selected users must be group members.",

            QuestionType.Deathmatch
                when dto.SelectedTargetUserIds == null
                    || !question.Metadata.Teams.Any(t =>
                        t.OrderBy(id => id)
                            .SequenceEqual(dto.SelectedTargetUserIds.OrderBy(id => id))
                    ) => "Must select a valid team.",

            _ => null, // valid
        };
    }

    public static List<Vote> Build(CreateVoteDto dto, Question question, int userId)
    {
        var votes = new List<Vote>();

        switch (question.Type)
        {
            case QuestionType.CustomPoll:
                foreach (var optionId in dto.SelectedOptionIds ?? [])
                    votes.Add(
                        new Vote
                        {
                            UserId = userId,
                            QuestionId = question.Id,
                            SelectedOptionId = optionId,
                        }
                    );
                // An "Otro" answer rides along as an extra row carrying only FreeText.
                if (question.Metadata.AllowOther && !string.IsNullOrWhiteSpace(dto.FreeText))
                    votes.Add(
                        new Vote
                        {
                            UserId = userId,
                            QuestionId = question.Id,
                            FreeText = dto.FreeText!.Trim(),
                        }
                    );
                break;

            case QuestionType.OpenText:
                votes.Add(
                    new Vote
                    {
                        UserId = userId,
                        QuestionId = question.Id,
                        FreeText = dto.FreeText!.Trim(),
                    }
                );
                break;

            case QuestionType.Superlative:
                votes.Add(
                    new Vote
                    {
                        UserId = userId,
                        QuestionId = question.Id,
                        SelectedTargetUserId =
                            dto.SelectedTargetUserId == 0
                                ? null // Nobody
                                : dto.SelectedTargetUserId,
                    }
                );
                break;

            case QuestionType.Scale:
                votes.Add(
                    new Vote
                    {
                        UserId = userId,
                        QuestionId = question.Id,
                        NumericValue = dto.NumericValue,
                    }
                );
                break;

            case QuestionType.SecretPairing:
                foreach (var targetId in dto.SelectedTargetUserIds!)
                    votes.Add(
                        new Vote
                        {
                            UserId = userId,
                            QuestionId = question.Id,
                            SelectedTargetUserId = targetId,
                        }
                    );
                break;

            case QuestionType.Deathmatch:
                var matchedTeamLeader = question
                    .Metadata.Teams.FirstOrDefault(t =>
                        t.OrderBy(id => id)
                            .SequenceEqual((dto.SelectedTargetUserIds ?? []).OrderBy(id => id))
                    )
                    ?.First();

                if (matchedTeamLeader.HasValue)
                    votes.Add(
                        new Vote
                        {
                            UserId = userId,
                            QuestionId = question.Id,
                            SelectedTargetUserId = matchedTeamLeader,
                        }
                    );
                break;
        }

        return votes;
    }
}
