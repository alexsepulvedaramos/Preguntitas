using FluentValidation;
using VayaPreguntita.API.DTOs.Daily;

namespace VayaPreguntita.API.Validators;

// Validates POST /daily/select. Ensures exactly one source is provided and — crucially —
// runs the SAME structural validation as POST /questions on the inline-create path, which
// previously bypassed FluentValidation entirely (§9). Membership checks (DB-dependent) are
// enforced in DailyService via QuestionMembershipValidator.
public class SelectQuestionDtoValidator : AbstractValidator<SelectQuestionDto>
{
    public SelectQuestionDtoValidator()
    {
        RuleFor(dto => dto)
            .Must(ExactlyOneSource)
            .WithMessage(
                "Provide exactly one of ExistingQuestionId, TemplateId or NewQuestion."
            );

        When(
            dto => dto.NewQuestion != null,
            () =>
                RuleFor(dto => dto.NewQuestion!).SetValidator(new CreateQuestionDtoValidator())
        );
    }

    private static bool ExactlyOneSource(SelectQuestionDto dto)
    {
        var count =
            (dto.ExistingQuestionId.HasValue ? 1 : 0)
            + (dto.TemplateId.HasValue ? 1 : 0)
            + (dto.NewQuestion != null ? 1 : 0);
        return count == 1;
    }
}
