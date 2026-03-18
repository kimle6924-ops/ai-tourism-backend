using BE_AI_Tourism.Application.DTOs.Media;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Shared.Core;

namespace BE_AI_Tourism.Application.Services.Media;

public interface IMediaService
{
    Task<Result<UploadSignatureResponse>> GenerateSignatureAsync(UploadSignatureRequest request, Guid userId, string role, Guid? userAdminUnitId);
    Task<Result<MediaAssetResponse>> FinalizeUploadAsync(FinalizeUploadRequest request, Guid userId, string role, Guid? userAdminUnitId);
    Task<Result<IEnumerable<MediaAssetResponse>>> GetByResourceAsync(ResourceType resourceType, Guid resourceId);
    Task<Result<MediaAssetResponse>> SetPrimaryAsync(Guid mediaId, Guid userId, string role, Guid? userAdminUnitId);
    Task<Result> ReorderAsync(ReorderMediaRequest request, Guid userId, string role, Guid? userAdminUnitId);
    Task<Result> DeleteAsync(Guid mediaId, Guid userId, string role, Guid? userAdminUnitId);
}
