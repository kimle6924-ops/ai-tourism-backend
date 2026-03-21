using BE_AI_Tourism.Application.DTOs.Administrative;
using FluentValidation;

namespace BE_AI_Tourism.Application.Validators.Administrative;

public class CreateAdministrativeUnitRequestValidator : AbstractValidator<CreateAdministrativeUnitRequest>
{
    public CreateAdministrativeUnitRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required")
            .MaximumLength(50).WithMessage("Code must not exceed 50 characters");

        RuleFor(x => x.Level)
            .IsInEnum().WithMessage("Invalid administrative level");

        RuleFor(x => x.ParentId)
            .NotNull().When(x => x.Level == Domain.Enums.AdministrativeLevel.Ward)
            .WithMessage("ParentId is required for Ward level");

        RuleFor(x => x.ParentId)
            .Null().When(x => x.Level == Domain.Enums.AdministrativeLevel.Province)
            .WithMessage("Province level must not have a parent");
    }
}
