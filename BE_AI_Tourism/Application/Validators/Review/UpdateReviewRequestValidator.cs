using BE_AI_Tourism.Application.DTOs.Review;
using FluentValidation;

namespace BE_AI_Tourism.Application.Validators.Review;

public class UpdateReviewRequestValidator : AbstractValidator<UpdateReviewRequest>
{
    public UpdateReviewRequestValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5");

        RuleFor(x => x.Comment)
            .MaximumLength(1000).WithMessage("Comment must not exceed 1000 characters");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(1000).WithMessage("ImageUrl must not exceed 1000 characters")
            .Must(url => string.IsNullOrWhiteSpace(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("ImageUrl must be a valid absolute URL");

    }
}
