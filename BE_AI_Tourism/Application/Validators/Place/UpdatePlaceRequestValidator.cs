using BE_AI_Tourism.Application.DTOs.Place;
using FluentValidation;

namespace BE_AI_Tourism.Application.Validators.Place;

public class UpdatePlaceRequestValidator : AbstractValidator<UpdatePlaceRequest>
{
    public UpdatePlaceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required")
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters");

        RuleFor(x => x.AdministrativeUnitId)
            .NotEmpty().WithMessage("Administrative unit is required");
    }
}
