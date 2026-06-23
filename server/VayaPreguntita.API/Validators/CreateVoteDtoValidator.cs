using FluentValidation;
using VayaPreguntita.API.DTOs.Questions;

namespace VayaPreguntita.API.Validators;

public class CreateVoteDtoValidator : AbstractValidator<CreateVoteDto>
{
    public CreateVoteDtoValidator()
    {
        // QuestionId ya no viene en el DTO — eliminado

        RuleForEach(vote => vote.SelectedOptionIds).GreaterThan(0);

        RuleForEach(vote => vote.SelectedTargetUserIds).GreaterThan(0);

        RuleFor(vote => vote.SelectedOptionIds)
            .Must(ids => ids == null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Selected option IDs must be unique.");

        RuleFor(vote => vote.SelectedTargetUserIds)
            .Must(ids => ids == null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Selected target user IDs must be unique.");

        // Structural guard only: at least one answer field must be present. The exact
        // per-type combination (incl. OpenText FreeText and CustomPoll "Otro" = options +
        // FreeText) is enforced authoritatively in DailyService.ValidateVotePayload.
        RuleFor(vote => vote)
            .Must(HasAnyAnswer)
            .WithMessage("At least one answer field must be provided.");
    }

    private static bool HasAnyAnswer(CreateVoteDto vote)
    {
        if (vote.SelectedOptionIds != null && vote.SelectedOptionIds.Count > 0)
            return true;

        // Includes 0 (Nobody) as a valid answer for Superlative
        if (vote.SelectedTargetUserId.HasValue)
            return true;

        if (vote.NumericValue.HasValue)
            return true;

        if (vote.SelectedTargetUserIds != null && vote.SelectedTargetUserIds.Count > 0)
            return true;

        if (!string.IsNullOrWhiteSpace(vote.FreeText))
            return true;

        return false;
    }
}
