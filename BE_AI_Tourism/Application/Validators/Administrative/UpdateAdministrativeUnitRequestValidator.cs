using BE_AI_Tourism.Application.DTOs.Administrative;
using FluentValidation;

namespace BE_AI_Tourism.Application.Validators.Administrative;

public class UpdateAdministrativeUnitRequestValidator : AbstractValidator<UpdateAdministrativeUnitRequest>
{
    public UpdateAdministrativeUnitRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required")
            .MaximumLength(50).WithMessage("Code must not exceed 50 characters");
    }
}
