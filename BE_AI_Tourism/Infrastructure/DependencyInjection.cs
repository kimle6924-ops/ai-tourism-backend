using BE_AI_Tourism.Application.Mapping;
using BE_AI_Tourism.Configuration;
using FluentValidation;
using Mapster;
using MapsterMapper;

namespace BE_AI_Tourism.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind configuration sections
        services.Configure<DatabaseOptions>(configuration.GetSection("Database"));
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<ExternalServiceOptions>(configuration.GetSection("ExternalServices"));
        services.Configure<CorsOptions>(configuration.GetSection("Cors"));

        // Register database context here when implementing
        // services.AddScoped<IDatabaseContext, YourDbContext>();

        // Register repositories here when implementing
        // services.AddScoped(typeof(IRepository<>), typeof(YourRepository<>));

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // FluentValidation - auto-register all validators in this assembly
        services.AddValidatorsFromAssemblyContaining<Program>();

        // Mapster
        MappingConfig.Configure();
        services.AddSingleton(TypeAdapterConfig.GlobalSettings);
        services.AddScoped<IMapper, ServiceMapper>();

        // Register application services here
        // services.AddScoped<IYourService, YourService>();

        return services;
    }
}
