using BE_AI_Tourism.Domain.Enums;

namespace BE_AI_Tourism.Application.DTOs.Event;

public class UpdateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public Guid AdministrativeUnitId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public List<Guid> CategoryIds { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public ScheduleType ScheduleType { get; set; } = ScheduleType.ExactDate;
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public int? StartMonth { get; set; }
    public int? StartDay { get; set; }
    public int? EndMonth { get; set; }
    public int? EndDay { get; set; }
}
