using Mapster;

namespace BE_AI_Tourism.Application.Mapping;

public static class MappingConfig
{
    public static void Configure()
    {
        // User → UserResponse: Mapster auto-maps matching properties.
        // Password, RefreshToken, RefreshTokenExpiryTime are excluded
        // because UserResponse doesn't have those fields.

        // Default settings
        TypeAdapterConfig.GlobalSettings.Default
            .IgnoreNullValues(true);
    }
}
