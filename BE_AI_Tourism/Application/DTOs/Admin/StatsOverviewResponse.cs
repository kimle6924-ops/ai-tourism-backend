namespace BE_AI_Tourism.Application.DTOs.Admin;

public class StatsOverviewResponse
{
    public UserStats Users { get; set; } = new();
    public PlaceStats Places { get; set; } = new();
    public EventStats Events { get; set; } = new();
    public ReviewStats Reviews { get; set; } = new();
    public ChatStats Chat { get; set; } = new();
    public ContentStats Content { get; set; } = new();
}

public class UserStats
{
    public int Total { get; set; }
    public int Admins { get; set; }
    public int Contributors { get; set; }
    public int RegularUsers { get; set; }
    public int Active { get; set; }
    public int Locked { get; set; }
}

public class PlaceStats
{
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
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
}

public class ReviewStats
{
    public int Total { get; set; }
    public double AverageRating { get; set; }
}

public class ChatStats
{
    public int TotalConversations { get; set; }
    public int TotalMessages { get; set; }
}

public class ContentStats
{
    public int Categories { get; set; }
    public int AdministrativeUnits { get; set; }
    public int MediaAssets { get; set; }
}
