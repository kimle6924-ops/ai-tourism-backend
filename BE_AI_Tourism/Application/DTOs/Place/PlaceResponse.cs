using BE_AI_Tourism.Domain.Enums;

namespace BE_AI_Tourism.Application.DTOs.Place;

public class PlaceResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public Guid AdministrativeUnitId { get; set; }
    public List<Guid> CategoryIds { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public ModerationStatus ModerationStatus { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
