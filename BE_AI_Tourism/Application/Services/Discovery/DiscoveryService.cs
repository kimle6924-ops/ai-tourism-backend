using BE_AI_Tourism.Application.DTOs.Discovery;
using BE_AI_Tourism.Application.DTOs.Event;
using BE_AI_Tourism.Application.DTOs.Place;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;
using BE_AI_Tourism.Shared.Utils;
using MapsterMapper;

namespace BE_AI_Tourism.Application.Services.Discovery;

public class DiscoveryService : IDiscoveryService
{
    private readonly IRepository<Domain.Entities.Place> _placeRepository;
    private readonly IRepository<Domain.Entities.Event> _eventRepository;
    private readonly IRepository<Domain.Entities.Review> _reviewRepository;
    private readonly IRepository<Domain.Entities.MediaAsset> _mediaRepository;
    private readonly IMapper _mapper;

    public DiscoveryService(
        IRepository<Domain.Entities.Place> placeRepository,
        IRepository<Domain.Entities.Event> eventRepository,
        IRepository<Domain.Entities.Review> reviewRepository,
        IRepository<Domain.Entities.MediaAsset> mediaRepository,
        IMapper mapper)
    {
        _placeRepository = placeRepository;
        _eventRepository = eventRepository;
        _reviewRepository = reviewRepository;
        _mediaRepository = mediaRepository;
        _mapper = mapper;
    }

    // ───────────── OLD API (fixed rating sort) ─────────────

    public async Task<Result<PaginationResponse<PlaceResponse>>> SearchPlacesAsync(DiscoveryRequest request)
    {
        var all = await _placeRepository.FindAsync(p => p.ModerationStatus == ModerationStatus.Approved);
        var places = all.ToList();

        places = ApplyPlaceFilters(places, request.Search, request.CategoryId, request.AdministrativeUnitId, request.Tag);

        var avgRatings = await GetAverageRatings(ResourceType.Place, places.Select(p => p.Id));
        var sorted = SortPlaces(places, request.SortBy, avgRatings);

        var totalCount = sorted.Count;
        var items = sorted
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var responses = items.Select(p =>
        {
            var r = _mapper.Map<PlaceResponse>(p);
            r.AverageRating = avgRatings.TryGetValue(p.Id, out var avg) ? avg : 0;
            return r;
        }).ToList();
        await EnrichPlaceImagesAsync(responses);

        return Result.Ok(PaginationResponse<PlaceResponse>.Create(
            responses, totalCount, request.PageNumber, request.PageSize));
    }

    public async Task<Result<PaginationResponse<EventResponse>>> SearchEventsAsync(DiscoveryRequest request)
    {
        var all = await _eventRepository.FindAsync(e => e.ModerationStatus == ModerationStatus.Approved);
        var events = all.ToList();

        events = ApplyEventFilters(events, request.Search, request.CategoryId, request.AdministrativeUnitId, request.Tag);

        var avgRatings = await GetAverageRatings(ResourceType.Event, events.Select(e => e.Id));
        var sorted = SortEvents(events, request.SortBy, avgRatings);

        var totalCount = sorted.Count;
        var items = sorted
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var responses = items.Select(e =>
        {
            var r = _mapper.Map<EventResponse>(e);
            r.AverageRating = avgRatings.TryGetValue(e.Id, out var avg) ? avg : 0;
            return r;
        }).ToList();
        await EnrichEventImagesAsync(responses);

        return Result.Ok(PaginationResponse<EventResponse>.Create(
            responses, totalCount, request.PageNumber, request.PageSize));
    }

    // ───────────── NEW SIMPLE SEARCH API ─────────────

    public async Task<Result<PaginationResponse<PlaceResponse>>> SimpleSearchPlacesAsync(SimpleSearchRequest request)
    {
        var all = await _placeRepository.FindAsync(p => p.ModerationStatus == ModerationStatus.Approved);
        var places = all.ToList();

        places = ApplyPlaceFilters(places, request.Search, null, null, null);

        var avgRatings = await GetAverageRatings(ResourceType.Place, places.Select(p => p.Id));

        if (request.AverageRating.HasValue)
            places = FilterByRating(places, avgRatings, request.AverageRating.Value);

        var sorted = SortPlaces(places, request.SortBy, avgRatings);

        var totalCount = sorted.Count;
        var items = sorted
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var responses = items.Select(p =>
        {
            var r = _mapper.Map<PlaceResponse>(p);
            r.AverageRating = avgRatings.TryGetValue(p.Id, out var avg) ? avg : 0;
            return r;
        }).ToList();
        await EnrichPlaceImagesAsync(responses);

        return Result.Ok(PaginationResponse<PlaceResponse>.Create(
            responses, totalCount, request.PageNumber, request.PageSize));
    }

    public async Task<Result<PaginationResponse<EventResponse>>> SimpleSearchEventsAsync(SimpleSearchRequest request)
    {
        var all = await _eventRepository.FindAsync(e => e.ModerationStatus == ModerationStatus.Approved);
        var events = all.ToList();

        events = ApplyEventFilters(events, request.Search, null, null, null);

        var avgRatings = await GetAverageRatings(ResourceType.Event, events.Select(e => e.Id));

        if (request.AverageRating.HasValue)
            events = FilterByRating(events, avgRatings, request.AverageRating.Value);

        var sorted = SortEvents(events, request.SortBy, avgRatings);

        var totalCount = sorted.Count;
        var items = sorted
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var responses = items.Select(e =>
        {
            var r = _mapper.Map<EventResponse>(e);
            r.AverageRating = avgRatings.TryGetValue(e.Id, out var avg) ? avg : 0;
            return r;
        }).ToList();
        await EnrichEventImagesAsync(responses);

        return Result.Ok(PaginationResponse<EventResponse>.Create(
            responses, totalCount, request.PageNumber, request.PageSize));
    }

    // ───────────── FILTERS ─────────────

    private static List<Domain.Entities.Place> ApplyPlaceFilters(
        List<Domain.Entities.Place> places, string? search, Guid? categoryId, Guid? adminUnitId, string? tag)
    {
        var query = places.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.RemoveDiacritics();
            query = query.Where(p =>
                p.Title.RemoveDiacritics().Contains(s) ||
                p.Description.RemoveDiacritics().Contains(s));
        }

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryIds.Contains(categoryId.Value));

        if (adminUnitId.HasValue)
            query = query.Where(p => p.AdministrativeUnitId == adminUnitId.Value);

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var t = tag.RemoveDiacritics();
            query = query.Where(p => p.Tags.Any(x => x.RemoveDiacritics().Contains(t)));
        }

        return query.ToList();
    }

    private static List<Domain.Entities.Event> ApplyEventFilters(
        List<Domain.Entities.Event> events, string? search, Guid? categoryId, Guid? adminUnitId, string? tag)
    {
        var query = events.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.RemoveDiacritics();
            query = query.Where(e =>
                e.Title.RemoveDiacritics().Contains(s) ||
                e.Description.RemoveDiacritics().Contains(s));
        }

        if (categoryId.HasValue)
            query = query.Where(e => e.CategoryIds.Contains(categoryId.Value));

        if (adminUnitId.HasValue)
            query = query.Where(e => e.AdministrativeUnitId == adminUnitId.Value);

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var t = tag.RemoveDiacritics();
            query = query.Where(e => e.Tags.Any(x => x.RemoveDiacritics().Contains(t)));
        }

        return query.ToList();
    }

    // ───────────── RATING FILTER ─────────────

    private static List<T> FilterByRating<T>(
        List<T> items, Dictionary<Guid, double> avgRatings, int ratingValue)
        where T : Shared.Core.BaseEntity
    {
        if (ratingValue == 5)
            return items.Where(x => avgRatings.TryGetValue(x.Id, out var avg) && avg == 5.0).ToList();

        double min = ratingValue;
        double max = ratingValue + 1;
        return items.Where(x =>
            avgRatings.TryGetValue(x.Id, out var avg) && avg >= min && avg < max).ToList();
    }

    // ───────────── SORTING ─────────────

    private static List<Domain.Entities.Place> SortPlaces(
        List<Domain.Entities.Place> places, string sortBy, Dictionary<Guid, double> avgRatings)
    {
        return sortBy.ToLower() switch
        {
            "rating" => places.OrderByDescending(p => avgRatings.TryGetValue(p.Id, out var avg) ? avg : 0).ToList(),
            "name" => places.OrderBy(p => p.Title).ToList(),
            "oldest" => places.OrderBy(p => p.CreatedAt).ToList(),
            _ => places.OrderByDescending(p => p.CreatedAt).ToList()
        };
    }

    private static List<Domain.Entities.Event> SortEvents(
        List<Domain.Entities.Event> events, string sortBy, Dictionary<Guid, double> avgRatings)
    {
        return sortBy.ToLower() switch
        {
            "rating" => events.OrderByDescending(e => avgRatings.TryGetValue(e.Id, out var avg) ? avg : 0).ToList(),
            "name" => events.OrderBy(e => e.Title).ToList(),
            "oldest" => events.OrderBy(e => e.CreatedAt).ToList(),
            "startdate" => events.OrderBy(e => e.StartAt).ToList(),
            _ => events.OrderByDescending(e => e.CreatedAt).ToList()
        };
    }

    // ───────────── RATING HELPERS ─────────────

    private async Task<Dictionary<Guid, double>> GetAverageRatings(ResourceType resourceType, IEnumerable<Guid> resourceIds)
    {
        var ids = resourceIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, double>();

        var reviews = await _reviewRepository.FindAsync(
            r => r.ResourceType == resourceType
                 && ids.Contains(r.ResourceId)
                 && r.Status == ReviewStatus.Active);

        return reviews
            .GroupBy(r => r.ResourceId)
            .ToDictionary(g => g.Key, g => Math.Round(g.Average(r => r.Rating), 1));
    }

    // ───────────── IMAGE HELPERS ─────────────

    private async Task EnrichPlaceImagesAsync(List<PlaceResponse> responses)
    {
        if (responses.Count == 0) return;
        var ids = responses.Select(x => x.Id).ToList();
        var allMedia = await _mediaRepository.FindAsync(
            m => m.ResourceType == ResourceType.Place && ids.Contains(m.ResourceId));
        var mediaByResource = allMedia.OrderBy(m => m.SortOrder).GroupBy(m => m.ResourceId)
            .ToDictionary(g => g.Key, g => g.ToList());
        foreach (var r in responses)
            if (mediaByResource.TryGetValue(r.Id, out var media))
                r.Images = media.Select(m => _mapper.Map<DTOs.Media.MediaAssetResponse>(m)).ToList();
    }

    private async Task EnrichEventImagesAsync(List<EventResponse> responses)
    {
        if (responses.Count == 0) return;
        var ids = responses.Select(x => x.Id).ToList();
        var allMedia = await _mediaRepository.FindAsync(
            m => m.ResourceType == ResourceType.Event && ids.Contains(m.ResourceId));
        var mediaByResource = allMedia.OrderBy(m => m.SortOrder).GroupBy(m => m.ResourceId)
            .ToDictionary(g => g.Key, g => g.ToList());
        foreach (var r in responses)
            if (mediaByResource.TryGetValue(r.Id, out var media))
                r.Images = media.Select(m => _mapper.Map<DTOs.Media.MediaAssetResponse>(m)).ToList();
    }
}
