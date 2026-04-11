using BE_AI_Tourism.Application.DTOs.Event;
using BE_AI_Tourism.Domain.Enums;
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

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90")
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180")
            .When(x => x.Longitude.HasValue);

        RuleFor(x => x.ScheduleType)
            .IsInEnum().WithMessage("Invalid schedule type");

        When(x => x.ScheduleType == ScheduleType.ExactDate, () =>
        {
            RuleFor(x => x.StartAt)
                .NotNull().WithMessage("Start date is required for ExactDate");

            RuleFor(x => x.EndAt)
                .NotNull().WithMessage("End date is required for ExactDate")
                .GreaterThan(x => x.StartAt).WithMessage("End date must be after start date");
        });

        When(x => x.ScheduleType == ScheduleType.YearlyRecurring, () =>
        {
            RuleFor(x => x.StartMonth)
                .NotNull().WithMessage("StartMonth is required for YearlyRecurring")
                .InclusiveBetween(1, 12).WithMessage("StartMonth must be between 1 and 12");

            RuleFor(x => x.EndMonth)
                .NotNull().WithMessage("EndMonth is required for YearlyRecurring")
                .InclusiveBetween(1, 12).WithMessage("EndMonth must be between 1 and 12");

            RuleFor(x => x.StartDay)
                .NotNull().WithMessage("StartDay is required for YearlyRecurring")
                .InclusiveBetween(1, 31).WithMessage("StartDay must be between 1 and 31");

            RuleFor(x => x.EndDay)
                .NotNull().WithMessage("EndDay is required for YearlyRecurring")
                .InclusiveBetween(1, 31).WithMessage("EndDay must be between 1 and 31");

            RuleFor(x => x)
                .Must(HaveValidYearlyDayMonth)
                .WithMessage("Invalid day/month combination for YearlyRecurring");
        });

        When(x => x.ScheduleType == ScheduleType.MonthlyRecurring, () =>
        {
            RuleFor(x => x.StartDay)
                .NotNull().WithMessage("StartDay is required for MonthlyRecurring")
                .InclusiveBetween(1, 31).WithMessage("StartDay must be between 1 and 31");

            RuleFor(x => x.EndDay)
                .NotNull().WithMessage("EndDay is required for MonthlyRecurring")
                .InclusiveBetween(1, 31).WithMessage("EndDay must be between 1 and 31");
        });
    }

    private static bool HaveValidYearlyDayMonth(UpdateEventRequest request)
    {
        if (!request.StartMonth.HasValue || !request.StartDay.HasValue || !request.EndMonth.HasValue || !request.EndDay.HasValue)
            return false;

        return IsValidMonthDay(request.StartMonth.Value, request.StartDay.Value)
               && IsValidMonthDay(request.EndMonth.Value, request.EndDay.Value);
    }

    private static bool IsValidMonthDay(int month, int day)
    {
        if (month is < 1 or > 12 || day < 1)
            return false;

        var maxDay = DateTime.DaysInMonth(2024, month);
        return day <= maxDay;
    }
}
