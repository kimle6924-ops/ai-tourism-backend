using BE_AI_Tourism.Application.DTOs.Moderation;
using FluentValidation;

namespace BE_AI_Tourism.Application.Validators.Moderation;

public class ModerationActionRequestValidator : AbstractValidator<ModerationActionRequest>
{
    public ModerationActionRequestValidator()
    {
        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Note must not exceed 500 characters");
    }
}
