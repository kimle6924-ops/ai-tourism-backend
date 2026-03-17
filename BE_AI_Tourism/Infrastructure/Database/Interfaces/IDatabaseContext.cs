namespace BE_AI_Tourism.Infrastructure.Database.Interfaces;

// Database abstraction - implement per database provider (EF Core, Dapper, MongoDB, etc.)
public interface IDatabaseContext : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
