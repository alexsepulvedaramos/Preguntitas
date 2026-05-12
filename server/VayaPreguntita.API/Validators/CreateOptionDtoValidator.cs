using FluentValidation;
using VayaPreguntita.API.DTOs;

namespace VayaPreguntita.API.Validators;

public class CreateOptionDtoValidator : AbstractValidator<CreateOptionDto>
{
    public CreateOptionDtoValidator()
    {
        RuleFor(option => option.Text).NotEmpty().MaximumLength(200);
    }
}
