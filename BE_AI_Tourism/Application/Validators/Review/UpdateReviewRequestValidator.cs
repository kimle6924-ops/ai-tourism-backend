using BE_AI_Tourism.Application.DTOs.Review;
using FluentValidation;

namespace BE_AI_Tourism.Application.Validators.Review;

public class UpdateReviewRequestValidator : AbstractValidator<UpdateReviewRequest>
{
    public UpdateReviewRequestValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5")
            .When(x => x.Rating.HasValue);

        RuleFor(x => x.Comment)
            .MaximumLength(1000).WithMessage("Comment must not exceed 1000 characters");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(1000).WithMessage("ImageUrl must not exceed 1000 characters")
            .Must(url => string.IsNullOrWhiteSpace(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("ImageUrl must be a valid absolute URL");

        RuleFor(x => x)
            .Must(x =>
                x.Rating.HasValue
                || !string.IsNullOrWhiteSpace(x.Comment)
                || !string.IsNullOrWhiteSpace(x.ImageUrl))
            .WithMessage("At least one of rating, comment or imageUrl is required");
    }
}
