namespace BE_AI_Tourism.Application.DTOs.Community;

public class CommunityPostResponse
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserAvatarUrl { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int ReactionCount { get; set; }
    public int CommentCount { get; set; }
    public List<CommunityPostMediaResponse> Media { get; set; } = [];
    public List<CommunityCommentResponse> Comments { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
