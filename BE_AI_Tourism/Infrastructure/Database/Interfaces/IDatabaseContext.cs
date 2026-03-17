namespace BE_AI_Tourism.Infrastructure.Database.Interfaces;

public interface IDatabaseContext : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
