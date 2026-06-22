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

        RuleFor(vote => vote)
            .Must(HasExactlyOneAnswer)
            .WithMessage("Exactly one answer field must be provided.");
    }

    private static bool HasExactlyOneAnswer(CreateVoteDto vote)
    {
        var answerCount = 0;

        if (vote.SelectedOptionIds != null && vote.SelectedOptionIds.Count > 0)
            answerCount++;

        // Incluye 0 (Nobody) como respuesta válida para Superlative
        if (vote.SelectedTargetUserId.HasValue)
            answerCount++;

        if (vote.NumericValue.HasValue)
            answerCount++;

        if (vote.SelectedTargetUserIds != null && vote.SelectedTargetUserIds.Count > 0)
            answerCount++;

        return answerCount == 1;
    }
}
