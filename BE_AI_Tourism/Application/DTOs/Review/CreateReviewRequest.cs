using BE_AI_Tourism.Domain.Enums;

namespace BE_AI_Tourism.Application.DTOs.Review;

public class CreateReviewRequest
{
    public ResourceType ResourceType { get; set; }
    public Guid ResourceId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string? ImageUrl { get; set; }
}
