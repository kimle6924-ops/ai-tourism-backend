using BE_AI_Tourism.Application.DTOs.User;
using FluentValidation;

namespace BE_AI_Tourism.Application.Validators.User;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters")
            .When(x => x.FullName != null);

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters")
            .When(x => x.Phone != null);

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(500).WithMessage("Avatar URL must not exceed 500 characters")
            .When(x => x.AvatarUrl != null);
    }
}
