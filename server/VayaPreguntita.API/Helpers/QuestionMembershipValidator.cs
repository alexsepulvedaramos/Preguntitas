using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Helpers;

// DB-dependent validation that FluentValidation can't express (§9): every user id a
// question references at creation time must be a current group member, and the creator
// cannot blacklist themselves. Shared by POST /questions and the inline-create path of
// daily/select so both enforce identical rules. Returns null when valid, else an error.
public static class QuestionMembershipValidator
{
    public static string? Validate(CreateQuestionDto dto, ISet<int> memberIds, int creatorId)
    {
        switch (dto.Type)
        {
            case QuestionType.Superlative:
                if (dto.BlacklistedUserIds.Contains(creatorId))
                    return "The creator cannot be blacklisted.";
                if (dto.BlacklistedUserIds.Any(id => !memberIds.Contains(id)))
                    return "Blacklisted users must be group members.";
                break;

            case QuestionType.Scale:
                if (dto.TargetUserId is int target && !memberIds.Contains(target))
                    return "Target user must be a group member.";
                break;

            case QuestionType.Deathmatch:
                if (dto.Teams.SelectMany(team => team).Any(id => !memberIds.Contains(id)))
                    return "All team members must be group members.";
                break;

            // SecretPairing / CustomPoll reference no user ids at creation time —
            // their targets are chosen at vote time and validated in VoteAsync.
        }

        return null;
    }
}
