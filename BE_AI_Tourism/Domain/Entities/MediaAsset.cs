using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Shared.Core;

namespace BE_AI_Tourism.Domain.Entities;

public class MediaAsset : BaseEntity
{
    public ResourceType ResourceType { get; set; }
    public Guid ResourceId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string SecureUrl { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long Bytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
    public Guid UploadedBy { get; set; }
}
