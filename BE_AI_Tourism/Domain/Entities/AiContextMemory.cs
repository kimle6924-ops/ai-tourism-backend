using BE_AI_Tourism.Shared.Core;

namespace BE_AI_Tourism.Domain.Entities;

public class AiContextMemory : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ConversationId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<string> KeyFacts { get; set; } = [];
    public Dictionary<string, object> PreferenceSnapshot { get; set; } = [];
    public int Version { get; set; }
}
