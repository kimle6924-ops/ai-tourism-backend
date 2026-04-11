using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Shared.Core;

namespace BE_AI_Tourism.Domain.Entities;

public class Review : BaseEntity
{
    public ResourceType ResourceType { get; set; }
    public Guid ResourceId { get; set; }
    public Guid UserId { get; set; }
    public int? Rating { get; set; }
    public string? Comment { get; set; }
    public string? ImageUrl { get; set; }
    public ReviewStatus Status { get; set; } = ReviewStatus.Active;
}
