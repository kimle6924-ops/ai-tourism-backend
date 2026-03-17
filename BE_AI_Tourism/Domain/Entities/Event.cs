using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Shared.Core;

namespace BE_AI_Tourism.Domain.Entities;

public class Event : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public Guid AdministrativeUnitId { get; set; }
    public List<Guid> CategoryIds { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public EventStatus EventStatus { get; set; } = EventStatus.Upcoming;
    public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.Pending;
    public Guid CreatedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
