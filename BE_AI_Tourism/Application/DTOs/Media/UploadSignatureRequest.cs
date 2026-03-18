using BE_AI_Tourism.Domain.Enums;

namespace BE_AI_Tourism.Application.DTOs.Media;

public class UploadSignatureRequest
{
    public ResourceType ResourceType { get; set; }
    public Guid ResourceId { get; set; }
}
