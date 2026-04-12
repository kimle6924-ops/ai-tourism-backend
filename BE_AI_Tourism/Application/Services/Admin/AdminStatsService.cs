using BE_AI_Tourism.Application.DTOs.Admin;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Infrastructure.Database;
using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Utils;
using Microsoft.EntityFrameworkCore;

namespace BE_AI_Tourism.Application.Services.Admin;

public class AdminStatsService : IAdminStatsService
{
    private readonly AppDbContext _dbContext;

    public AdminStatsService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<StatsOverviewResponse>> GetOverviewAsync(DateTime? fromUtc = null, DateTime? toUtc = null)
    {
        var (rangeFromUtc, rangeToUtc) = ResolveRange(fromUtc, toUtc);
        if (rangeFromUtc > rangeToUtc)
        {
            return Result.Fail<StatsOverviewResponse>(
                "fromUtc must be less than or equal to toUtc",
                StatusCodes.Status400BadRequest,
                AppConstants.ErrorCodes.BadRequest);
        }

        var rangeEndExclusiveUtc = rangeToUtc.Date.AddDays(1);

        // Aggregate metrics (DB-side COUNT/GROUP BY/AVG)
        var totalUsers = await _dbContext.Users.AsNoTracking().CountAsync();
        var usersByRoleRows = await _dbContext.Users.AsNoTracking()
            .GroupBy(u => u.Role)
            .Select(g => new { Value = g.Key, Count = g.Count() })
            .ToListAsync();
        var usersByStatusRows = await _dbContext.Users.AsNoTracking()
            .GroupBy(u => u.Status)
            .Select(g => new { Value = g.Key, Count = g.Count() })
            .ToListAsync();

        var totalPlaces = await _dbContext.Places.AsNoTracking().CountAsync();
        var placesByModerationRows = await _dbContext.Places.AsNoTracking()
            .GroupBy(p => p.ModerationStatus)
            .Select(g => new { Value = g.Key, Count = g.Count() })
            .ToListAsync();

        var totalEvents = await _dbContext.Events.AsNoTracking().CountAsync();
        var allEvents = await _dbContext.Events.AsNoTracking().ToListAsync();
        var eventsByModerationRows = await _dbContext.Events.AsNoTracking()
            .GroupBy(e => e.ModerationStatus)
            .Select(g => new { Value = g.Key, Count = g.Count() })
            .ToListAsync();

        var totalReviews = await _dbContext.Reviews.AsNoTracking().CountAsync();
        var reviewsByStatusRows = await _dbContext.Reviews.AsNoTracking()
            .GroupBy(r => r.Status)
            .Select(g => new { Value = g.Key, Count = g.Count() })
            .ToListAsync();
        var avgActiveRating = await _dbContext.Reviews.AsNoTracking()
            .Where(r => r.Status == ReviewStatus.Active)
            .Select(r => (double?)r.Rating)
            .AverageAsync() ?? 0d;

        var totalConversations = await _dbContext.AiConversations.AsNoTracking().CountAsync();
        var totalMessages = await _dbContext.AiMessages.AsNoTracking().CountAsync();

        var totalCategories = await _dbContext.Categories.AsNoTracking().CountAsync();
        var totalAdministrativeUnits = await _dbContext.AdministrativeUnits.AsNoTracking().CountAsync();
        var totalMediaAssets = await _dbContext.MediaAssets.AsNoTracking().CountAsync();
        var totalMediaBytes = await _dbContext.MediaAssets.AsNoTracking()
            .Select(m => (long?)m.Bytes)
            .SumAsync() ?? 0L;

        // Daily time-series in selected range
        var usersSeries = await BuildDailySeriesAsync(_dbContext.Users.AsNoTracking(), rangeFromUtc, rangeEndExclusiveUtc);
        var placesSeries = await BuildDailySeriesAsync(_dbContext.Places.AsNoTracking(), rangeFromUtc, rangeEndExclusiveUtc);
        var eventsSeries = await BuildDailySeriesAsync(_dbContext.Events.AsNoTracking(), rangeFromUtc, rangeEndExclusiveUtc);
        var reviewsSeries = await BuildDailySeriesAsync(_dbContext.Reviews.AsNoTracking(), rangeFromUtc, rangeEndExclusiveUtc);

        var newConversationsInRange = await _dbContext.AiConversations.AsNoTracking()
            .CountAsync(c => c.CreatedAt >= rangeFromUtc && c.CreatedAt < rangeEndExclusiveUtc);
        var newMessagesInRange = await _dbContext.AiMessages.AsNoTracking()
            .CountAsync(m => m.CreatedAt >= rangeFromUtc && m.CreatedAt < rangeEndExclusiveUtc);

        var userRoleMap = BuildEnumMap(Enum.GetValues<UserRole>(), usersByRoleRows.Select(x => (x.Value, x.Count)));
        var userStatusMap = BuildEnumMap(Enum.GetValues<UserStatus>(), usersByStatusRows.Select(x => (x.Value, x.Count)));
        var placeModerationMap = BuildEnumMap(Enum.GetValues<ModerationStatus>(), placesByModerationRows.Select(x => (x.Value, x.Count)));
        var eventModerationMap = BuildEnumMap(Enum.GetValues<ModerationStatus>(), eventsByModerationRows.Select(x => (x.Value, x.Count)));
        var eventStatusMap = BuildDynamicEventStatusMap(allEvents, DateTime.UtcNow);
        var reviewStatusMap = BuildEnumMap(Enum.GetValues<ReviewStatus>(), reviewsByStatusRows.Select(x => (x.Value, x.Count)));

        var response = new StatsOverviewResponse
        {
            GeneratedAtUtc = DateTime.UtcNow,
            Range = new StatsRange
            {
                FromUtc = rangeFromUtc,
                ToUtc = rangeToUtc,
                Granularity = "day"
            },
            Users = new UserStats
            {
                Total = totalUsers,
                Admins = userRoleMap[UserRole.Admin.ToString()],
                Contributors = userRoleMap[UserRole.Contributor.ToString()],
                RegularUsers = userRoleMap[UserRole.User.ToString()],
                Active = userStatusMap[UserStatus.Active.ToString()],
                Locked = userStatusMap[UserStatus.Locked.ToString()],
                PendingApproval = userStatusMap[UserStatus.PendingApproval.ToString()],
                ByRole = userRoleMap,
                ByStatus = userStatusMap
            },
            Places = new PlaceStats
            {
                Total = totalPlaces,
                Pending = placeModerationMap[ModerationStatus.Pending.ToString()],
                Approved = placeModerationMap[ModerationStatus.Approved.ToString()],
                Rejected = placeModerationMap[ModerationStatus.Rejected.ToString()],
                ByModerationStatus = placeModerationMap
            },
            Events = new EventStats
            {
                Total = totalEvents,
                Pending = eventModerationMap[ModerationStatus.Pending.ToString()],
                Approved = eventModerationMap[ModerationStatus.Approved.ToString()],
                Rejected = eventModerationMap[ModerationStatus.Rejected.ToString()],
                Upcoming = eventStatusMap[EventStatus.Upcoming.ToString()],
                Ongoing = eventStatusMap[EventStatus.Ongoing.ToString()],
                Ended = eventStatusMap[EventStatus.Ended.ToString()],
                ByModerationStatus = eventModerationMap,
                ByEventStatus = eventStatusMap
            },
            Reviews = new ReviewStats
            {
                Total = totalReviews,
                Active = reviewStatusMap[ReviewStatus.Active.ToString()],
                Hidden = reviewStatusMap[ReviewStatus.Hidden.ToString()],
                AverageRating = Math.Round(avgActiveRating, 2),
                ByStatus = reviewStatusMap
            },
            Moderation = new ModerationStats
            {
                PendingPlaces = placeModerationMap[ModerationStatus.Pending.ToString()],
                PendingEvents = eventModerationMap[ModerationStatus.Pending.ToString()],
                TotalPending = placeModerationMap[ModerationStatus.Pending.ToString()] + eventModerationMap[ModerationStatus.Pending.ToString()]
            },
            Chat = new ChatStats
            {
                TotalConversations = totalConversations,
                TotalMessages = totalMessages,
                NewConversationsInRange = newConversationsInRange,
                NewMessagesInRange = newMessagesInRange
            },
            Content = new ContentStats
            {
                Categories = totalCategories,
                AdministrativeUnits = totalAdministrativeUnits,
                MediaAssets = totalMediaAssets,
                TotalMediaBytes = totalMediaBytes
            },
            TimeSeries = new TimeSeriesStats
            {
                Users = usersSeries,
                Places = placesSeries,
                Events = eventsSeries,
                Reviews = reviewsSeries
            }
        };

        return Result.Ok(response);
    }

    private static Dictionary<string, int> BuildEnumMap<TEnum>(
        IEnumerable<TEnum> enumValues,
        IEnumerable<(TEnum Value, int Count)> rows) where TEnum : struct, Enum
    {
        var map = enumValues.ToDictionary(v => v.ToString(), _ => 0);
        foreach (var row in rows)
            map[row.Value.ToString()] = row.Count;
        return map;
    }

    private static Dictionary<string, int> BuildDynamicEventStatusMap(
        IEnumerable<Domain.Entities.Event> events,
        DateTime nowUtc)
    {
        var map = Enum.GetValues<EventStatus>().ToDictionary(v => v.ToString(), _ => 0);

        foreach (var evt in events)
        {
            var status = EventScheduleUtils.ResolveStatus(evt, nowUtc).ToString();
            map[status] = map.GetValueOrDefault(status, 0) + 1;
        }

        return map;
    }

    private static async Task<List<DailyCountPoint>> BuildDailySeriesAsync<TEntity>(
        IQueryable<TEntity> query,
        DateTime fromUtcInclusive,
        DateTime toUtcExclusive) where TEntity : BaseEntity
    {
        var rows = await query
            .Where(e => e.CreatedAt >= fromUtcInclusive && e.CreatedAt < toUtcExclusive)
            .GroupBy(e => e.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var countByDate = rows.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Count);
        var result = new List<DailyCountPoint>();
        var from = DateOnly.FromDateTime(fromUtcInclusive);
        var to = DateOnly.FromDateTime(toUtcExclusive.AddDays(-1));

        for (var day = from; day <= to; day = day.AddDays(1))
        {
            result.Add(new DailyCountPoint
            {
                Date = day,
                Count = countByDate.GetValueOrDefault(day, 0)
            });
        }

        return result;
    }

    private static (DateTime fromUtc, DateTime toUtc) ResolveRange(DateTime? fromUtc, DateTime? toUtc)
    {
        var resolvedTo = EnsureUtc(toUtc ?? DateTime.UtcNow);
        var resolvedFrom = EnsureUtc(fromUtc ?? resolvedTo.AddDays(-29));
        return (resolvedFrom.Date, resolvedTo.Date);
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
