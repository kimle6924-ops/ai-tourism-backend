using BE_AI_Tourism.Application.DTOs.Media;
using FluentValidation;

namespace BE_AI_Tourism.Application.Validators.Media;

public class FinalizeUploadRequestValidator : AbstractValidator<FinalizeUploadRequest>
{
    public FinalizeUploadRequestValidator()
    {
        RuleFor(x => x.ResourceType)
            .IsInEnum().WithMessage("Invalid resource type");

        RuleFor(x => x.ResourceId)
            .NotEmpty().WithMessage("Resource ID is required");

        RuleFor(x => x.PublicId)
            .NotEmpty().WithMessage("Public ID is required");

        RuleFor(x => x.SecureUrl)
            .NotEmpty().WithMessage("Secure URL is required");
    }
}
