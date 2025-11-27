using IT_Project2526.Models;
using System.Linq.Expressions;

namespace IT_Project2526.Repositories
{
    /// <summary>
    /// Generic repository interface for CRUD operations on entities that inherit from BaseModel
    /// </summary>
    /// <typeparam name="T">Entity type that inherits from BaseModel</typeparam>
    public interface IRepository<T> where T : BaseModel
    {
        /// <summary>
        /// Gets an entity by its unique identifier
        /// </summary>
        Task<T?> GetByIdAsync(Guid id);
        
        /// <summary>
        /// Gets all entities (including soft-deleted ones)
        /// </summary>
        Task<IEnumerable<T>> GetAllAsync();
        
        /// <summary>
        /// Gets only active entities (ValidUntil is null)
        /// </summary>
        Task<IEnumerable<T>> GetActiveAsync();
        
        /// <summary>
        /// Finds entities matching the given predicate
        /// </summary>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        
        /// <summary>
        /// Gets a single entity matching the predicate
        /// </summary>
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        
        /// <summary>
        /// Adds a new entity
        /// </summary>
        Task<T> AddAsync(T entity);
        
        /// <summary>
        /// Adds multiple entities
        /// </summary>
        Task AddRangeAsync(IEnumerable<T> entities);
        
        /// <summary>
        /// Updates an existing entity
        /// </summary>
        Task UpdateAsync(T entity);
        
        /// <summary>
        /// Soft deletes an entity by setting ValidUntil
        /// </summary>
        Task DeleteAsync(Guid id);
        
        /// <summary>
        /// Hard deletes an entity from the database
        /// </summary>
        Task RemoveAsync(T entity);
        
        /// <summary>
        /// Checks if an entity exists
        /// </summary>
        Task<bool> ExistsAsync(Guid id);
        
        /// <summary>
        /// Counts entities matching the predicate
        /// </summary>
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
        
        /// <summary>
        /// Saves all pending changes to the database
        /// </summary>
        Task<int> SaveChangesAsync();
    }
}
