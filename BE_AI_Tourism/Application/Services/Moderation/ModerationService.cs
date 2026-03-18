using BE_AI_Tourism.Application.DTOs.Moderation;
using BE_AI_Tourism.Application.Services.Scope;
using BE_AI_Tourism.Domain.Entities;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Core;
using MapsterMapper;

namespace BE_AI_Tourism.Application.Services.Moderation;

public class ModerationService : IModerationService
{
    private readonly IRepository<Domain.Entities.Place> _placeRepository;
    private readonly IRepository<Domain.Entities.Event> _eventRepository;
    private readonly IRepository<ModerationLog> _moderationLogRepository;
    private readonly IScopeService _scopeService;
    private readonly IMapper _mapper;

    public ModerationService(
        IRepository<Domain.Entities.Place> placeRepository,
        IRepository<Domain.Entities.Event> eventRepository,
        IRepository<ModerationLog> moderationLogRepository,
        IScopeService scopeService,
        IMapper mapper)
    {
        _placeRepository = placeRepository;
        _eventRepository = eventRepository;
        _moderationLogRepository = moderationLogRepository;
        _scopeService = scopeService;
        _mapper = mapper;
    }

    public async Task<Result> ApproveAsync(ResourceType resourceType, Guid resourceId, ModerationActionRequest request, Guid actorId, string role, Guid? actorAdminUnitId)
    {
        return await ModerateAsync(resourceType, resourceId, ModerationStatus.Approved, "approve", request.Note, actorId, role, actorAdminUnitId);
    }

    public async Task<Result> RejectAsync(ResourceType resourceType, Guid resourceId, ModerationActionRequest request, Guid actorId, string role, Guid? actorAdminUnitId)
    {
        return await ModerateAsync(resourceType, resourceId, ModerationStatus.Rejected, "reject", request.Note, actorId, role, actorAdminUnitId);
    }

    public async Task<Result<IEnumerable<ModerationLogResponse>>> GetLogsAsync(ResourceType resourceType, Guid resourceId)
    {
        var logs = await _moderationLogRepository.FindAsync(
            l => l.ResourceType == resourceType && l.ResourceId == resourceId);
        var responses = logs.Select(l => _mapper.Map<ModerationLogResponse>(l));
        return Result.Ok(responses);
    }

    private async Task<Result> ModerateAsync(ResourceType resourceType, Guid resourceId, ModerationStatus newStatus, string action, string note, Guid actorId, string role, Guid? actorAdminUnitId)
    {
        Guid adminUnitId;

        if (resourceType == ResourceType.Place)
        {
            var place = await _placeRepository.GetByIdAsync(resourceId);
            if (place == null)
                return Result.Fail(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound);

            if (!await HasModerationPermission(role, actorAdminUnitId, place.AdministrativeUnitId))
                return Result.Fail(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden);

            place.ModerationStatus = newStatus;
            place.ApprovedBy = newStatus == ModerationStatus.Approved ? actorId : null;
            place.ApprovedAt = newStatus == ModerationStatus.Approved ? DateTime.UtcNow : null;
            await _placeRepository.UpdateAsync(place);
            adminUnitId = place.AdministrativeUnitId;
        }
        else
        {
            var evt = await _eventRepository.GetByIdAsync(resourceId);
            if (evt == null)
                return Result.Fail(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound);

            if (!await HasModerationPermission(role, actorAdminUnitId, evt.AdministrativeUnitId))
                return Result.Fail(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden);

            evt.ModerationStatus = newStatus;
            evt.ApprovedBy = newStatus == ModerationStatus.Approved ? actorId : null;
            evt.ApprovedAt = newStatus == ModerationStatus.Approved ? DateTime.UtcNow : null;
            await _eventRepository.UpdateAsync(evt);
            adminUnitId = evt.AdministrativeUnitId;
        }

        var log = new ModerationLog
        {
            ResourceType = resourceType,
            ResourceId = resourceId,
            Action = action,
            Note = note,
            ActedBy = actorId,
            ActedAt = DateTime.UtcNow
        };
        await _moderationLogRepository.AddAsync(log);

        return Result.Ok($"Resource {action}d successfully");
    }

    private async Task<bool> HasModerationPermission(string role, Guid? actorAdminUnitId, Guid resourceAdminUnitId)
    {
        if (role == UserRole.Admin.ToString())
            return true;

        if (role == UserRole.Contributor.ToString() && actorAdminUnitId.HasValue)
            return await _scopeService.IsInScopeAsync(actorAdminUnitId.Value, resourceAdminUnitId);

        return false;
    }
}
