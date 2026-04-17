using System.Linq.Expressions;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace BE_AI_Tourism.Infrastructure.Database;

public class EfRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<T> DbSet;

    public EfRepository(AppDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await DbSet.FindAsync(id);
    }

    public async Task<T?> FindOneAsync(Expression<Func<T, bool>> filter)
    {
        return await DbSet.FirstOrDefaultAsync(filter);
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> filter)
    {
        return await DbSet.Where(filter).ToListAsync();
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await DbSet.ToListAsync();
    }
    
    public IQueryable<T> AsQueryable()
    {
        return DbSet.AsQueryable();
    }

    public async Task<PaginationResponse<T>> GetPagedAsync(PaginationRequest request)
    {
        var totalCount = await DbSet.CountAsync();
        var items = await DbSet
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return PaginationResponse<T>.Create(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task AddAsync(T entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

        await DbSet.AddAsync(entity);
        await Context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        DbSet.Update(entity);
        await Context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await DbSet.FindAsync(id);
        if (entity != null)
        {
            DbSet.Remove(entity);
            await Context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await DbSet.AnyAsync(x => x.Id == id);
    }
}
