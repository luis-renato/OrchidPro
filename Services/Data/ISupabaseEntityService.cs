using OrchidPro.Models.Base;

namespace OrchidPro.Services.Data;

/// <summary>
/// Generic interface for Supabase entity services providing database operations.
/// Standardizes database access patterns across all entity types and eliminates service code duplication.
/// </summary>
/// <typeparam name="T">Entity type that implements IBaseEntity</typeparam>
public interface ISupabaseEntityService<T> where T : class, IBaseEntity
{
    /// <summary>
    /// Gets all entities from the database
    /// </summary>
    /// <returns>Collection of all entities</returns>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Gets entity by identifier
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <returns>Entity if found, null otherwise</returns>
    Task<T?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates new entity in the database
    /// </summary>
    /// <param name="entity">Entity to create</param>
    /// <returns>Created entity with server-generated values</returns>
    Task<T?> CreateAsync(T entity);

    /// <summary>
    /// Updates existing entity in the database
    /// </summary>
    /// <param name="entity">Entity to update</param>
    /// <returns>Updated entity with server values</returns>
    Task<T?> UpdateAsync(T entity);

    /// <summary>
    /// Deletes entity by identifier
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <returns>True if deletion was successful</returns>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Checks if entity name already exists
    /// </summary>
    /// <param name="name">Name to check</param>
    /// <param name="excludeId">Optional ID to exclude from check</param>
    /// <returns>True if name exists</returns>
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null);
}