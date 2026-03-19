namespace BE_AI_Tourism.Application.DTOs.Admin;

public class StatsOverviewResponse
{
    public DateTime GeneratedAtUtc { get; set; }
    public StatsRange Range { get; set; } = new();
    public UserStats Users { get; set; } = new();
    public PlaceStats Places { get; set; } = new();
    public EventStats Events { get; set; } = new();
    public ReviewStats Reviews { get; set; } = new();
    public ModerationStats Moderation { get; set; } = new();
    public ChatStats Chat { get; set; } = new();
    public ContentStats Content { get; set; } = new();
    public TimeSeriesStats TimeSeries { get; set; } = new();
}

public class StatsRange
{
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public string Granularity { get; set; } = "day";
}

public class UserStats
{
    public int Total { get; set; }
    public int Admins { get; set; }
    public int Contributors { get; set; }
    public int RegularUsers { get; set; }
    public int Active { get; set; }
    public int Locked { get; set; }
    public int PendingApproval { get; set; }
    public Dictionary<string, int> ByRole { get; set; } = new();
    public Dictionary<string, int> ByStatus { get; set; } = new();
}

public class PlaceStats
{
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public Dictionary<string, int> ByModerationStatus { get; set; } = new();
}

public class EventStats
{
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int Upcoming { get; set; }
    public int Ongoing { get; set; }
    public int Ended { get; set; }
    public Dictionary<string, int> ByModerationStatus { get; set; } = new();
    public Dictionary<string, int> ByEventStatus { get; set; } = new();
}

public class ReviewStats
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Hidden { get; set; }
    public int Deleted { get; set; }
    public double AverageRating { get; set; }
    public Dictionary<string, int> ByStatus { get; set; } = new();
}

public class ModerationStats
{
    public int PendingPlaces { get; set; }
    public int PendingEvents { get; set; }
    public int TotalPending { get; set; }
}

public class ChatStats
{
    public int TotalConversations { get; set; }
    public int TotalMessages { get; set; }
    public int NewConversationsInRange { get; set; }
    public int NewMessagesInRange { get; set; }
}

public class ContentStats
{
    public int Categories { get; set; }
    public int AdministrativeUnits { get; set; }
    public int MediaAssets { get; set; }
    public long TotalMediaBytes { get; set; }
}

public class TimeSeriesStats
{
    public List<DailyCountPoint> Users { get; set; } = [];
    public List<DailyCountPoint> Places { get; set; } = [];
    public List<DailyCountPoint> Events { get; set; } = [];
    public List<DailyCountPoint> Reviews { get; set; } = [];
}

public class DailyCountPoint
{
    public DateOnly Date { get; set; }
    public int Count { get; set; }
}
