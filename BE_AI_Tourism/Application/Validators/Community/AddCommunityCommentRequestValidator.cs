using BE_AI_Tourism.Application.DTOs.Community;
using FluentValidation;

namespace BE_AI_Tourism.Application.Validators.Community;

public class AddCommunityCommentRequestValidator : AbstractValidator<AddCommunityCommentRequest>
{
    public AddCommunityCommentRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MaximumLength(1000).WithMessage("Content must not exceed 1000 characters");
    }
}
