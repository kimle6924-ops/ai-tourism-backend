using System.Linq.Expressions;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;

namespace BE_AI_Tourism.Domain.Interfaces;

// Generic repository interface - implement per database provider
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<T?> FindOneAsync(Expression<Func<T, bool>> filter);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> filter);
    Task<IEnumerable<T>> GetAllAsync();
    IQueryable<T> AsQueryable();
    Task<PaginationResponse<T>> GetPagedAsync(PaginationRequest request);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
