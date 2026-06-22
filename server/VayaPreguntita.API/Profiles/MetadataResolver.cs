using AutoMapper;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Entities;

namespace VayaPreguntita.API.Profiles
{
    /// <summary>
    /// Resolves the <see cref="QuestionMetadata"/> for a <see cref="CreateQuestionDto"/>.
    /// Handles the conditional logic required for each QuestionType without using unsupported collection expressions in expression trees.
    /// </summary>
    public class MetadataResolver : IValueResolver<CreateQuestionDto, Question, QuestionMetadata>
    {
        public QuestionMetadata Resolve(
            CreateQuestionDto source,
            Question destination,
            QuestionMetadata destMember,
            ResolutionContext context
        )
        {
            var metadata = new QuestionMetadata();

            // Superlative
            if (source.Type == Enums.QuestionType.Superlative)
            {
                metadata.AllowNobody = source.AllowNobody;
                metadata.BlacklistedUserIds = source.BlacklistedUserIds ?? new List<int>();
            }
            else
            {
                metadata.AllowNobody = false;
                metadata.BlacklistedUserIds = new List<int>();
            }

            // Scale
            if (source.Type == Enums.QuestionType.Scale)
            {
                metadata.RangeMin = source.RangeMin ?? 1;
                metadata.RangeMax = source.RangeMax ?? 10;
                metadata.TargetUserId = source.TargetUserId;
            }

            // Secret Pairing
            if (source.Type == Enums.QuestionType.SecretPairing)
            {
                metadata.MinSelections = 2;
                metadata.MaxSelections = 2;
            }
            else if (source.Type == Enums.QuestionType.CustomPoll)
            {
                metadata.MinSelections = source.MinSelections ?? 1;
                metadata.MaxSelections = source.MaxSelections ?? 1;
            }
            else
            {
                metadata.MinSelections = 0;
                metadata.MaxSelections = 0;
            }

            // Deathmatch
            if (source.Type == Enums.QuestionType.Deathmatch)
            {
                metadata.Teams = source.Teams ?? new List<List<int>>();
            }
            else
            {
                metadata.Teams = new List<List<int>>();
            }

            return metadata;
        }
    }
}
