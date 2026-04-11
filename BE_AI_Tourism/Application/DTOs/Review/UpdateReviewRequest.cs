namespace BE_AI_Tourism.Application.DTOs.Review;

public class UpdateReviewRequest
{
    public int? Rating { get; set; }
    public string? Comment { get; set; }
    public string? ImageUrl { get; set; }
}
