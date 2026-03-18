using BE_AI_Tourism.Domain.Enums;

namespace BE_AI_Tourism.Application.DTOs.Moderation;

public class ModerationLogResponse
{
    public Guid Id { get; set; }
    public ResourceType ResourceType { get; set; }
    public Guid ResourceId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public Guid ActedBy { get; set; }
    public DateTime ActedAt { get; set; }
}
