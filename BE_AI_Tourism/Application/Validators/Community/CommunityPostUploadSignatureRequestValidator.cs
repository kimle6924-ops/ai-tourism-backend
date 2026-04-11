using BE_AI_Tourism.Application.DTOs.Community;
using FluentValidation;

namespace BE_AI_Tourism.Application.Validators.Community;

public class CommunityPostUploadSignatureRequestValidator : AbstractValidator<CommunityPostUploadSignatureRequest>
{
    public CommunityPostUploadSignatureRequestValidator()
    {
        RuleFor(x => x.PostId)
            .NotEmpty().WithMessage("PostId is required");
    }
}
