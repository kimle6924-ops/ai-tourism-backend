using BE_AI_Tourism.Shared.Core;

namespace BE_AI_Tourism.Domain.Entities;

public class UserPreference : BaseEntity
{
    public Guid UserId { get; set; }
    public List<Guid> CategoryIds { get; set; } = [];
}
