using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Shared.Core;

namespace BE_AI_Tourism.Domain.Entities;

public class AiMessage : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public int TokenCount { get; set; }
    public List<string> Citations { get; set; } = [];
}
