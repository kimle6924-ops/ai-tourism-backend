namespace BE_AI_Tourism.Application.DTOs.Place;

public class CreatePlaceRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public Guid AdministrativeUnitId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public List<Guid> CategoryIds { get; set; } = [];
    public List<string> Tags { get; set; } = [];
}
