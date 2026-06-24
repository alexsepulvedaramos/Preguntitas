using FluentValidation;
using VayaPreguntita.API.DTOs.Chat;

namespace VayaPreguntita.API.Validators;

public class SendChatMessageDtoValidator : AbstractValidator<SendChatMessageDto>
{
    public SendChatMessageDtoValidator()
    {
        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("El mensaje no puede estar vacío.")
            .MaximumLength(300).WithMessage("El mensaje no puede superar 300 caracteres.");
    }
}
