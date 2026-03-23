using BE_AI_Tourism.Application.DTOs.Moderation;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Shared.Core;

namespace BE_AI_Tourism.Application.Services.Moderation;

public interface IModerationService
{
    Task<Result> ApproveAsync(ResourceType resourceType, Guid resourceId, ModerationActionRequest request, Guid actorId, string role, ContributorType? contributorType, Guid? actorAdminUnitId);
    Task<Result> RejectAsync(ResourceType resourceType, Guid resourceId, ModerationActionRequest request, Guid actorId, string role, ContributorType? contributorType, Guid? actorAdminUnitId);
    Task<Result<IEnumerable<ModerationLogResponse>>> GetLogsAsync(ResourceType resourceType, Guid resourceId);
}
