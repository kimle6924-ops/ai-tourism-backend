using BE_AI_Tourism.Domain.Enums;

namespace BE_AI_Tourism.Application.DTOs.Administrative;

public class CreateAdministrativeUnitRequest
{
    public string Name { get; set; } = string.Empty;
    public AdministrativeLevel Level { get; set; }
    public Guid? ParentId { get; set; }
    public string Code { get; set; } = string.Empty;
}
