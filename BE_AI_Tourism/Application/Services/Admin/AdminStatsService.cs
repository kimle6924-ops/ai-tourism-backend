using BE_AI_Tourism.Application.DTOs.Admin;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Shared.Core;

namespace BE_AI_Tourism.Application.Services.Admin;

public class AdminStatsService : IAdminStatsService
{
    private readonly IRepository<Domain.Entities.User> _userRepository;
    private readonly IRepository<Domain.Entities.Place> _placeRepository;
    private readonly IRepository<Domain.Entities.Event> _eventRepository;
    private readonly IRepository<Domain.Entities.Review> _reviewRepository;
    private readonly IRepository<Domain.Entities.AiConversation> _conversationRepository;
    private readonly IRepository<Domain.Entities.AiMessage> _messageRepository;
    private readonly IRepository<Domain.Entities.Category> _categoryRepository;
    private readonly IRepository<Domain.Entities.AdministrativeUnit> _adminUnitRepository;
    private readonly IRepository<Domain.Entities.MediaAsset> _mediaRepository;

    public AdminStatsService(
        IRepository<Domain.Entities.User> userRepository,
        IRepository<Domain.Entities.Place> placeRepository,
        IRepository<Domain.Entities.Event> eventRepository,
        IRepository<Domain.Entities.Review> reviewRepository,
        IRepository<Domain.Entities.AiConversation> conversationRepository,
        IRepository<Domain.Entities.AiMessage> messageRepository,
        IRepository<Domain.Entities.Category> categoryRepository,
        IRepository<Domain.Entities.AdministrativeUnit> adminUnitRepository,
        IRepository<Domain.Entities.MediaAsset> mediaRepository)
    {
        _userRepository = userRepository;
        _placeRepository = placeRepository;
        _eventRepository = eventRepository;
        _reviewRepository = reviewRepository;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _categoryRepository = categoryRepository;
        _adminUnitRepository = adminUnitRepository;
        _mediaRepository = mediaRepository;
    }

    public async Task<Result<StatsOverviewResponse>> GetOverviewAsync()
    {
        var users = (await _userRepository.GetAllAsync()).ToList();
        var places = (await _placeRepository.GetAllAsync()).ToList();
        var events = (await _eventRepository.GetAllAsync()).ToList();
        var reviews = (await _reviewRepository.GetAllAsync()).ToList();
        var conversations = (await _conversationRepository.GetAllAsync()).ToList();
        var messages = (await _messageRepository.GetAllAsync()).ToList();
        var categories = (await _categoryRepository.GetAllAsync()).ToList();
        var adminUnits = (await _adminUnitRepository.GetAllAsync()).ToList();
        var media = (await _mediaRepository.GetAllAsync()).ToList();

        var activeReviews = reviews.Where(r => r.Status == ReviewStatus.Active).ToList();

        var response = new StatsOverviewResponse
        {
            Users = new UserStats
            {
                Total = users.Count,
                Admins = users.Count(u => u.Role == UserRole.Admin),
                Contributors = users.Count(u => u.Role == UserRole.Contributor),
                RegularUsers = users.Count(u => u.Role == UserRole.User),
                Active = users.Count(u => u.Status == UserStatus.Active),
                Locked = users.Count(u => u.Status == UserStatus.Locked)
            },
            Places = new PlaceStats
            {
                Total = places.Count,
                Pending = places.Count(p => p.ModerationStatus == ModerationStatus.Pending),
                Approved = places.Count(p => p.ModerationStatus == ModerationStatus.Approved),
                Rejected = places.Count(p => p.ModerationStatus == ModerationStatus.Rejected)
            },
            Events = new EventStats
            {
                Total = events.Count,
                Pending = events.Count(e => e.ModerationStatus == ModerationStatus.Pending),
                Approved = events.Count(e => e.ModerationStatus == ModerationStatus.Approved),
                Rejected = events.Count(e => e.ModerationStatus == ModerationStatus.Rejected),
                Upcoming = events.Count(e => e.EventStatus == EventStatus.Upcoming),
                Ongoing = events.Count(e => e.EventStatus == EventStatus.Ongoing),
                Ended = events.Count(e => e.EventStatus == EventStatus.Ended)
            },
            Reviews = new ReviewStats
            {
                Total = activeReviews.Count,
                AverageRating = activeReviews.Any() ? Math.Round(activeReviews.Average(r => r.Rating), 2) : 0
            },
            Chat = new ChatStats
            {
                TotalConversations = conversations.Count,
                TotalMessages = messages.Count
            },
            Content = new ContentStats
            {
                Categories = categories.Count,
                AdministrativeUnits = adminUnits.Count,
                MediaAssets = media.Count
            }
        };

        return Result.Ok(response);
    }
}
