using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Shared.Core;

namespace BE_AI_Tourism.Domain.Entities;

public class ModerationLog : BaseEntity
{
    public ResourceType ResourceType { get; set; }
    public Guid ResourceId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public Guid ActedBy { get; set; }
    public DateTime ActedAt { get; set; }
}
