using BE_AI_Tourism.Application.DTOs.User;
using FluentValidation;

namespace BE_AI_Tourism.Application.Validators.User;

public class UpdatePreferencesRequestValidator : AbstractValidator<UpdatePreferencesRequest>
{
    public UpdatePreferencesRequestValidator()
    {
        RuleFor(x => x.CategoryIds)
            .NotNull().WithMessage("Category IDs list is required");
    }
}
