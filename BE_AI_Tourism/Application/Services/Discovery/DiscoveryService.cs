using BE_AI_Tourism.Application.DTOs.Discovery;
using BE_AI_Tourism.Application.DTOs.Event;
using BE_AI_Tourism.Application.DTOs.Place;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;
using BE_AI_Tourism.Shared.Utils;
using MapsterMapper;
using Microsoft.AspNetCore.Http;

namespace BE_AI_Tourism.Application.Services.Discovery;

public class DiscoveryService : IDiscoveryService
{
    private const string NoLocationErrorMessage = "Chưa cập nhật vị trí. Hãy gọi PUT /api/user/me/location trước.";
    private readonly IRepository<Domain.Entities.Place> _placeRepository;
    private readonly IRepository<Domain.Entities.Event> _eventRepository;
    private readonly IRepository<Domain.Entities.Review> _reviewRepository;
    private readonly IRepository<Domain.Entities.MediaAsset> _mediaRepository;
    private readonly IRepository<Domain.Entities.User> _userRepository;
    private readonly IRepository<Domain.Entities.UserPreference> _preferenceRepository;
    private readonly IMapper _mapper;

    public DiscoveryService(
        IRepository<Domain.Entities.Place> placeRepository,
        IRepository<Domain.Entities.Event> eventRepository,
        IRepository<Domain.Entities.Review> reviewRepository,
        IRepository<Domain.Entities.MediaAsset> mediaRepository,
        IRepository<Domain.Entities.User> userRepository,
        IRepository<Domain.Entities.UserPreference> preferenceRepository,
        IMapper mapper)
    {
        _placeRepository = placeRepository;
        _eventRepository = eventRepository;
        _reviewRepository = reviewRepository;
        _mediaRepository = mediaRepository;
        _userRepository = userRepository;
        _preferenceRepository = preferenceRepository;
        _mapper = mapper;
    }

    public async Task<Result<List<string>>> GetAllTagsAsync()
    {
        var approvedPlaces = await _placeRepository.FindAsync(p => p.ModerationStatus == ModerationStatus.Approved);
        var approvedEvents = await _eventRepository.FindAsync(e => e.ModerationStatus == ModerationStatus.Approved);

        var tags = approvedPlaces
            .SelectMany(p => p.Tags ?? [])
            .Concat(approvedEvents.SelectMany(e => e.Tags ?? []))
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result.Ok(tags);
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
        var nowUtc = DateTime.UtcNow;
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
            r.EventStatus = EventScheduleUtils.ResolveStatus(e, nowUtc);
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
        var nowUtc = DateTime.UtcNow;
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
            r.EventStatus = EventScheduleUtils.ResolveStatus(e, nowUtc);
            r.AverageRating = avgRatings.TryGetValue(e.Id, out var avg) ? avg : 0;
            return r;
        }).ToList();
        await EnrichEventImagesAsync(responses);

        return Result.Ok(PaginationResponse<EventResponse>.Create(
            responses, totalCount, request.PageNumber, request.PageSize));
    }

    // ───────────── RECOMMEND API ─────────────

    public async Task<Result<PaginationResponse<PlaceResponse>>> RecommendPlacesAsync(Guid userId, RecommendRequest request)
    {
        var userCheck = await TryGetUserLocationAsync<PlaceResponse>(userId);
        if (!userCheck.Success)
            return userCheck.Result!;

        var user = userCheck.User!;

        var preference = await _preferenceRepository.FindOneAsync(p => p.UserId == userId);
        var preferredCategoryIds = preference?.CategoryIds ?? [];

        var all = await _placeRepository.FindAsync(p => p.ModerationStatus == ModerationStatus.Approved);
        var places = all.Where(p => p.Latitude.HasValue && p.Longitude.HasValue).ToList();

        // Tính khoảng cách
        var placeDistances = places.ToDictionary(p => p.Id,
            p => HaversineKm(user.Latitude.Value, user.Longitude.Value, p.Latitude!.Value, p.Longitude!.Value));

        // Lọc theo khoảng cách tối đa (nếu có)
        if (request.MaxDistanceKm.HasValue)
            places = places.Where(p => placeDistances[p.Id] <= request.MaxDistanceKm.Value).ToList();

        // Ưu tiên: match sở thích trước, sau đó sort theo khoảng cách gần → xa
        var sorted = places
            .OrderByDescending(p => preferredCategoryIds.Count > 0 && p.CategoryIds.Any(c => preferredCategoryIds.Contains(c)) ? 1 : 0)
            .ThenBy(p => placeDistances[p.Id])
            .ToList();

        var avgRatings = await GetAverageRatings(ResourceType.Place, sorted.Select(p => p.Id));

        var totalCount = sorted.Count;
        var items = sorted
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var responses = items.Select(p =>
        {
            var r = _mapper.Map<PlaceResponse>(p);
            r.AverageRating = avgRatings.TryGetValue(p.Id, out var avg) ? avg : 0;
            r.DistanceKm = Math.Round(placeDistances[p.Id], 2);
            return r;
        }).ToList();
        await EnrichPlaceImagesAsync(responses);

        return Result.Ok(PaginationResponse<PlaceResponse>.Create(
            responses, totalCount, request.PageNumber, request.PageSize));
    }

    public async Task<Result<PaginationResponse<EventResponse>>> RecommendEventsAsync(Guid userId, RecommendRequest request)
    {
        var userCheck = await TryGetUserLocationAsync<EventResponse>(userId);
        if (!userCheck.Success)
            return userCheck.Result!;

        var user = userCheck.User!;

        var preference = await _preferenceRepository.FindOneAsync(p => p.UserId == userId);
        var preferredCategoryIds = preference?.CategoryIds ?? [];

        var nowUtc = DateTime.UtcNow;
        var all = await _eventRepository.FindAsync(e => e.ModerationStatus == ModerationStatus.Approved);
        var events = all
            .Where(e => e.Latitude.HasValue
                        && e.Longitude.HasValue
                        && EventScheduleUtils.ResolveStatus(e, nowUtc) != EventStatus.Ended)
            .ToList();

        var eventDistances = events.ToDictionary(e => e.Id,
            e => HaversineKm(user.Latitude.Value, user.Longitude.Value, e.Latitude!.Value, e.Longitude!.Value));

        if (request.MaxDistanceKm.HasValue)
            events = events.Where(e => eventDistances[e.Id] <= request.MaxDistanceKm.Value).ToList();

        var sorted = events
            .OrderByDescending(e => preferredCategoryIds.Count > 0 && e.CategoryIds.Any(c => preferredCategoryIds.Contains(c)) ? 1 : 0)
            .ThenBy(e => eventDistances[e.Id])
            .ToList();

        var avgRatings = await GetAverageRatings(ResourceType.Event, sorted.Select(e => e.Id));

        var totalCount = sorted.Count;
        var items = sorted
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var responses = items.Select(e =>
        {
            var r = _mapper.Map<EventResponse>(e);
            r.EventStatus = EventScheduleUtils.ResolveStatus(e, nowUtc);
            if (EventScheduleUtils.TryGetCurrentOccurrence(e, nowUtc, out var currentStart, out var currentEnd))
            {
                r.StartAt = currentStart;
                r.EndAt = currentEnd;
            }
            else if (EventScheduleUtils.TryGetNextOccurrence(e, nowUtc, out var nextStart, out var nextEnd))
            {
                r.StartAt = nextStart;
                r.EndAt = nextEnd;
            }
            r.AverageRating = avgRatings.TryGetValue(e.Id, out var avg) ? avg : 0;
            r.DistanceKm = Math.Round(eventDistances[e.Id], 2);
            return r;
        }).ToList();
        await EnrichEventImagesAsync(responses);

        return Result.Ok(PaginationResponse<EventResponse>.Create(
            responses, totalCount, request.PageNumber, request.PageSize));
    }

    public async Task<Result<PaginationResponse<DiscoveryMixItemResponse>>> RecommendMixAsync(Guid userId, RecommendMixRequest request)
    {
        var userCheck = await TryGetUserLocationAsync<DiscoveryMixItemResponse>(userId);
        if (!userCheck.Success)
            return userCheck.Result!;

        var user = userCheck.User!;
        var preference = await _preferenceRepository.FindOneAsync(p => p.UserId == userId);
        var preferredCategoryIds = preference?.CategoryIds ?? [];

        var allPlaces = await _placeRepository.FindAsync(p => p.ModerationStatus == ModerationStatus.Approved);
        var places = allPlaces.Where(p => p.Latitude.HasValue && p.Longitude.HasValue).ToList();

        var nowUtc = DateTime.UtcNow;
        var allEvents = await _eventRepository.FindAsync(e => e.ModerationStatus == ModerationStatus.Approved);
        var events = allEvents
            .Where(e => e.Latitude.HasValue
                        && e.Longitude.HasValue
                        && EventScheduleUtils.ResolveStatus(e, nowUtc) != EventStatus.Ended)
            .ToList();

        var placeRatings = await GetAverageRatings(ResourceType.Place, places.Select(p => p.Id));
        var eventRatings = await GetAverageRatings(ResourceType.Event, events.Select(e => e.Id));

        var placeMediaByResource = await GetMediaByResourceAsync(ResourceType.Place, places.Select(p => p.Id));
        var eventMediaByResource = await GetMediaByResourceAsync(ResourceType.Event, events.Select(e => e.Id));

        var items = new List<DiscoveryMixItemResponse>(places.Count + events.Count);

        foreach (var place in places)
        {
            var distance = HaversineKm(user.Latitude!.Value, user.Longitude!.Value, place.Latitude!.Value, place.Longitude!.Value);
            if (request.MaxDistanceKm.HasValue && distance > request.MaxDistanceKm.Value)
                continue;

            var avgRating = placeRatings.TryGetValue(place.Id, out var rating) ? rating : 0;
            var preferenceMatched = preferredCategoryIds.Count > 0 && place.CategoryIds.Any(c => preferredCategoryIds.Contains(c));
            var score = BuildDiscoveryScore(preferenceMatched, distance, avgRating);

            items.Add(new DiscoveryMixItemResponse
            {
                ResourceType = ResourceType.Place,
                ResourceId = place.Id,
                Title = place.Title,
                Address = place.Address,
                AdministrativeUnitId = place.AdministrativeUnitId,
                Latitude = place.Latitude,
                Longitude = place.Longitude,
                AverageRating = avgRating,
                DistanceKm = Math.Round(distance, 2),
                PrimaryImageUrl = GetPrimaryImageUrl(placeMediaByResource, place.Id),
                PreferenceMatched = preferenceMatched,
                PreferenceMatchScore = score.PreferenceMatchScore,
                DistanceScore = score.DistanceScore,
                RatingScore = score.RatingScore,
                TotalScore = score.TotalScore
            });
        }

        foreach (var evt in events)
        {
            var distance = HaversineKm(user.Latitude!.Value, user.Longitude!.Value, evt.Latitude!.Value, evt.Longitude!.Value);
            if (request.MaxDistanceKm.HasValue && distance > request.MaxDistanceKm.Value)
                continue;

            var avgRating = eventRatings.TryGetValue(evt.Id, out var rating) ? rating : 0;
            var preferenceMatched = preferredCategoryIds.Count > 0 && evt.CategoryIds.Any(c => preferredCategoryIds.Contains(c));
            var score = BuildDiscoveryScore(preferenceMatched, distance, avgRating);

            items.Add(new DiscoveryMixItemResponse
            {
                ResourceType = ResourceType.Event,
                ResourceId = evt.Id,
                Title = evt.Title,
                Address = evt.Address,
                AdministrativeUnitId = evt.AdministrativeUnitId,
                Latitude = evt.Latitude,
                Longitude = evt.Longitude,
                AverageRating = avgRating,
                DistanceKm = Math.Round(distance, 2),
                PrimaryImageUrl = GetPrimaryImageUrl(eventMediaByResource, evt.Id),
                PreferenceMatched = preferenceMatched,
                PreferenceMatchScore = score.PreferenceMatchScore,
                DistanceScore = score.DistanceScore,
                RatingScore = score.RatingScore,
                TotalScore = score.TotalScore
            });
        }

        var sorted = items
            .OrderByDescending(x => x.TotalScore)
            .ThenBy(x => x.DistanceKm)
            .ToList();

        var totalCount = sorted.Count;
        var pagedItems = sorted
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return Result.Ok(PaginationResponse<DiscoveryMixItemResponse>.Create(
            pagedItems, totalCount, request.PageNumber, request.PageSize));
    }

    public async Task<Result<PaginationResponse<PlaceResponse>>> GetPlacesByLocationTagAsync(Guid userId, PlaceByLocationTagRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Tag))
            return Result.Fail<PaginationResponse<PlaceResponse>>("Tag is required", StatusCodes.Status400BadRequest, "VALIDATION_FAILED");

        var userCheck = await TryGetUserLocationAsync<PlaceResponse>(userId);
        if (!userCheck.Success)
            return userCheck.Result!;

        var user = userCheck.User!;
        var normalizedTag = request.Tag.RemoveDiacritics();

        var all = await _placeRepository.FindAsync(p => p.ModerationStatus == ModerationStatus.Approved);
        var places = all
            .Where(p => p.Latitude.HasValue
                        && p.Longitude.HasValue
                        && p.Tags.Any(t => t.RemoveDiacritics().Contains(normalizedTag)))
            .ToList();

        var placeDistances = places.ToDictionary(
            p => p.Id,
            p => HaversineKm(user.Latitude!.Value, user.Longitude!.Value, p.Latitude!.Value, p.Longitude!.Value));

        if (request.RadiusKm.HasValue)
            places = places.Where(p => placeDistances[p.Id] <= request.RadiusKm.Value).ToList();

        var sorted = places
            .OrderBy(p => placeDistances[p.Id])
            .ThenByDescending(p => p.CreatedAt)
            .ToList();

        var avgRatings = await GetAverageRatings(ResourceType.Place, sorted.Select(p => p.Id));

        var totalCount = sorted.Count;
        var pagedItems = sorted
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var responses = pagedItems.Select(p =>
        {
            var r = _mapper.Map<PlaceResponse>(p);
            r.AverageRating = avgRatings.TryGetValue(p.Id, out var avg) ? avg : 0;
            r.DistanceKm = Math.Round(placeDistances[p.Id], 2);
            return r;
        }).ToList();
        await EnrichPlaceImagesAsync(responses);

        return Result.Ok(PaginationResponse<PlaceResponse>.Create(
            responses, totalCount, request.PageNumber, request.PageSize));
    }

    public async Task<Result<PaginationResponse<EventResponse>>> GetEventsTimelineAsync(Guid userId, EventTimelineRequest request)
    {
        var timeline = string.IsNullOrWhiteSpace(request.Timeline)
            ? "both"
            : request.Timeline.Trim().ToLowerInvariant();
        if (timeline is not ("ongoing" or "upcoming" or "both"))
            return Result.Fail<PaginationResponse<EventResponse>>(
                "Timeline must be one of: ongoing, upcoming, both", StatusCodes.Status400BadRequest, "VALIDATION_FAILED");

        var userCheck = await TryGetUserLocationAsync<EventResponse>(userId);
        if (!userCheck.Success)
            return userCheck.Result!;

        var user = userCheck.User!;
        var nowUtc = DateTime.UtcNow;
        var all = await _eventRepository.FindAsync(e => e.ModerationStatus == ModerationStatus.Approved);
        var events = all.Where(e => e.Latitude.HasValue && e.Longitude.HasValue).ToList();
        var effectiveStatus = events.ToDictionary(e => e.Id, e => EventScheduleUtils.ResolveStatus(e, nowUtc));
        var occurrenceStartMap = events.ToDictionary(e => e.Id, e =>
        {
            if (EventScheduleUtils.TryGetCurrentOccurrence(e, nowUtc, out var currentStart, out _))
                return currentStart;
            if (EventScheduleUtils.TryGetNextOccurrence(e, nowUtc, out var nextStart, out _))
                return nextStart;
            return DateTime.MaxValue;
        });

        events = timeline switch
        {
            "ongoing" => events.Where(e => effectiveStatus[e.Id] == EventStatus.Ongoing).ToList(),
            "upcoming" => events.Where(e => effectiveStatus[e.Id] == EventStatus.Upcoming).ToList(),
            _ => events.Where(e => effectiveStatus[e.Id] is EventStatus.Ongoing or EventStatus.Upcoming).ToList()
        };

        var eventDistances = events.ToDictionary(
            e => e.Id,
            e => HaversineKm(user.Latitude!.Value, user.Longitude!.Value, e.Latitude!.Value, e.Longitude!.Value));

        if (request.RadiusKm.HasValue)
            events = events.Where(e => eventDistances[e.Id] <= request.RadiusKm.Value).ToList();

        var sorted = events
            .OrderBy(e => effectiveStatus[e.Id] == EventStatus.Ongoing ? 0 : 1)
            .ThenBy(e => eventDistances[e.Id])
            .ThenBy(e => occurrenceStartMap[e.Id])
            .ToList();

        var avgRatings = await GetAverageRatings(ResourceType.Event, sorted.Select(e => e.Id));

        var totalCount = sorted.Count;
        var pagedItems = sorted
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var responses = pagedItems.Select(e =>
        {
            var r = _mapper.Map<EventResponse>(e);
            r.EventStatus = effectiveStatus[e.Id];
            if (EventScheduleUtils.TryGetCurrentOccurrence(e, nowUtc, out var currentStart, out var currentEnd))
            {
                r.StartAt = currentStart;
                r.EndAt = currentEnd;
            }
            else if (EventScheduleUtils.TryGetNextOccurrence(e, nowUtc, out var nextStart, out var nextEnd))
            {
                r.StartAt = nextStart;
                r.EndAt = nextEnd;
            }
            r.AverageRating = avgRatings.TryGetValue(e.Id, out var avg) ? avg : 0;
            r.DistanceKm = Math.Round(eventDistances[e.Id], 2);
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
            "startdate" => events.OrderBy(e => e.StartAt ?? DateTime.MaxValue).ToList(),
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
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var rated = g.Where(x => x.Rating.HasValue).Select(x => x.Rating!.Value).ToList();
                    return rated.Count > 0 ? Math.Round(rated.Average(), 1) : 0d;
                });
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

    // ───────────── DISTANCE HELPERS ─────────────

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;

    private async Task<(bool Success, Domain.Entities.User? User, Result<PaginationResponse<T>>? Result)> TryGetUserLocationAsync<T>(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return (false, null, Result.Fail<PaginationResponse<T>>("User not found", StatusCodes.Status404NotFound, "NOT_FOUND"));

        if (!user.Latitude.HasValue || !user.Longitude.HasValue)
            return (false, null, Result.Fail<PaginationResponse<T>>(NoLocationErrorMessage, StatusCodes.Status400BadRequest, "NO_LOCATION"));

        return (true, user, null);
    }

    private static (double PreferenceMatchScore, double DistanceScore, double RatingScore, double TotalScore) BuildDiscoveryScore(
        bool preferenceMatched, double distanceKm, double averageRating)
    {
        var preferenceScore = preferenceMatched ? 1d : 0d;
        var distanceScore = 1d / (1d + Math.Max(distanceKm, 0d));
        var ratingScore = Math.Clamp(averageRating / 5d, 0d, 1d);

        var totalScore = (0.6d * preferenceScore) + (0.25d * distanceScore) + (0.15d * ratingScore);

        return (
            Math.Round(preferenceScore, 4),
            Math.Round(distanceScore, 4),
            Math.Round(ratingScore, 4),
            Math.Round(totalScore, 4));
    }

    private async Task<Dictionary<Guid, List<Domain.Entities.MediaAsset>>> GetMediaByResourceAsync(
        ResourceType resourceType,
        IEnumerable<Guid> resourceIds)
    {
        var ids = resourceIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        var media = await _mediaRepository.FindAsync(
            m => m.ResourceType == resourceType && ids.Contains(m.ResourceId));

        return media
            .OrderBy(m => m.SortOrder)
            .GroupBy(m => m.ResourceId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private static string? GetPrimaryImageUrl(Dictionary<Guid, List<Domain.Entities.MediaAsset>> mediaByResource, Guid resourceId)
    {
        if (!mediaByResource.TryGetValue(resourceId, out var media) || media.Count == 0)
            return null;

        var primary = media.FirstOrDefault(m => m.IsPrimary);
        return primary?.SecureUrl ?? primary?.Url ?? media[0].SecureUrl ?? media[0].Url;
    }
}
