using BE_AI_Tourism.Application.DTOs.Community;
using FluentValidation;

namespace BE_AI_Tourism.Application.Validators.Community;

public class CreateCommunityPostRequestValidator : AbstractValidator<CreateCommunityPostRequest>
{
    public CreateCommunityPostRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MaximumLength(5000).WithMessage("Content must not exceed 5000 characters");
    }
}
