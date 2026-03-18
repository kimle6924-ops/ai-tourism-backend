using BE_AI_Tourism.Application.DTOs.Event;
using FluentValidation;

namespace BE_AI_Tourism.Application.Validators.Event;

public class UpdateEventRequestValidator : AbstractValidator<UpdateEventRequest>
{
    public UpdateEventRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required")
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters");

        RuleFor(x => x.AdministrativeUnitId)
            .NotEmpty().WithMessage("Administrative unit is required");

        RuleFor(x => x.StartAt)
            .NotEmpty().WithMessage("Start date is required");

        RuleFor(x => x.EndAt)
            .NotEmpty().WithMessage("End date is required")
            .GreaterThan(x => x.StartAt).WithMessage("End date must be after start date");

        RuleFor(x => x.EventStatus)
            .IsInEnum().WithMessage("Invalid event status");
    }
}
