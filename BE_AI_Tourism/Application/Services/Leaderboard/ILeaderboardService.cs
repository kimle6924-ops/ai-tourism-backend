using BE_AI_Tourism.Application.DTOs.Leaderboard;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;

namespace BE_AI_Tourism.Application.Services.Leaderboard;

public interface ILeaderboardService
{
    Task<Result<PaginationResponse<UserLeaderboardItemResponse>>> GetUserLeaderboardAsync(PaginationRequest request);
}
