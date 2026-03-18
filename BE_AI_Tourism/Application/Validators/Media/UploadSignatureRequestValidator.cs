using BE_AI_Tourism.Application.DTOs.Media;
using FluentValidation;

namespace BE_AI_Tourism.Application.Validators.Media;

public class UploadSignatureRequestValidator : AbstractValidator<UploadSignatureRequest>
{
    public UploadSignatureRequestValidator()
    {
        RuleFor(x => x.ResourceType)
            .IsInEnum().WithMessage("Invalid resource type");

        RuleFor(x => x.ResourceId)
            .NotEmpty().WithMessage("Resource ID is required");
    }
}
