using BE_AI_Tourism.Application.DTOs.Chat;
using FluentValidation;

namespace BE_AI_Tourism.Application.Validators.Chat;

public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content is required")
            .MaximumLength(5000).WithMessage("Message must not exceed 5000 characters");
    }
}
