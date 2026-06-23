using FluentValidation;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Validators;

public class CreateQuestionDtoValidator : AbstractValidator<CreateQuestionDto>
{
    public CreateQuestionDtoValidator()
    {
        RuleFor(question => question.Text).NotEmpty().MinimumLength(3).MaximumLength(200);

        RuleFor(question => question.Type).IsInEnum();

        // Only Custom Poll questions may let voters add their own "Otro" answer.
        RuleFor(question => question.AllowOther)
            .Equal(false)
            .When(question => question.Type != QuestionType.CustomPoll)
            .WithMessage("Only custom poll questions can allow a free-text \"Otro\" answer.");

        RuleForEach(question => question.Options).SetValidator(new CreateOptionDtoValidator());

        RuleForEach(question => question.BlacklistedUserIds).GreaterThan(0);

        RuleFor(question => question.BlacklistedUserIds)
            .Must(ids => ids == null || ids.Distinct().Count() == ids.Count)
            .When(question =>
                question.BlacklistedUserIds != null && question.BlacklistedUserIds.Count > 1
            )
            .WithMessage("Blacklisted user IDs must be unique.");

        When(
            question => question.Type == QuestionType.Superlative,
            () =>
            {
                RuleFor(question => question.Options)
                    .Must(options => options == null || options.Count == 0)
                    .WithMessage("Superlative questions cannot define options.");

                RuleFor(question => question.Teams)
                    .Must(teams => teams == null || teams.Count == 0)
                    .WithMessage("Superlative questions cannot define teams.");

                RuleFor(question => question.RangeMin).Null();

                RuleFor(question => question.RangeMax).Null();

                RuleFor(question => question.TargetUserId).Null();

                RuleFor(question => question.MinSelections).Null();

                RuleFor(question => question.MaxSelections).Null();
            }
        );

        When(
            question => question.Type == QuestionType.Deathmatch,
            () =>
            {
                RuleFor(question => question.Options)
                    .Must(options => options == null || options.Count == 0)
                    .WithMessage("Deathmatch questions cannot define options.");

                RuleFor(question => question.Teams)
                    .NotNull()
                    .Must(teams => teams.Count is >= 2 and <= 4)
                    .WithMessage("Deathmatch questions must define between 2 and 4 teams.")
                    .Must(TeamsHaveValidMembers)
                    .WithMessage(
                        "Deathmatch teams must have 1 to 4 members each and not repeat users across teams."
                    );

                RuleFor(question => question.RangeMin).Null();

                RuleFor(question => question.RangeMax).Null();

                RuleFor(question => question.TargetUserId).Null();

                RuleFor(question => question.MinSelections).Null();

                RuleFor(question => question.MaxSelections).Null();

                RuleFor(question => question.BlacklistedUserIds)
                    .Must(ids => ids == null || ids.Count == 0)
                    .WithMessage("Deathmatch questions cannot define blacklisted users.");

                RuleFor(question => question.AllowNobody).Equal(false);
            }
        );

        When(
            question => question.Type == QuestionType.Scale,
            () =>
            {
                // TargetUserId is optional: a Scale can rate a person OR a free subject
                // described only by the question text (e.g. "¿Qué nota le pones a Titanic?").
                RuleFor(question => question.TargetUserId)
                    .GreaterThan(0)
                    .When(question => question.TargetUserId.HasValue);

                RuleFor(question => question)
                    .Must(q => AreScaleBoundsValid(q.RangeMin, q.RangeMax))
                    .WithMessage("RangeMin must be less than RangeMax when provided.");

                RuleFor(question => question.Options)
                    .Must(options => options == null || options.Count == 0)
                    .WithMessage("Scale questions cannot define options.");

                RuleFor(question => question.Teams)
                    .Must(teams => teams == null || teams.Count == 0)
                    .WithMessage("Scale questions cannot define teams.");

                RuleFor(question => question.MinSelections).Null();

                RuleFor(question => question.MaxSelections).Null();

                RuleFor(question => question.BlacklistedUserIds)
                    .Must(ids => ids == null || ids.Count == 0)
                    .WithMessage("Scale questions cannot define blacklisted users.");

                RuleFor(question => question.AllowNobody).Equal(false);
            }
        );

        When(
            question => question.Type == QuestionType.SecretPairing,
            () =>
            {
                RuleFor(question => question.MinSelections)
                    .Equal(2)
                    .WithMessage("Secret Pairing questions must require exactly 2 selections.");

                RuleFor(question => question.MaxSelections)
                    .Equal(2)
                    .WithMessage("Secret Pairing questions must allow exactly 2 selections.");

                RuleFor(question => question.Options)
                    .Must(options => options == null || options.Count == 0)
                    .WithMessage("Secret Pairing questions cannot define options.");

                RuleFor(question => question.Teams)
                    .Must(teams => teams == null || teams.Count == 0)
                    .WithMessage("Secret Pairing questions cannot define teams.");

                RuleFor(question => question.RangeMin).Null();

                RuleFor(question => question.RangeMax).Null();

                RuleFor(question => question.TargetUserId).Null();

                RuleFor(question => question.BlacklistedUserIds)
                    .Must(ids => ids == null || ids.Count == 0)
                    .WithMessage("Secret Pairing questions cannot define blacklisted users.");

                RuleFor(question => question.AllowNobody).Equal(false);
            }
        );

        When(
            question => question.Type == QuestionType.CustomPoll,
            () =>
            {
                RuleFor(question => question.Options)
                    .NotNull()
                    .Must(options => options.Count >= 2)
                    .WithMessage("Custom poll questions require at least 2 options.");

                RuleFor(question => question.Teams)
                    .Must(teams => teams == null || teams.Count == 0)
                    .WithMessage("Custom poll questions cannot define teams.");

                RuleFor(question => question.RangeMin).Null();

                RuleFor(question => question.RangeMax).Null();

                RuleFor(question => question.TargetUserId).Null();

                RuleFor(question => question)
                    .Must(HasValidSelectionLimits)
                    .WithMessage("Selection limits must be valid for the provided options.");

                RuleFor(question => question.BlacklistedUserIds)
                    .Must(ids => ids == null || ids.Count == 0)
                    .WithMessage("Custom poll questions cannot define blacklisted users.");

                RuleFor(question => question.AllowNobody).Equal(false);
            }
        );

        When(
            question => question.Type == QuestionType.OpenText,
            () =>
            {
                // Open-ended questions carry no structured metadata — just the text.
                RuleFor(question => question.Options)
                    .Must(options => options == null || options.Count == 0)
                    .WithMessage("Open-text questions cannot define options.");

                RuleFor(question => question.Teams)
                    .Must(teams => teams == null || teams.Count == 0)
                    .WithMessage("Open-text questions cannot define teams.");

                RuleFor(question => question.RangeMin).Null();
                RuleFor(question => question.RangeMax).Null();
                RuleFor(question => question.TargetUserId).Null();
                RuleFor(question => question.MinSelections).Null();
                RuleFor(question => question.MaxSelections).Null();

                RuleFor(question => question.BlacklistedUserIds)
                    .Must(ids => ids == null || ids.Count == 0)
                    .WithMessage("Open-text questions cannot define blacklisted users.");

                RuleFor(question => question.AllowNobody).Equal(false);
            }
        );
    }

    private static bool AreScaleBoundsValid(int? rangeMin, int? rangeMax)
    {
        if (!rangeMin.HasValue && !rangeMax.HasValue)
        {
            return true;
        }

        return rangeMin.HasValue && rangeMax.HasValue && rangeMin.Value < rangeMax.Value;
    }

    private static bool TeamsHaveValidMembers(List<List<int>>? teams)
    {
        if (teams == null || teams.Count is < 2 or > 4)
        {
            return false;
        }

        var allMembers = new HashSet<int>();

        foreach (var team in teams)
        {
            if (team == null || team.Count is < 1 or > 4)
            {
                return false;
            }

            foreach (var memberId in team)
            {
                if (memberId <= 0 || !allMembers.Add(memberId))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool HasValidSelectionLimits(CreateQuestionDto question)
    {
        if (!question.MinSelections.HasValue && !question.MaxSelections.HasValue)
        {
            return true;
        }

        if (!question.MinSelections.HasValue || !question.MaxSelections.HasValue)
        {
            return false;
        }

        if (
            question.MinSelections.Value <= 0
            || question.MaxSelections.Value < question.MinSelections
        )
        {
            return false;
        }

        var optionCount = question.Options?.Count ?? 0;
        return question.MaxSelections.Value <= optionCount;
    }
}
