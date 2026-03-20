using BE_AI_Tourism.Application.DTOs.Discovery;
using BE_AI_Tourism.Application.DTOs.Event;
using BE_AI_Tourism.Application.DTOs.Place;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;
using MapsterMapper;

namespace BE_AI_Tourism.Application.Services.Discovery;

public class DiscoveryService : IDiscoveryService
{
    private readonly IRepository<Domain.Entities.Place> _placeRepository;
    private readonly IRepository<Domain.Entities.Event> _eventRepository;
    private readonly IRepository<Domain.Entities.Review> _reviewRepository;
    private readonly IMapper _mapper;

    public DiscoveryService(
        IRepository<Domain.Entities.Place> placeRepository,
        IRepository<Domain.Entities.Event> eventRepository,
        IRepository<Domain.Entities.Review> reviewRepository,
        IMapper mapper)
    {
        _placeRepository = placeRepository;
        _eventRepository = eventRepository;
        _reviewRepository = reviewRepository;
        _mapper = mapper;
    }

    public async Task<Result<PaginationResponse<PlaceResponse>>> SearchPlacesAsync(DiscoveryRequest request)
    {
        var all = await _placeRepository.FindAsync(p => p.ModerationStatus == ModerationStatus.Approved);
        var query = all.AsQueryable();

        query = ApplyPlaceFilters(query, request);
        query = await SortPlaces(query, request.SortBy);

        var totalCount = query.Count();
        var items = query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();
        var responses = items.Select(p => _mapper.Map<PlaceResponse>(p)).ToList();
        await EnrichPlaceAverageRatingsAsync(responses);

        return Result.Ok(PaginationResponse<PlaceResponse>.Create(
            responses, totalCount, request.PageNumber, request.PageSize));
    }

    public async Task<Result<PaginationResponse<EventResponse>>> SearchEventsAsync(DiscoveryRequest request)
    {
        var all = await _eventRepository.FindAsync(e => e.ModerationStatus == ModerationStatus.Approved);
        var query = all.AsQueryable();

        query = ApplyEventFilters(query, request);
        query = await SortEvents(query, request.SortBy);

        var totalCount = query.Count();
        var items = query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();
        var responses = items.Select(e => _mapper.Map<EventResponse>(e)).ToList();
        await EnrichEventAverageRatingsAsync(responses);

        return Result.Ok(PaginationResponse<EventResponse>.Create(
            responses, totalCount, request.PageNumber, request.PageSize));
    }

    private static IQueryable<Domain.Entities.Place> ApplyPlaceFilters(IQueryable<Domain.Entities.Place> query, DiscoveryRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(search) ||
                p.Description.ToLower().Contains(search));
        }

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryIds.Contains(request.CategoryId.Value));

        if (request.AdministrativeUnitId.HasValue)
            query = query.Where(p => p.AdministrativeUnitId == request.AdministrativeUnitId.Value);

        if (!string.IsNullOrWhiteSpace(request.Tag))
        {
            var tag = request.Tag.ToLower();
            query = query.Where(p => p.Tags.Any(t => t.ToLower().Contains(tag)));
        }

        return query;
    }

    private static IQueryable<Domain.Entities.Event> ApplyEventFilters(IQueryable<Domain.Entities.Event> query, DiscoveryRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(e =>
                e.Title.ToLower().Contains(search) ||
                e.Description.ToLower().Contains(search));
        }

        if (request.CategoryId.HasValue)
            query = query.Where(e => e.CategoryIds.Contains(request.CategoryId.Value));

        if (request.AdministrativeUnitId.HasValue)
            query = query.Where(e => e.AdministrativeUnitId == request.AdministrativeUnitId.Value);

        if (!string.IsNullOrWhiteSpace(request.Tag))
        {
            var tag = request.Tag.ToLower();
            query = query.Where(e => e.Tags.Any(t => t.ToLower().Contains(tag)));
        }

        return query;
    }

    private async Task<IQueryable<Domain.Entities.Place>> SortPlaces(IQueryable<Domain.Entities.Place> query, string sortBy)
    {
        return sortBy.ToLower() switch
        {
            "rating" => await SortByAverageRating(query),
            "name" => query.OrderBy(p => p.Name),
            "oldest" => query.OrderBy(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt) // newest
        };
    }

    private async Task<IQueryable<Domain.Entities.Event>> SortEvents(IQueryable<Domain.Entities.Event> query, string sortBy)
    {
        return sortBy.ToLower() switch
        {
            "rating" => await SortByAverageRatingEvents(query),
            "name" => query.OrderBy(e => e.Title),
            "oldest" => query.OrderBy(e => e.CreatedAt),
            "startdate" => query.OrderBy(e => e.StartAt),
            _ => query.OrderByDescending(e => e.CreatedAt) // newest
        };
    }

    private async Task<IQueryable<Domain.Entities.Place>> SortByAverageRating(IQueryable<Domain.Entities.Place> query)
    {
        var placeIds = query.Select(p => p.Id).ToList();
        var reviews = await _reviewRepository.FindAsync(
            r => r.ResourceType == ResourceType.Place && placeIds.Contains(r.ResourceId) && r.Status == ReviewStatus.Active);

        var avgRatings = reviews
            .GroupBy(r => r.ResourceId)
            .ToDictionary(g => g.Key, g => g.Average(r => r.Rating));

        return query.OrderByDescending(p =>
            avgRatings.ContainsKey(p.Id) ? avgRatings[p.Id] : 0);
    }

    private async Task<IQueryable<Domain.Entities.Event>> SortByAverageRatingEvents(IQueryable<Domain.Entities.Event> query)
    {
        var eventIds = query.Select(e => e.Id).ToList();
        var reviews = await _reviewRepository.FindAsync(
            r => r.ResourceType == ResourceType.Event && eventIds.Contains(r.ResourceId) && r.Status == ReviewStatus.Active);

        var avgRatings = reviews
            .GroupBy(r => r.ResourceId)
            .ToDictionary(g => g.Key, g => g.Average(r => r.Rating));

        return query.OrderByDescending(e =>
            avgRatings.ContainsKey(e.Id) ? avgRatings[e.Id] : 0);
    }

    private async Task EnrichPlaceAverageRatingsAsync(List<PlaceResponse> responses)
    {
        if (responses.Count == 0)
            return;

        var placeIds = responses.Select(x => x.Id).Distinct().ToList();
        var reviews = await _reviewRepository.FindAsync(
            r => r.ResourceType == ResourceType.Place
                 && placeIds.Contains(r.ResourceId)
                 && r.Status == ReviewStatus.Active);

        var avgRatings = reviews
            .GroupBy(r => r.ResourceId)
            .ToDictionary(g => g.Key, g => g.Average(r => r.Rating));

        foreach (var response in responses)
            response.AverageRating = avgRatings.TryGetValue(response.Id, out var avg) ? avg : 0;
    }

    private async Task EnrichEventAverageRatingsAsync(List<EventResponse> responses)
    {
        if (responses.Count == 0)
            return;

        var eventIds = responses.Select(x => x.Id).Distinct().ToList();
        var reviews = await _reviewRepository.FindAsync(
            r => r.ResourceType == ResourceType.Event
                 && eventIds.Contains(r.ResourceId)
                 && r.Status == ReviewStatus.Active);

        var avgRatings = reviews
            .GroupBy(r => r.ResourceId)
            .ToDictionary(g => g.Key, g => g.Average(r => r.Rating));

        foreach (var response in responses)
            response.AverageRating = avgRatings.TryGetValue(response.Id, out var avg) ? avg : 0;
    }
}
