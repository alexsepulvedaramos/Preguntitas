// Helpers/QuestionMetadataBuilder.cs
namespace VayaPreguntita.API.Helpers;

using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;

public static class QuestionMetadataBuilder
{
    public static QuestionMetadata Build(CreateQuestionDto dto)
    {
        var metadata = new QuestionMetadata();

        if (dto.Type == QuestionType.Superlative)
        {
            metadata.AllowNobody = dto.AllowNobody;
            metadata.BlacklistedUserIds = dto.BlacklistedUserIds ?? [];
        }
        if (dto.Type == QuestionType.Scale)
        {
            metadata.RangeMin = dto.RangeMin ?? 1;
            metadata.RangeMax = dto.RangeMax ?? 10;
            metadata.TargetUserId = dto.TargetUserId;
        }
        if (dto.Type == QuestionType.SecretPairing)
        {
            metadata.MinSelections = 2;
            metadata.MaxSelections = 2;
        }
        if (dto.Type == QuestionType.CustomPoll)
        {
            metadata.MinSelections = dto.MinSelections ?? 1;
            metadata.MaxSelections = dto.MaxSelections ?? 1;
        }
        if (dto.Type == QuestionType.Deathmatch)
        {
            metadata.Teams = dto.Teams ?? [];
        }

        return metadata;
    }
}
