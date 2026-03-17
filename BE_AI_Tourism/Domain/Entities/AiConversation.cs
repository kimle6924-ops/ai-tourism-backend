using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Shared.Core;

namespace BE_AI_Tourism.Domain.Entities;

public class AiConversation : BaseEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public ConversationStatus Status { get; set; } = ConversationStatus.Active;
    public DateTime LastMessageAt { get; set; }
}
