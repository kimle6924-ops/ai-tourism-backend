using BE_AI_Tourism.Application.DTOs.User;
using FluentValidation;

namespace BE_AI_Tourism.Application.Validators.User;

public class UpdateAccountRequestValidator : AbstractValidator<UpdateAccountRequest>
{
    public UpdateAccountRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Email != null || x.FullName != null || x.Phone != null)
            .WithMessage("At least one field must be provided");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters")
            .When(x => x.Email != null);

        RuleFor(x => x.FullName)
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters")
            .When(x => x.FullName != null);

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters")
            .When(x => x.Phone != null);
    }
}
