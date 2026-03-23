using BE_AI_Tourism.Application.DTOs.Auth;
using BE_AI_Tourism.Domain.Enums;
using FluentValidation;

namespace BE_AI_Tourism.Application.Validators.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters");

        // Chặn client tự tạo Admin
        RuleFor(x => x.Role)
            .Must(role => role != UserRole.Admin)
            .When(x => x.Role.HasValue)
            .WithMessage("Cannot register as Admin");

        // Contributor bắt buộc có ContributorType
        RuleFor(x => x.ContributorType)
            .NotNull().WithMessage("ContributorType is required for Contributor")
            .When(x => x.Role == UserRole.Contributor);

        // Contributor Province/Ward/Collaborator bắt buộc có AdministrativeUnitId
        RuleFor(x => x.AdministrativeUnitId)
            .NotNull().WithMessage("AdministrativeUnitId is required")
            .NotEqual(Guid.Empty).WithMessage("AdministrativeUnitId must not be empty")
            .When(x => x.Role == UserRole.Contributor
                && x.ContributorType.HasValue
                && x.ContributorType.Value != ContributorType.Central);

        // Sở thích bắt buộc ít nhất 1 chỉ với User (role=User hoặc không truyền role -> mặc định User)
        RuleFor(x => x.CategoryIds)
            .NotEmpty().WithMessage("At least one category preference is required")
            .When(x => !x.Role.HasValue || x.Role == UserRole.User);
    }
}
