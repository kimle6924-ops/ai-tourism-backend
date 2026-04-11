using BE_AI_Tourism.Domain.Enums;

namespace BE_AI_Tourism.Application.DTOs.Review;

public class ReviewHistoryItemResponse
{
    public Guid Id { get; set; }
    public ResourceType ResourceType { get; set; }
    public Guid ResourceId { get; set; }
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserAvatarUrl { get; set; } = string.Empty;
    public int? Rating { get; set; }
    public string? Comment { get; set; }
    public string? ImageUrl { get; set; }
    public ReviewStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string ResourceTitle { get; set; } = string.Empty;
    public string ResourceAddress { get; set; } = string.Empty;
    public string ResourceImageUrl { get; set; } = string.Empty;
}
