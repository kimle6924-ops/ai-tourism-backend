using BE_AI_Tourism.Application.DTOs.Event;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;

namespace BE_AI_Tourism.Application.Services.Event;

public interface IEventService
{
    Task<Result<EventResponse>> CreateAsync(CreateEventRequest request, Guid userId, string role, Guid? userAdminUnitId);
    Task<Result<EventResponse>> GetByIdAsync(Guid id);
    Task<Result<PaginationResponse<EventResponse>>> GetApprovedPagedAsync(PaginationRequest request);
    Task<Result<PaginationResponse<EventResponse>>> GetAllPagedAsync(PaginationRequest request, string role, Guid? userAdminUnitId);
    Task<Result<EventResponse>> UpdateAsync(Guid id, UpdateEventRequest request, Guid userId, string role, Guid? userAdminUnitId);
    Task<Result> DeleteAsync(Guid id, Guid userId, string role, Guid? userAdminUnitId);
}
