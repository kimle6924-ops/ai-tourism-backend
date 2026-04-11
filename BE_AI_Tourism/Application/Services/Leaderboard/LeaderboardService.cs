using BE_AI_Tourism.Application.DTOs.Leaderboard;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;

namespace BE_AI_Tourism.Application.Services.Leaderboard;

public class LeaderboardService : ILeaderboardService
{
    private readonly IRepository<Domain.Entities.User> _userRepository;
    private readonly IRepository<Domain.Entities.Review> _reviewRepository;

    public LeaderboardService(
        IRepository<Domain.Entities.User> userRepository,
        IRepository<Domain.Entities.Review> reviewRepository)
    {
        _userRepository = userRepository;
        _reviewRepository = reviewRepository;
    }

    public async Task<Result<PaginationResponse<UserLeaderboardItemResponse>>> GetUserLeaderboardAsync(PaginationRequest request)
    {
        var users = (await _userRepository.FindAsync(u => u.Role == UserRole.User)).ToList();
        var activeReviews = (await _reviewRepository.FindAsync(r => r.Status == ReviewStatus.Active)).ToList();

        var reviewGroups = activeReviews
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var ranked = users
            .Select(u =>
            {
                reviewGroups.TryGetValue(u.Id, out var reviews);
                reviews ??= [];

                var totalScore = reviews.Sum(GetReviewScore);
                var totalReviews = reviews.Count;
                var avgScorePerReview = totalReviews > 0
                    ? Math.Round((double)totalScore / totalReviews, 2)
                    : 0d;

                return new UserLeaderboardItemResponse
                {
                    UserId = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    AvatarUrl = u.AvatarUrl,
                    TotalScore = totalScore,
                    TotalReviews = totalReviews,
                    AvgScorePerReview = avgScorePerReview
                };
            })
            .OrderByDescending(x => x.TotalScore)
            .ThenByDescending(x => x.TotalReviews)
            .ThenBy(x => x.FullName)
            .ToList();

        for (var i = 0; i < ranked.Count; i++)
            ranked[i].Rank = i + 1;

        var totalCount = ranked.Count;
        var items = ranked
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return Result.Ok(PaginationResponse<UserLeaderboardItemResponse>.Create(
            items, totalCount, request.PageNumber, request.PageSize));
    }

    private static int GetReviewScore(Domain.Entities.Review review)
    {
        var score = 0;

        if (review.Rating.HasValue)
            score++;
        if (!string.IsNullOrWhiteSpace(review.Comment))
            score++;
        if (!string.IsNullOrWhiteSpace(review.ImageUrl))
            score++;

        return score;
    }
}
