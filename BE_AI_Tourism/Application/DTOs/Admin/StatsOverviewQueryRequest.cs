namespace BE_AI_Tourism.Application.DTOs.Admin;

public class StatsOverviewQueryRequest
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
