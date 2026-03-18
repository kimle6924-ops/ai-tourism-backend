using BE_AI_Tourism.Application.DTOs.Event;
using BE_AI_Tourism.Application.Services.Scope;
using BE_AI_Tourism.Domain.Entities;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;
using MapsterMapper;

namespace BE_AI_Tourism.Application.Services.Event;

public class EventService : IEventService
{
    private readonly IRepository<Domain.Entities.Event> _eventRepository;
    private readonly IRepository<AdministrativeUnit> _adminUnitRepository;
    private readonly IScopeService _scopeService;
    private readonly IMapper _mapper;

    public EventService(
        IRepository<Domain.Entities.Event> eventRepository,
        IRepository<AdministrativeUnit> adminUnitRepository,
        IScopeService scopeService,
        IMapper mapper)
    {
        _eventRepository = eventRepository;
        _adminUnitRepository = adminUnitRepository;
        _scopeService = scopeService;
        _mapper = mapper;
    }

    public async Task<Result<EventResponse>> CreateAsync(CreateEventRequest request, Guid userId)
    {
        var adminUnit = await _adminUnitRepository.GetByIdAsync(request.AdministrativeUnitId);
        if (adminUnit == null)
            return Result.Fail<EventResponse>(AppConstants.Administrative.ParentNotFound, StatusCodes.Status404NotFound);

        var entity = new Domain.Entities.Event
        {
            Title = request.Title,
            Description = request.Description,
            Address = request.Address,
            AdministrativeUnitId = request.AdministrativeUnitId,
            CategoryIds = request.CategoryIds,
            Tags = request.Tags,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            EventStatus = EventStatus.Upcoming,
            ModerationStatus = ModerationStatus.Pending,
            CreatedBy = userId
        };

        await _eventRepository.AddAsync(entity);
        return Result.Ok(_mapper.Map<EventResponse>(entity), StatusCodes.Status201Created);
    }

    public async Task<Result<EventResponse>> GetByIdAsync(Guid id)
    {
        var entity = await _eventRepository.GetByIdAsync(id);
        if (entity == null)
            return Result.Fail<EventResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound);

        return Result.Ok(_mapper.Map<EventResponse>(entity));
    }

    public async Task<Result<PaginationResponse<EventResponse>>> GetApprovedPagedAsync(PaginationRequest request)
    {
        var all = await _eventRepository.FindAsync(e => e.ModerationStatus == ModerationStatus.Approved);
        var items = all.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();
        var responses = items.Select(e => _mapper.Map<EventResponse>(e)).ToList();

        return Result.Ok(PaginationResponse<EventResponse>.Create(
            responses, all.Count(), request.PageNumber, request.PageSize));
    }

    public async Task<Result<PaginationResponse<EventResponse>>> GetAllPagedAsync(PaginationRequest request)
    {
        var paged = await _eventRepository.GetPagedAsync(request);
        var responses = paged.Items.Select(e => _mapper.Map<EventResponse>(e)).ToList();

        return Result.Ok(PaginationResponse<EventResponse>.Create(
            responses, paged.TotalCount, paged.PageNumber, paged.PageSize));
    }

    public async Task<Result<EventResponse>> UpdateAsync(Guid id, UpdateEventRequest request, Guid userId, string role, Guid? userAdminUnitId)
    {
        var entity = await _eventRepository.GetByIdAsync(id);
        if (entity == null)
            return Result.Fail<EventResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound);

        if (!await HasPermission(entity, userId, role, userAdminUnitId))
            return Result.Fail<EventResponse>(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden);

        var adminUnit = await _adminUnitRepository.GetByIdAsync(request.AdministrativeUnitId);
        if (adminUnit == null)
            return Result.Fail<EventResponse>(AppConstants.Administrative.ParentNotFound, StatusCodes.Status404NotFound);

        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.Address = request.Address;
        entity.AdministrativeUnitId = request.AdministrativeUnitId;
        entity.CategoryIds = request.CategoryIds;
        entity.Tags = request.Tags;
        entity.StartAt = request.StartAt;
        entity.EndAt = request.EndAt;
        entity.EventStatus = request.EventStatus;

        await _eventRepository.UpdateAsync(entity);
        return Result.Ok(_mapper.Map<EventResponse>(entity));
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId, string role, Guid? userAdminUnitId)
    {
        var entity = await _eventRepository.GetByIdAsync(id);
        if (entity == null)
            return Result.Fail(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound);

        if (!await HasPermission(entity, userId, role, userAdminUnitId))
            return Result.Fail(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden);

        await _eventRepository.DeleteAsync(id);
        return Result.Ok("Event deleted successfully");
    }

    private async Task<bool> HasPermission(Domain.Entities.Event evt, Guid userId, string role, Guid? userAdminUnitId)
    {
        if (role == UserRole.Admin.ToString())
            return true;

        if (role == UserRole.Contributor.ToString() && userAdminUnitId.HasValue)
            return await _scopeService.IsInScopeAsync(userAdminUnitId.Value, evt.AdministrativeUnitId);

        return false;
    }
}
