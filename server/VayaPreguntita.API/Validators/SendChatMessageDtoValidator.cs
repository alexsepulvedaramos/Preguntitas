using FluentValidation;
using VayaPreguntita.API.DTOs.Chat;
using VayaPreguntita.API.Localization;

namespace VayaPreguntita.API.Validators;

public class SendChatMessageDtoValidator : AbstractValidator<SendChatMessageDto>
{
    public SendChatMessageDtoValidator()
    {
        RuleFor(x => x.Body)
            .NotEmpty().WithMessage(_ => ServerText.ChatMessageEmpty)
            .MaximumLength(300).WithMessage(_ => ServerText.ChatMessageTooLong(300));
    }
}
