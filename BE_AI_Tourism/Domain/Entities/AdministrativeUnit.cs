using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Shared.Core;

namespace BE_AI_Tourism.Domain.Entities;

public class AdministrativeUnit : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public AdministrativeLevel Level { get; set; }
    public Guid? ParentId { get; set; }
    public string Code { get; set; } = string.Empty;
}
