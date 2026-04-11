using BE_AI_Tourism.Application.DTOs.Community;
using FluentValidation;

namespace BE_AI_Tourism.Application.Validators.Community;

public class ReactCommunityPostRequestValidator : AbstractValidator<ReactCommunityPostRequest>
{
    public ReactCommunityPostRequestValidator()
    {
        RuleFor(x => x.ReactionType)
            .NotEmpty().WithMessage("ReactionType is required")
            .MaximumLength(50).WithMessage("ReactionType must not exceed 50 characters");
    }
}
