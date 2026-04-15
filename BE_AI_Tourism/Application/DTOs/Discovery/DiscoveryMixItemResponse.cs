using BE_AI_Tourism.Domain.Enums;

namespace BE_AI_Tourism.Application.DTOs.Discovery;

public class DiscoveryMixItemResponse
{
    public ResourceType ResourceType { get; set; }
    public Guid ResourceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public Guid AdministrativeUnitId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double AverageRating { get; set; }
    public double DistanceKm { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool PreferenceMatched { get; set; }
    public double PreferenceMatchScore { get; set; }
    public double DistanceScore { get; set; }
    public double RatingScore { get; set; }
    public double TotalScore { get; set; }
}
