using BE_AI_Tourism.Application.DTOs.Media;
using FluentValidation;

namespace BE_AI_Tourism.Application.Validators.Media;

public class ReorderMediaRequestValidator : AbstractValidator<ReorderMediaRequest>
{
    public ReorderMediaRequestValidator()
    {
        RuleFor(x => x.OrderedIds)
            .NotEmpty().WithMessage("Ordered IDs list is required");
    }
}
