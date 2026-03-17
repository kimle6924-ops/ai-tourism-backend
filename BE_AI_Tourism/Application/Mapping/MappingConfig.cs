using Mapster;

namespace BE_AI_Tourism.Application.Mapping;

// Register custom mapping rules here
public static class MappingConfig
{
    public static void Configure()
    {
        // Example: map Entity → ResponseDTO with custom rules
        // TypeAdapterConfig<User, UserResponse>.NewConfig()
        //     .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");

        // Default settings
        TypeAdapterConfig.GlobalSettings.Default
            .IgnoreNullValues(true);
    }
}
