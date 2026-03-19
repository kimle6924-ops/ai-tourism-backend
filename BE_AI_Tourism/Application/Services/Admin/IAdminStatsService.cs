using BE_AI_Tourism.Application.DTOs.Admin;
using BE_AI_Tourism.Shared.Core;

namespace BE_AI_Tourism.Application.Services.Admin;

public interface IAdminStatsService
{
    Task<Result<StatsOverviewResponse>> GetOverviewAsync(DateTime? fromUtc = null, DateTime? toUtc = null);
}
