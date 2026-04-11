using BE_AI_Tourism.Application.DTOs.User;
using FluentValidation;

namespace BE_AI_Tourism.Application.Validators.User;

public class FinalizeAvatarUploadRequestValidator : AbstractValidator<FinalizeAvatarUploadRequest>
{
    public FinalizeAvatarUploadRequestValidator()
    {
        RuleFor(x => x.PublicId)
            .NotEmpty().WithMessage("PublicId is required")
            .MaximumLength(500).WithMessage("PublicId must not exceed 500 characters");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Url is required")
            .MaximumLength(1000).WithMessage("Url must not exceed 1000 characters")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _)).WithMessage("Url must be a valid absolute URL");

        RuleFor(x => x.SecureUrl)
            .NotEmpty().WithMessage("SecureUrl is required")
            .MaximumLength(1000).WithMessage("SecureUrl must not exceed 1000 characters")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _)).WithMessage("SecureUrl must be a valid absolute URL");
    }
}
