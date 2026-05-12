using FluentValidation;
using VayaPreguntita.API.DTOs;

namespace VayaPreguntita.API.Validators;

public class CreateVoteDtoValidator : AbstractValidator<CreateVoteDto>
{
    public CreateVoteDtoValidator()
    {
        RuleFor(vote => vote.QuestionId).GreaterThan(0);

        RuleForEach(vote => vote.SelectedOptionIds).GreaterThan(0);

        RuleForEach(vote => vote.SelectedTargetUserIds).GreaterThan(0);

        RuleFor(vote => vote.SelectedOptionIds)
            .Must(ids => ids == null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Selected option IDs must be unique.");

        RuleFor(vote => vote.SelectedTargetUserIds)
            .Must(ids => ids == null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Selected target user IDs must be unique.");

        RuleFor(vote => vote)
            .Must(HasExactlyOneAnswer)
            .WithMessage("Exactly one answer field must be provided.");
    }

    private static bool HasExactlyOneAnswer(CreateVoteDto vote)
    {
        var answerCount = 0;

        if (vote.SelectedOptionIds != null && vote.SelectedOptionIds.Count > 0)
        {
            answerCount++;
        }

        if (vote.SelectedTargetUserId.HasValue)
        {
            answerCount++;
        }

        if (vote.NumericValue.HasValue)
        {
            answerCount++;
        }

        if (vote.SelectedTargetUserIds != null && vote.SelectedTargetUserIds.Count > 0)
        {
            answerCount++;
        }

        return answerCount == 1;
    }
}
