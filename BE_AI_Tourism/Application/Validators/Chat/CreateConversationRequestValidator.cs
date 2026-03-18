using BE_AI_Tourism.Application.DTOs.Chat;
using FluentValidation;

namespace BE_AI_Tourism.Application.Validators.Chat;

public class CreateConversationRequestValidator : AbstractValidator<CreateConversationRequest>
{
    public CreateConversationRequestValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");
    }
}
