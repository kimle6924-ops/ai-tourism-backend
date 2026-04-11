using BE_AI_Tourism.Application.DTOs.Community;
using FluentValidation;

namespace BE_AI_Tourism.Application.Validators.Community;

public class FinalizeCommunityPostMediaRequestValidator : AbstractValidator<FinalizeCommunityPostMediaRequest>
{
    public FinalizeCommunityPostMediaRequestValidator()
    {
        RuleFor(x => x.PostId)
            .NotEmpty().WithMessage("PostId is required");

        RuleFor(x => x.PublicId)
            .NotEmpty().WithMessage("PublicId is required");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Url is required")
            .MaximumLength(1000).WithMessage("Url must not exceed 1000 characters");

        RuleFor(x => x.SecureUrl)
            .NotEmpty().WithMessage("SecureUrl is required")
            .MaximumLength(1000).WithMessage("SecureUrl must not exceed 1000 characters");

        RuleFor(x => x.Format)
            .NotEmpty().WithMessage("Format is required")
            .MaximumLength(20).WithMessage("Format must not exceed 20 characters");

        RuleFor(x => x.MimeType)
            .NotEmpty().WithMessage("MimeType is required")
            .MaximumLength(100).WithMessage("MimeType must not exceed 100 characters");

        RuleFor(x => x.Bytes)
            .GreaterThan(0).WithMessage("Bytes must be greater than 0");

        RuleFor(x => x.Width)
            .GreaterThan(0).WithMessage("Width must be greater than 0");

        RuleFor(x => x.Height)
            .GreaterThan(0).WithMessage("Height must be greater than 0");
    }
}
