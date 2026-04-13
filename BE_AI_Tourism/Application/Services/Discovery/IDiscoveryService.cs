using BE_AI_Tourism.Application.DTOs.Discovery;
using BE_AI_Tourism.Application.DTOs.Event;
using BE_AI_Tourism.Application.DTOs.Place;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;

namespace BE_AI_Tourism.Application.Services.Discovery;

public interface IDiscoveryService
{
    Task<Result<List<string>>> GetAllTagsAsync();
    Task<Result<PaginationResponse<PlaceResponse>>> SearchPlacesAsync(DiscoveryRequest request);
    Task<Result<PaginationResponse<EventResponse>>> SearchEventsAsync(DiscoveryRequest request);
    Task<Result<PaginationResponse<PlaceResponse>>> SimpleSearchPlacesAsync(SimpleSearchRequest request);
    Task<Result<PaginationResponse<EventResponse>>> SimpleSearchEventsAsync(SimpleSearchRequest request);
    Task<Result<PaginationResponse<PlaceResponse>>> RecommendPlacesAsync(Guid userId, RecommendRequest request);
    Task<Result<PaginationResponse<EventResponse>>> RecommendEventsAsync(Guid userId, RecommendRequest request);
    Task<Result<PaginationResponse<DiscoveryMixItemResponse>>> RecommendMixAsync(Guid userId, RecommendMixRequest request);
    Task<Result<PaginationResponse<PlaceResponse>>> GetPlacesByLocationTagAsync(Guid userId, PlaceByLocationTagRequest request);
    Task<Result<PaginationResponse<EventResponse>>> GetEventsTimelineAsync(Guid userId, EventTimelineRequest request);
}
