using IT_Project2526.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IT_Project2526.Repositories
{
    /// <summary>
    /// Generic repository implementation for CRUD operations
    /// </summary>
    public class Repository<T> : IRepository<T> where T : BaseModel
    {
        protected readonly ITProjectDB _context;
        protected readonly DbSet<T> _dbSet;
        protected readonly ILogger<Repository<T>> _logger;

        public Repository(ITProjectDB context, ILogger<Repository<T>> logger)
        {
            _context = context;
            _dbSet = context.Set<T>();
            _logger = logger;
        }

        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            return await _dbSet.FirstOrDefaultAsync(e => e.Guid == id && e.ValidUntil == null);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> GetActiveAsync()
        {
            return await _dbSet.Where(e => e.ValidUntil == null).ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            return entity;
        }

        public virtual async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }

        public virtual Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            return Task.CompletedTask;
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                entity.ValidUntil = DateTime.UtcNow;
                await UpdateAsync(entity);
                _logger.LogInformation("Soft deleted entity {EntityType} with ID {Id}", typeof(T).Name, id);
            }
        }

        public virtual Task RemoveAsync(T entity)
        {
            _dbSet.Remove(entity);
            _logger.LogWarning("Hard deleted entity {EntityType} with ID {Id}", typeof(T).Name, entity.Guid);
            return Task.CompletedTask;
        }

        public virtual async Task<bool> ExistsAsync(Guid id)
        {
            return await _dbSet.AnyAsync(e => e.Guid == id && e.ValidUntil == null);
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            if (predicate == null)
                return await _dbSet.CountAsync(e => e.ValidUntil == null);
            
            return await _dbSet.Where(predicate).CountAsync();
        }

        public virtual async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
