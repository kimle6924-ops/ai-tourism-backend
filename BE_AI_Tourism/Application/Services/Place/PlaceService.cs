using BE_AI_Tourism.Application.DTOs.Place;
using BE_AI_Tourism.Application.Services.Scope;
using BE_AI_Tourism.Domain.Entities;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;
using MapsterMapper;

namespace BE_AI_Tourism.Application.Services.Place;

public class PlaceService : IPlaceService
{
    private readonly IRepository<Domain.Entities.Place> _placeRepository;
    private readonly IRepository<AdministrativeUnit> _adminUnitRepository;
    private readonly IScopeService _scopeService;
    private readonly IMapper _mapper;

    public PlaceService(
        IRepository<Domain.Entities.Place> placeRepository,
        IRepository<AdministrativeUnit> adminUnitRepository,
        IScopeService scopeService,
        IMapper mapper)
    {
        _placeRepository = placeRepository;
        _adminUnitRepository = adminUnitRepository;
        _scopeService = scopeService;
        _mapper = mapper;
    }

    public async Task<Result<PlaceResponse>> CreateAsync(CreatePlaceRequest request, Guid userId, string role, Guid? userAdminUnitId)
    {
        var adminUnit = await _adminUnitRepository.GetByIdAsync(request.AdministrativeUnitId);
        if (adminUnit == null)
            return Result.Fail<PlaceResponse>(AppConstants.Administrative.ParentNotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        // Contributor chỉ được tạo Place trong scope của mình
        if (role == UserRole.Contributor.ToString())
        {
            if (!userAdminUnitId.HasValue || !await _scopeService.IsInScopeAsync(userAdminUnitId.Value, request.AdministrativeUnitId))
                return Result.Fail<PlaceResponse>(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.Forbidden);
        }

        var entity = new Domain.Entities.Place
        {
            Name = request.Name,
            Description = request.Description,
            Address = request.Address,
            AdministrativeUnitId = request.AdministrativeUnitId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CategoryIds = request.CategoryIds,
            Tags = request.Tags,
            ModerationStatus = ModerationStatus.Pending,
            CreatedBy = userId
        };

        await _placeRepository.AddAsync(entity);
        return Result.Ok(_mapper.Map<PlaceResponse>(entity), StatusCodes.Status201Created);
    }

    public async Task<Result<PlaceResponse>> GetByIdAsync(Guid id)
    {
        var entity = await _placeRepository.GetByIdAsync(id);
        if (entity == null)
            return Result.Fail<PlaceResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        return Result.Ok(_mapper.Map<PlaceResponse>(entity));
    }

    public async Task<Result<PaginationResponse<PlaceResponse>>> GetApprovedPagedAsync(PaginationRequest request)
    {
        var all = await _placeRepository.FindAsync(p => p.ModerationStatus == ModerationStatus.Approved);
        var items = all.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();
        var responses = items.Select(p => _mapper.Map<PlaceResponse>(p)).ToList();

        return Result.Ok(PaginationResponse<PlaceResponse>.Create(
            responses, all.Count(), request.PageNumber, request.PageSize));
    }

    public async Task<Result<PaginationResponse<PlaceResponse>>> GetAllPagedAsync(PaginationRequest request, string role, Guid? userAdminUnitId)
    {
        IEnumerable<Domain.Entities.Place> all;

        if (role == UserRole.Admin.ToString())
        {
            // Admin thấy tất cả
            all = await _placeRepository.GetAllAsync();
        }
        else if (role == UserRole.Contributor.ToString() && userAdminUnitId.HasValue)
        {
            // Contributor chỉ thấy Places trong scope
            var allPlaces = await _placeRepository.GetAllAsync();
            var filtered = new List<Domain.Entities.Place>();
            foreach (var p in allPlaces)
            {
                if (await _scopeService.IsInScopeAsync(userAdminUnitId.Value, p.AdministrativeUnitId))
                    filtered.Add(p);
            }
            all = filtered;
        }
        else
        {
            all = [];
        }

        var totalCount = all.Count();
        var items = all.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();
        var responses = items.Select(p => _mapper.Map<PlaceResponse>(p)).ToList();

        return Result.Ok(PaginationResponse<PlaceResponse>.Create(
            responses, totalCount, request.PageNumber, request.PageSize));
    }

    public async Task<Result<PlaceResponse>> UpdateAsync(Guid id, UpdatePlaceRequest request, Guid userId, string role, Guid? userAdminUnitId)
    {
        var entity = await _placeRepository.GetByIdAsync(id);
        if (entity == null)
            return Result.Fail<PlaceResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        if (!await HasPermission(entity, userId, role, userAdminUnitId))
            return Result.Fail<PlaceResponse>(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.Forbidden);

        var adminUnit = await _adminUnitRepository.GetByIdAsync(request.AdministrativeUnitId);
        if (adminUnit == null)
            return Result.Fail<PlaceResponse>(AppConstants.Administrative.ParentNotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.Address = request.Address;
        entity.AdministrativeUnitId = request.AdministrativeUnitId;
        entity.Latitude = request.Latitude;
        entity.Longitude = request.Longitude;
        entity.CategoryIds = request.CategoryIds;
        entity.Tags = request.Tags;

        await _placeRepository.UpdateAsync(entity);
        return Result.Ok(_mapper.Map<PlaceResponse>(entity));
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId, string role, Guid? userAdminUnitId)
    {
        var entity = await _placeRepository.GetByIdAsync(id);
        if (entity == null)
            return Result.Fail(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        if (!await HasPermission(entity, userId, role, userAdminUnitId))
            return Result.Fail(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.Forbidden);

        await _placeRepository.DeleteAsync(id);
        return Result.Ok("Place deleted successfully");
    }

    private async Task<bool> HasPermission(Domain.Entities.Place place, Guid userId, string role, Guid? userAdminUnitId)
    {
        if (role == UserRole.Admin.ToString())
            return true;

        if (role == UserRole.Contributor.ToString() && userAdminUnitId.HasValue)
            return await _scopeService.IsInScopeAsync(userAdminUnitId.Value, place.AdministrativeUnitId);

        return false;
    }
}
