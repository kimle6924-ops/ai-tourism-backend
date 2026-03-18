using BE_AI_Tourism.Application.DTOs.Administrative;
using BE_AI_Tourism.Domain.Entities;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;
using MapsterMapper;

namespace BE_AI_Tourism.Application.Services.Administrative;

public class AdministrativeUnitService : IAdministrativeUnitService
{
    private readonly IRepository<AdministrativeUnit> _repository;
    private readonly IMapper _mapper;

    public AdministrativeUnitService(IRepository<AdministrativeUnit> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<Result<AdministrativeUnitResponse>> CreateAsync(CreateAdministrativeUnitRequest request)
    {
        var existingCode = await _repository.FindOneAsync(u => u.Code == request.Code);
        if (existingCode != null)
            return Result.Fail<AdministrativeUnitResponse>(AppConstants.Administrative.CodeAlreadyExists);

        if (request.ParentId.HasValue)
        {
            var parent = await _repository.GetByIdAsync(request.ParentId.Value);
            if (parent == null)
                return Result.Fail<AdministrativeUnitResponse>(AppConstants.Administrative.ParentNotFound, StatusCodes.Status404NotFound);

            if (!IsValidChildLevel(parent.Level, request.Level))
                return Result.Fail<AdministrativeUnitResponse>(AppConstants.Administrative.InvalidLevelHierarchy);
        }

        var entity = new AdministrativeUnit
        {
            Name = request.Name,
            Level = request.Level,
            ParentId = request.ParentId,
            Code = request.Code
        };

        await _repository.AddAsync(entity);
        return Result.Ok(_mapper.Map<AdministrativeUnitResponse>(entity), StatusCodes.Status201Created);
    }

    public async Task<Result<AdministrativeUnitResponse>> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
            return Result.Fail<AdministrativeUnitResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound);

        return Result.Ok(_mapper.Map<AdministrativeUnitResponse>(entity));
    }

    public async Task<Result<PaginationResponse<AdministrativeUnitResponse>>> GetPagedAsync(PaginationRequest request)
    {
        var paged = await _repository.GetPagedAsync(request);
        var responses = paged.Items.Select(e => _mapper.Map<AdministrativeUnitResponse>(e)).ToList();

        return Result.Ok(PaginationResponse<AdministrativeUnitResponse>.Create(
            responses, paged.TotalCount, paged.PageNumber, paged.PageSize));
    }

    public async Task<Result<IEnumerable<AdministrativeUnitResponse>>> GetByLevelAsync(AdministrativeLevel level)
    {
        var entities = await _repository.FindAsync(u => u.Level == level);
        var responses = entities.Select(e => _mapper.Map<AdministrativeUnitResponse>(e));
        return Result.Ok(responses);
    }

    public async Task<Result<IEnumerable<AdministrativeUnitResponse>>> GetChildrenAsync(Guid parentId)
    {
        var parent = await _repository.GetByIdAsync(parentId);
        if (parent == null)
            return Result.Fail<IEnumerable<AdministrativeUnitResponse>>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound);

        var children = await _repository.FindAsync(u => u.ParentId == parentId);
        var responses = children.Select(e => _mapper.Map<AdministrativeUnitResponse>(e));
        return Result.Ok(responses);
    }

    public async Task<Result<AdministrativeUnitResponse>> UpdateAsync(Guid id, UpdateAdministrativeUnitRequest request)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
            return Result.Fail<AdministrativeUnitResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound);

        var existingCode = await _repository.FindOneAsync(u => u.Code == request.Code && u.Id != id);
        if (existingCode != null)
            return Result.Fail<AdministrativeUnitResponse>(AppConstants.Administrative.CodeAlreadyExists);

        entity.Name = request.Name;
        entity.Code = request.Code;

        await _repository.UpdateAsync(entity);
        return Result.Ok(_mapper.Map<AdministrativeUnitResponse>(entity));
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
            return Result.Fail(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound);

        var children = await _repository.FindAsync(u => u.ParentId == id);
        if (children.Any())
            return Result.Fail(AppConstants.Administrative.HasChildren);

        await _repository.DeleteAsync(id);
        return Result.Ok("Administrative unit deleted successfully");
    }

    private static bool IsValidChildLevel(AdministrativeLevel parentLevel, AdministrativeLevel childLevel)
    {
        return (parentLevel, childLevel) switch
        {
            (AdministrativeLevel.Central, AdministrativeLevel.Province) => true,
            (AdministrativeLevel.Province, AdministrativeLevel.Ward) => true,
            (AdministrativeLevel.Ward, AdministrativeLevel.Neighborhood) => true,
            _ => false
        };
    }
}
