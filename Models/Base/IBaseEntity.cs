using System.ComponentModel.DataAnnotations;

namespace OrchidPro.Models.Base;

/// <summary>
/// Base interface for all domain entities with standard properties and validation
/// </summary>
public interface IBaseEntity
{
    /// <summary>
    /// Unique identifier for the entity
    /// </summary>
    Guid Id { get; set; }

    /// <summary>
    /// Owner user ID (null for system data)
    /// </summary>
    Guid? UserId { get; set; }

    /// <summary>
    /// Primary name/title of the entity
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Optional description of the entity
    /// </summary>
    string? Description { get; set; }

    /// <summary>
    /// Indicates if the entity is active
    /// </summary>
    bool IsActive { get; set; }

    /// <summary>
    /// Indicates if the entity is marked as favorite by the user
    /// </summary>
    bool IsFavorite { get; set; }

    /// <summary>
    /// Computed property indicating if entity is system default
    /// Based on UserId == null instead of database field
    /// </summary>
    bool IsSystemDefault { get; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Display name for UI presentation
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Status display for UI presentation
    /// </summary>
    string StatusDisplay { get; }

    /// <summary>
    /// Validates the entity and returns list of errors
    /// </summary>
    /// <param name="errors">List of validation errors found</param>
    /// <returns>True if valid, false if validation errors exist</returns>
    bool IsValid(out List<string> errors);

    /// <summary>
    /// Creates a copy of the entity for editing purposes
    /// </summary>
    /// <returns>Cloned entity instance</returns>
    IBaseEntity Clone();
}

/// <summary>
/// Generic interface for repository pattern implementation
/// </summary>
public interface IBaseRepository<T> where T : class, IBaseEntity
{
    #region Read Operations

    /// <summary>
    /// Gets all entities with optional inactive inclusion
    /// </summary>
    Task<List<T>> GetAllAsync(bool includeInactive = false);

    /// <summary>
    /// Gets filtered entities based on search criteria
    /// </summary>
    Task<List<T>> GetFilteredAsync(string? searchText = null, bool? statusFilter = null);

    /// <summary>
    /// Gets entity by unique identifier
    /// </summary>
    Task<T?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets entity by name
    /// </summary>
    Task<T?> GetByNameAsync(string name);

    #endregion

    #region Write Operations

    /// <summary>
    /// Creates new entity
    /// </summary>
    Task<T> CreateAsync(T entity);

    /// <summary>
    /// Updates existing entity
    /// </summary>
    Task<T> UpdateAsync(T entity);

    /// <summary>
    /// Deletes entity by identifier
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Deletes multiple entities by identifiers
    /// </summary>
    Task<int> DeleteMultipleAsync(IEnumerable<Guid> ids);

    #endregion

    #region Validation and Utilities

    /// <summary>
    /// Checks if name already exists, optionally excluding specific entity
    /// </summary>
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null);

    /// <summary>
    /// Gets comprehensive statistics for the entity type
    /// </summary>
    Task<BaseStatistics> GetStatisticsAsync();

    /// <summary>
    /// Refreshes cached data
    /// </summary>
    Task RefreshCacheAsync();

    /// <summary>
    /// Tests repository connection
    /// </summary>
    Task<bool> TestConnectionAsync();

    /// <summary>
    /// Gets cache information for debugging
    /// </summary>
    string GetCacheInfo();

    /// <summary>
    /// Invalidates external cache
    /// </summary>
    void InvalidateCacheExternal();

    #endregion
}

/// <summary>
/// Base statistics for any entity type
/// </summary>
public class BaseStatistics
{
    /// <summary>
    /// Total number of entities
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Number of active entities
    /// </summary>
    public int ActiveCount { get; set; }

    /// <summary>
    /// Number of inactive entities
    /// </summary>
    public int InactiveCount { get; set; }

    /// <summary>
    /// Number of system default entities
    /// </summary>
    public int SystemDefaultCount { get; set; }

    /// <summary>
    /// Number of user-created entities
    /// </summary>
    public int UserCreatedCount { get; set; }

    /// <summary>
    /// Last data refresh timestamp
    /// </summary>
    public DateTime LastRefreshTime { get; set; }

    /// <summary>
    /// Percentage of active entities
    /// </summary>
    public double ActivePercentage => TotalCount > 0 ? (double)ActiveCount / TotalCount * 100 : 0;

    /// <summary>
    /// Percentage of user-created entities
    /// </summary>
    public double UserCreatedPercentage => TotalCount > 0 ? (double)UserCreatedCount / TotalCount * 100 : 0;
}