using BE_AI_Tourism.Shared.Core;

namespace BE_AI_Tourism.Domain.Entities;

public class CommunityPost : BaseEntity
{
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
}
