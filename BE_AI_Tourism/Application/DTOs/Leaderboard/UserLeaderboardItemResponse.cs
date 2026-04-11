namespace BE_AI_Tourism.Application.DTOs.Leaderboard;

public class UserLeaderboardItemResponse
{
    public int Rank { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public int TotalScore { get; set; }
    public int TotalReviews { get; set; }
    public double AvgScorePerReview { get; set; }
}
