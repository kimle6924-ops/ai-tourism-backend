using BE_AI_Tourism.Domain.Enums;

namespace BE_AI_Tourism.Application.DTOs.Media;

public class FinalizeUploadRequest
{
    public ResourceType ResourceType { get; set; }
    public Guid ResourceId { get; set; }
    public string PublicId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string SecureUrl { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long Bytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
