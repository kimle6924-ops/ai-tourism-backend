using BE_AI_Tourism.Application.Mapping;
using BE_AI_Tourism.Application.Services.Admin;
using BE_AI_Tourism.Application.Services.Administrative;
using BE_AI_Tourism.Application.Services.Auth;
using BE_AI_Tourism.Application.Services.Category;
using BE_AI_Tourism.Application.Services.Chat;
using BE_AI_Tourism.Application.Services.Discovery;
using BE_AI_Tourism.Application.Services.Event;
using BE_AI_Tourism.Application.Services.Media;
using BE_AI_Tourism.Application.Services.Moderation;
using BE_AI_Tourism.Application.Services.Place;
using BE_AI_Tourism.Application.Services.Review;
using BE_AI_Tourism.Application.Services.Scope;
using BE_AI_Tourism.Application.Services.User;
using BE_AI_Tourism.Configuration;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Infrastructure.Authorization;
using BE_AI_Tourism.Infrastructure.Cloudinary;
using BE_AI_Tourism.Infrastructure.Gemini;
using BE_AI_Tourism.Infrastructure.Database;
using BE_AI_Tourism.Infrastructure.Database.Interfaces;
using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BE_AI_Tourism.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind configuration sections
        services.Configure<DatabaseOptions>(configuration.GetSection("Database"));
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<CloudinaryOptions>(configuration.GetSection("Cloudinary"));
        services.Configure<GeminiOptions>(configuration.GetSection("Gemini"));
        services.Configure<SecurityOptions>(configuration.GetSection("Security"));
        services.Configure<CorsOptions>(configuration.GetSection("Cors"));

        // PostgreSQL + EF Core
        // Ưu tiên đọc từ env var DATABASE_URL, fallback sang appsettings
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
                               ?? configuration.GetSection("Database:ConnectionString").Value
                               ?? string.Empty;
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IDatabaseContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));

        // Authorization handlers
        services.AddScoped<IAuthorizationHandler, ScopeAuthorizationHandler>();

        // Cloudinary provider
        services.AddScoped<ICloudinaryProvider, CloudinaryProvider>();

        // Gemini provider
        services.AddScoped<IGeminiProvider, GeminiProvider>();

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

        // Auth services
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();

        // User services
        services.AddScoped<IUserService, UserService>();

        // Admin services
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminStatsService, AdminStatsService>();

        // Administrative unit services
        services.AddScoped<IAdministrativeUnitService, AdministrativeUnitService>();

        // Category services
        services.AddScoped<ICategoryService, CategoryService>();

        // Scope service
        services.AddScoped<IScopeService, ScopeService>();

        // Place services
        services.AddScoped<IPlaceService, PlaceService>();

        // Event services
        services.AddScoped<IEventService, EventService>();

        // Moderation services
        services.AddScoped<IModerationService, ModerationService>();

        // Media services
        services.AddScoped<IMediaService, MediaService>();

        // Review services
        services.AddScoped<IReviewService, ReviewService>();

        // Discovery services
        services.AddScoped<IDiscoveryService, DiscoveryService>();

        // Chat services
        services.AddScoped<IChatService, ChatService>();

        return services;
    }
}
