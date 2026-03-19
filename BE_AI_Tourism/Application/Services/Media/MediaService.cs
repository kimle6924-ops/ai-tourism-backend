using BE_AI_Tourism.Application.DTOs.Media;
using BE_AI_Tourism.Application.Services.Scope;
using BE_AI_Tourism.Configuration;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Infrastructure.Cloudinary;
using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Core;
using MapsterMapper;
using Microsoft.Extensions.Options;

namespace BE_AI_Tourism.Application.Services.Media;

public class MediaService : IMediaService
{
    private readonly IRepository<Domain.Entities.MediaAsset> _mediaRepository;
    private readonly IRepository<Domain.Entities.Place> _placeRepository;
    private readonly IRepository<Domain.Entities.Event> _eventRepository;
    private readonly ICloudinaryProvider _cloudinaryProvider;
    private readonly IScopeService _scopeService;
    private readonly IMapper _mapper;
    private readonly CloudinaryOptions _cloudinaryOptions;

    public MediaService(
        IRepository<Domain.Entities.MediaAsset> mediaRepository,
        IRepository<Domain.Entities.Place> placeRepository,
        IRepository<Domain.Entities.Event> eventRepository,
        ICloudinaryProvider cloudinaryProvider,
        IScopeService scopeService,
        IMapper mapper,
        IOptions<CloudinaryOptions> cloudinaryOptions)
    {
        _mediaRepository = mediaRepository;
        _placeRepository = placeRepository;
        _eventRepository = eventRepository;
        _cloudinaryProvider = cloudinaryProvider;
        _scopeService = scopeService;
        _mapper = mapper;
        _cloudinaryOptions = cloudinaryOptions.Value;
    }

    public async Task<Result<UploadSignatureResponse>> GenerateSignatureAsync(UploadSignatureRequest request, Guid userId, string role, Guid? userAdminUnitId)
    {
        if (!await HasResourcePermission(request.ResourceType, request.ResourceId, userId, role, userAdminUnitId))
            return Result.Fail<UploadSignatureResponse>(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.Forbidden);

        var folder = $"{_cloudinaryOptions.Folder}/{request.ResourceType}/{request.ResourceId}";
        var (signature, timestamp) = _cloudinaryProvider.GenerateSignature(folder);

        var response = new UploadSignatureResponse
        {
            Signature = signature,
            Timestamp = timestamp,
            ApiKey = _cloudinaryOptions.ApiKey,
            CloudName = _cloudinaryOptions.CloudName,
            Folder = folder
        };

        return Result.Ok(response);
    }

    public async Task<Result<MediaAssetResponse>> FinalizeUploadAsync(FinalizeUploadRequest request, Guid userId, string role, Guid? userAdminUnitId)
    {
        if (!await HasResourcePermission(request.ResourceType, request.ResourceId, userId, role, userAdminUnitId))
            return Result.Fail<MediaAssetResponse>(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.Forbidden);

        var existingMedia = await _mediaRepository.FindAsync(
            m => m.ResourceType == request.ResourceType && m.ResourceId == request.ResourceId);
        var isPrimary = !existingMedia.Any();

        var entity = new Domain.Entities.MediaAsset
        {
            ResourceType = request.ResourceType,
            ResourceId = request.ResourceId,
            Url = request.Url,
            SecureUrl = request.SecureUrl,
            PublicId = request.PublicId,
            Format = request.Format,
            MimeType = request.MimeType,
            Bytes = request.Bytes,
            Width = request.Width,
            Height = request.Height,
            IsPrimary = isPrimary,
            SortOrder = existingMedia.Count(),
            UploadedBy = userId
        };

        await _mediaRepository.AddAsync(entity);
        return Result.Ok(_mapper.Map<MediaAssetResponse>(entity), StatusCodes.Status201Created);
    }

    public async Task<Result<IEnumerable<MediaAssetResponse>>> GetByResourceAsync(ResourceType resourceType, Guid resourceId)
    {
        var media = await _mediaRepository.FindAsync(
            m => m.ResourceType == resourceType && m.ResourceId == resourceId);
        var ordered = media.OrderBy(m => m.SortOrder);
        var responses = ordered.Select(m => _mapper.Map<MediaAssetResponse>(m));
        return Result.Ok(responses);
    }

    public async Task<Result<MediaAssetResponse>> SetPrimaryAsync(Guid mediaId, Guid userId, string role, Guid? userAdminUnitId)
    {
        var media = await _mediaRepository.GetByIdAsync(mediaId);
        if (media == null)
            return Result.Fail<MediaAssetResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        if (!await HasResourcePermission(media.ResourceType, media.ResourceId, userId, role, userAdminUnitId))
            return Result.Fail<MediaAssetResponse>(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.Forbidden);

        // Unset current primary
        var allMedia = await _mediaRepository.FindAsync(
            m => m.ResourceType == media.ResourceType && m.ResourceId == media.ResourceId);
        foreach (var m in allMedia.Where(m => m.IsPrimary))
        {
            m.IsPrimary = false;
            await _mediaRepository.UpdateAsync(m);
        }

        // Set new primary
        media.IsPrimary = true;
        await _mediaRepository.UpdateAsync(media);
        return Result.Ok(_mapper.Map<MediaAssetResponse>(media));
    }

    public async Task<Result> ReorderAsync(ReorderMediaRequest request, Guid userId, string role, Guid? userAdminUnitId)
    {
        if (!request.OrderedIds.Any())
            return Result.Fail(AppConstants.ErrorMessages.BadRequest, StatusCodes.Status400BadRequest, AppConstants.ErrorCodes.BadRequest);

        var firstMedia = await _mediaRepository.GetByIdAsync(request.OrderedIds.First());
        if (firstMedia == null)
            return Result.Fail(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        if (!await HasResourcePermission(firstMedia.ResourceType, firstMedia.ResourceId, userId, role, userAdminUnitId))
            return Result.Fail(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.Forbidden);

        for (var i = 0; i < request.OrderedIds.Count; i++)
        {
            var media = await _mediaRepository.GetByIdAsync(request.OrderedIds[i]);
            if (media == null) continue;
            media.SortOrder = i;
            await _mediaRepository.UpdateAsync(media);
        }

        return Result.Ok("Media reordered successfully");
    }

    public async Task<Result> DeleteAsync(Guid mediaId, Guid userId, string role, Guid? userAdminUnitId)
    {
        var media = await _mediaRepository.GetByIdAsync(mediaId);
        if (media == null)
            return Result.Fail(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        if (!await HasResourcePermission(media.ResourceType, media.ResourceId, userId, role, userAdminUnitId))
            return Result.Fail(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.Forbidden);

        // Delete from Cloudinary
        await _cloudinaryProvider.DestroyAsync(media.PublicId);

        var wasPrimary = media.IsPrimary;
        var resourceType = media.ResourceType;
        var resourceId = media.ResourceId;

        await _mediaRepository.DeleteAsync(mediaId);

        // If deleted was primary, promote next one
        if (wasPrimary)
        {
            var remaining = await _mediaRepository.FindAsync(
                m => m.ResourceType == resourceType && m.ResourceId == resourceId);
            var next = remaining.OrderBy(m => m.SortOrder).FirstOrDefault();
            if (next != null)
            {
                next.IsPrimary = true;
                await _mediaRepository.UpdateAsync(next);
            }
        }

        return Result.Ok("Media deleted successfully");
    }

    private async Task<bool> HasResourcePermission(ResourceType resourceType, Guid resourceId, Guid userId, string role, Guid? userAdminUnitId)
    {
        if (role == UserRole.Admin.ToString())
            return true;

        if (role != UserRole.Contributor.ToString() || !userAdminUnitId.HasValue)
            return false;

        Guid targetAdminUnitId;
        if (resourceType == ResourceType.Place)
        {
            var place = await _placeRepository.GetByIdAsync(resourceId);
            if (place == null) return false;
            targetAdminUnitId = place.AdministrativeUnitId;
        }
        else
        {
            var evt = await _eventRepository.GetByIdAsync(resourceId);
            if (evt == null) return false;
            targetAdminUnitId = evt.AdministrativeUnitId;
        }

        return await _scopeService.IsInScopeAsync(userAdminUnitId.Value, targetAdminUnitId);
    }
}
