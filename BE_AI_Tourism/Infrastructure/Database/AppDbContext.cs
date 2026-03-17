using BE_AI_Tourism.Domain.Entities;
using BE_AI_Tourism.Infrastructure.Database.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BE_AI_Tourism.Infrastructure.Database;

public class AppDbContext : DbContext, IDatabaseContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<AdministrativeUnit> AdministrativeUnits => Set<AdministrativeUnit>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
    public DbSet<Place> Places => Set<Place>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ModerationLog> ModerationLogs => Set<ModerationLog>();
    public DbSet<AiConversation> AiConversations => Set<AiConversation>();
    public DbSet<AiMessage> AiMessages => Set<AiMessage>();
    public DbSet<AiContextMemory> AiContextMemories => Set<AiContextMemory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Use snake_case table names
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(ToSnakeCase(entity.GetTableName()!));
        }

        ConfigureUser(modelBuilder);
        ConfigureAdministrativeUnit(modelBuilder);
        ConfigureCategory(modelBuilder);
        ConfigureUserPreference(modelBuilder);
        ConfigurePlace(modelBuilder);
        ConfigureEvent(modelBuilder);
        ConfigureMediaAsset(modelBuilder);
        ConfigureReview(modelBuilder);
        ConfigureModerationLog(modelBuilder);
        ConfigureAiConversation(modelBuilder);
        ConfigureAiMessage(modelBuilder);
        ConfigureAiContextMemory(modelBuilder);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Role);
            entity.HasIndex(e => e.AdministrativeUnitId);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.Role).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
        });
    }

    private static void ConfigureAdministrativeUnit(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdministrativeUnit>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Level);
            entity.HasIndex(e => e.ParentId);

            entity.Property(e => e.Level).HasConversion<string>();
        });
    }

    private static void ConfigureCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IsActive);
        });
    }

    private static void ConfigureUserPreference(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();
        });
    }

    private static void ConfigurePlace(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Place>(entity =>
        {
            entity.HasIndex(e => e.AdministrativeUnitId);
            entity.HasIndex(e => e.ModerationStatus);

            entity.Property(e => e.ModerationStatus).HasConversion<string>();
        });
    }

    private static void ConfigureEvent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasIndex(e => e.AdministrativeUnitId);
            entity.HasIndex(e => e.StartAt);
            entity.HasIndex(e => e.EndAt);
            entity.HasIndex(e => e.EventStatus);
            entity.HasIndex(e => e.ModerationStatus);

            entity.Property(e => e.EventStatus).HasConversion<string>();
            entity.Property(e => e.ModerationStatus).HasConversion<string>();
        });
    }

    private static void ConfigureMediaAsset(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MediaAsset>(entity =>
        {
            entity.HasIndex(e => new { e.ResourceType, e.ResourceId });
            entity.HasIndex(e => new { e.ResourceId, e.IsPrimary });
            entity.HasIndex(e => new { e.ResourceType, e.ResourceId, e.SortOrder });

            entity.Property(e => e.ResourceType).HasConversion<string>();
        });
    }

    private static void ConfigureReview(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasIndex(e => new { e.ResourceType, e.ResourceId });
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.ResourceType).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
        });
    }

    private static void ConfigureModerationLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ModerationLog>(entity =>
        {
            entity.HasIndex(e => new { e.ResourceType, e.ResourceId });
            entity.HasIndex(e => e.ActedBy);
            entity.HasIndex(e => e.ActedAt);

            entity.Property(e => e.ResourceType).HasConversion<string>();
        });
    }

    private static void ConfigureAiConversation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiConversation>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.LastMessageAt });
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.Status).HasConversion<string>();
        });
    }

    private static void ConfigureAiMessage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiMessage>(entity =>
        {
            entity.HasIndex(e => new { e.ConversationId, e.CreatedAt });
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });

            entity.Property(e => e.Role).HasConversion<string>();
        });
    }

    private static void ConfigureAiContextMemory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiContextMemory>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.UpdatedAt });
            entity.HasIndex(e => new { e.ConversationId, e.UpdatedAt });

            entity.Property(e => e.PreferenceSnapshot).HasColumnType("jsonb");
        });
    }

    private static string ToSnakeCase(string name)
    {
        return string.Concat(name.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + c : c.ToString()
        )).ToLowerInvariant();
    }
}
