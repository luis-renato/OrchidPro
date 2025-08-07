using OrchidPro.Models.Base;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;

namespace OrchidPro.Services.Base;

/// <summary>
/// Base repository implementation providing comprehensive CRUD operations with intelligent caching, 
/// connectivity management, and performance optimization for all entity types.
/// Eliminates code duplication and ensures consistent data access patterns across the application.
/// </summary>
public abstract class BaseRepository<T> : IBaseRepository<T>
    where T : class, IBaseEntity, new()
{
    #region Protected Fields

    protected readonly SupabaseService _supabaseService;
    protected readonly List<T> _cache = new();
    protected DateTime? _lastCacheUpdate;
    protected readonly TimeSpan _cacheValidTime = TimeSpan.FromMinutes(5);
    protected readonly SemaphoreSlim _semaphore = new(1, 1);
    protected readonly object _cacheLock = new object();

    #endregion

    #region Abstract Methods - Must be implemented by derived classes

    /// <summary>
    /// Entity type name for logging and error messages
    /// </summary>
    protected abstract string EntityTypeName { get; }

    /// <summary>
    /// Get all entities from the specific service implementation
    /// </summary>
    protected abstract Task<IEnumerable<T>> GetAllFromServiceAsync();

    /// <summary>
    /// Get entity by ID from the specific service implementation
    /// </summary>
    protected abstract Task<T?> GetByIdFromServiceAsync(Guid id);

    /// <summary>
    /// Create entity in the specific service implementation
    /// </summary>
    protected abstract Task<T?> CreateInServiceAsync(T entity);

    /// <summary>
    /// Update entity in the specific service implementation
    /// </summary>
    protected abstract Task<T?> UpdateInServiceAsync(T entity);

    /// <summary>
    /// Delete entity in the specific service implementation
    /// </summary>
    protected abstract Task<bool> DeleteInServiceAsync(Guid id);

    /// <summary>
    /// Check if name exists in the specific service implementation
    /// </summary>
    protected abstract Task<bool> NameExistsInServiceAsync(string name, Guid? excludeId);

    #endregion

    #region Constructor

    /// <summary>
    /// Initialize base repository with Supabase service
    /// </summary>
    protected BaseRepository(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        this.LogInfo($"{EntityTypeName}Repository initialized with caching and performance optimization");
    }

    #endregion

    #region IBaseRepository Implementation

    /// <summary>
    /// Gets all entities with optional inactive inclusion and intelligent caching
    /// </summary>
    public virtual async Task<List<T>> GetAllAsync(bool includeInactive = false)
    {
        using (this.LogPerformance($"Get All {EntityTypeName}"))
        {
            await _semaphore.WaitAsync();
            try
            {
                var result = await this.SafeDataExecuteAsync(async () =>
                {
                    // Check if cache is valid
                    if (IsCacheValid())
                    {
                        this.LogInfo("Using cached data");
                        return GetFromCache(includeInactive);
                    }

                    // Check connectivity before trying server
                    var isConnected = await TestConnectionAsync();
                    if (!isConnected)
                    {
                        this.LogWarning("Offline - returning cached data");
                        return GetFromCache(includeInactive);
                    }

                    // Cache invalid and connected - fetch from server
                    this.LogInfo("Cache expired - fetching from server");
                    await RefreshCacheInternalAsync();

                    return GetFromCache(includeInactive);
                }, EntityTypeName);

                if (result.Success && result.Data != null)
                {
                    return result.Data;
                }
                else
                {
                    this.LogError($"GetAllAsync error: {result.Message}");
                    this.LogWarning("Using cache as fallback");
                    return GetFromCache(includeInactive);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    /// <summary>
    /// Gets filtered entities based on search criteria with intelligent caching
    /// </summary>
    public virtual async Task<List<T>> GetFilteredAsync(string? searchText = null, bool? statusFilter = null)
    {
        using (this.LogPerformance($"Get Filtered {EntityTypeName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var allEntities = await GetAllAsync(statusFilter != true);
                var filtered = allEntities.AsEnumerable();

                // Apply text search if provided
                if (!string.IsNullOrEmpty(searchText))
                {
                    filtered = ApplyTextSearch(filtered, searchText);
                }

                // Apply status filter if provided
                if (statusFilter.HasValue)
                {
                    filtered = filtered.Where(e => e.IsActive == statusFilter.Value);
                }

                return filtered.ToList();
            }, $"Filtered {EntityTypeName}");

            return result.Success && result.Data != null ? result.Data : new List<T>();
        }
    }

    /// <summary>
    /// Gets entity by ID with caching support
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        using (this.LogPerformance($"Get {EntityTypeName} by ID"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var allEntities = await GetAllAsync(true);
                return allEntities.FirstOrDefault(e => e.Id == id);
            }, $"{EntityTypeName} by ID");

            return result.Success ? result.Data : null;
        }
    }

    /// <summary>
    /// Creates new entity with validation and cache invalidation
    /// </summary>
    public virtual async Task<T> CreateAsync(T entity)
    {
        using (this.LogPerformance($"Create {EntityTypeName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                // Set creation metadata
                entity.Id = Guid.NewGuid();
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                this.LogDataOperation("Creating", EntityTypeName, entity.Id);

                // Create via service implementation
                var created = await CreateInServiceAsync(entity);
                if (created != null)
                {
                    InvalidateCache();
                    this.LogDataOperation("Created", EntityTypeName, $"{created.Name} successfully");
                    return created;
                }

                throw new InvalidOperationException($"Failed to create {EntityTypeName}");
            }, EntityTypeName);

            if (result.Success && result.Data != null)
            {
                return result.Data;
            }
            else
            {
                throw new InvalidOperationException(result.Message ?? $"Failed to create {EntityTypeName}");
            }
        }
    }

    /// <summary>
    /// Updates existing entity with validation and cache invalidation
    /// </summary>
    public virtual async Task<T> UpdateAsync(T entity)
    {
        using (this.LogPerformance($"Update {EntityTypeName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                // Set update metadata
                entity.UpdatedAt = DateTime.UtcNow;

                this.LogDataOperation("Updating", EntityTypeName, entity.Id);

                // Update via service implementation
                var updated = await UpdateInServiceAsync(entity);
                if (updated != null)
                {
                    InvalidateCache();
                    this.LogDataOperation("Updated", EntityTypeName, $"{updated.Name} successfully");
                    return updated;
                }

                throw new InvalidOperationException($"Failed to update {EntityTypeName}");
            }, EntityTypeName);

            if (result.Success && result.Data != null)
            {
                return result.Data;
            }
            else
            {
                throw new InvalidOperationException(result.Message ?? $"Failed to update {EntityTypeName}");
            }
        }
    }

    /// <summary>
    /// Deletes entity by ID with cascade validation and cache invalidation
    /// </summary>
    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        using (this.LogPerformance($"Delete {EntityTypeName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                this.LogDataOperation("Deleting", EntityTypeName, id);

                // Delete via service implementation
                var success = await DeleteInServiceAsync(id);
                if (success)
                {
                    InvalidateCache();
                    this.LogDataOperation("Deleted", EntityTypeName, "successfully");
                    return true;
                }

                throw new InvalidOperationException($"Failed to delete {EntityTypeName}");
            }, $"Delete {EntityTypeName}");

            return result.Success && result.Data;
        }
    }

    /// <summary>
    /// Gets entity by name with case-insensitive matching
    /// </summary>
    public virtual async Task<T?> GetByNameAsync(string name)
    {
        using (this.LogPerformance($"Get {EntityTypeName} by Name"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var allEntities = await GetAllAsync(true);
                return allEntities.FirstOrDefault(e =>
                    string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
            }, $"{EntityTypeName} by Name");

            return result.Success ? result.Data : null;
        }
    }

    /// <summary>
    /// Deletes multiple entities efficiently with batch operations
    /// </summary>
    public virtual async Task<int> DeleteMultipleAsync(IEnumerable<Guid> ids)
    {
        var idsList = ids.ToList();
        using (this.LogPerformance($"Delete Multiple {EntityTypeName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                this.LogDataOperation("Bulk deleting", EntityTypeName, $"{idsList.Count} items");

                var deletedCount = 0;
                foreach (var id in idsList)
                {
                    var success = await DeleteInServiceAsync(id);
                    if (success) deletedCount++;
                }

                if (deletedCount > 0)
                {
                    InvalidateCache();
                    this.LogDataOperation("Bulk deleted", EntityTypeName, $"{deletedCount} items successfully");
                }

                return deletedCount;
            }, $"Bulk Delete {EntityTypeName}");

            return result.Success ? result.Data : 0;
        }
    }

    /// <summary>
    /// Checks if entity name already exists with case-insensitive comparison
    /// </summary>
    public virtual async Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
    {
        using (this.LogPerformance($"Check Name Exists {EntityTypeName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var entities = await GetAllAsync(true);
                var exists = entities.Any(e =>
                    string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase) &&
                    e.Id != excludeId);

                this.LogInfo($"Name '{name}' exists: {exists}");
                return exists;
            }, "Name Check");

            return result.Success && result.Data;
        }
    }

    /// <summary>
    /// Toggle favorite status for an entity
    /// </summary>
    public virtual async Task<T> ToggleFavoriteAsync(Guid entityId)
    {
        using (this.LogPerformance($"Toggle Favorite {EntityTypeName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                this.LogDataOperation("Toggling favorite", EntityTypeName, entityId);

                // Find current entity
                var entity = await GetByIdAsync(entityId);
                if (entity == null)
                {
                    throw new ArgumentException($"{EntityTypeName} with ID {entityId} not found");
                }

                // Toggle favorite using dynamic method call
                var originalFavoriteStatus = entity.IsFavorite;

                // Use reflection to call ToggleFavorite if available, otherwise manual toggle
                var toggleMethod = entity.GetType().GetMethod("ToggleFavorite");
                if (toggleMethod != null)
                {
                    toggleMethod.Invoke(entity, null);
                }
                else
                {
                    entity.IsFavorite = !entity.IsFavorite;
                    entity.UpdatedAt = DateTime.UtcNow;
                }

                this.LogDataOperation("Toggled favorite", EntityTypeName, $"'{entity.Name}' {originalFavoriteStatus} → {entity.IsFavorite}");

                // Save to database
                var updatedEntity = await UpdateAsync(entity);
                if (updatedEntity == null)
                {
                    throw new InvalidOperationException($"Failed to update {EntityTypeName} favorite status");
                }

                this.LogDataOperation("Favorite toggled", EntityTypeName, $"{entity.Name} successfully");
                return updatedEntity;
            }, EntityTypeName);

            if (result.Success && result.Data != null)
            {
                return result.Data;
            }
            else
            {
                throw new InvalidOperationException(result.Message);
            }
        }
    }

    /// <summary>
    /// Refresh cache data from server
    /// </summary>
    public virtual async Task RefreshCacheAsync()
    {
        using (this.LogPerformance($"Refresh Cache {EntityTypeName}"))
        {
            await _semaphore.WaitAsync();
            try
            {
                await RefreshCacheInternalAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    /// <summary>
    /// Get comprehensive statistics for this entity type
    /// </summary>
    public virtual async Task<BaseStatistics> GetStatisticsAsync()
    {
        using (this.LogPerformance($"Get {EntityTypeName} Statistics"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var entities = await GetAllAsync(true);

                return new BaseStatistics
                {
                    TotalCount = entities.Count,
                    ActiveCount = entities.Count(e => e.IsActive),
                    InactiveCount = entities.Count(e => !e.IsActive),
                    SystemDefaultCount = entities.Count(e => e.IsSystemDefault),
                    UserCreatedCount = entities.Count(e => !e.IsSystemDefault),
                    LastRefreshTime = _lastCacheUpdate ?? DateTime.UtcNow
                };
            }, $"{EntityTypeName} Statistics");

            return result.Success && result.Data != null ? result.Data : new BaseStatistics();
        }
    }

    /// <summary>
    /// Test connectivity to data source
    /// </summary>
    public virtual async Task<bool> TestConnectionAsync()
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            // Simple connectivity test - try to get minimal data from service
            try
            {
                var entities = await GetAllFromServiceAsync();
                return entities != null;
            }
            catch
            {
                return false;
            }
        }, "Connection Test");

        return result.Success && result.Data;
    }

    /// <summary>
    /// Get cache information for diagnostics
    /// </summary>
    public virtual string GetCacheInfo()
    {
        lock (_cacheLock)
        {
            var cacheAge = _lastCacheUpdate.HasValue
                ? DateTime.UtcNow - _lastCacheUpdate.Value
                : TimeSpan.Zero;

            return $"{EntityTypeName} cache: {_cache.Count} entries, age: {cacheAge.TotalMinutes:F1}min, valid: {IsCacheValid()}";
        }
    }

    /// <summary>
    /// Invalidate cache externally
    /// </summary>
    public virtual void InvalidateCacheExternal()
    {
        InvalidateCache();
    }

    #endregion

    #region Protected Virtual Methods - Can be overridden by derived classes

    /// <summary>
    /// Apply text search filtering - can be customized by derived classes
    /// </summary>
    protected virtual IEnumerable<T> ApplyTextSearch(IEnumerable<T> entities, string searchText)
    {
        var searchLower = searchText.ToLower();
        return entities.Where(e =>
            e.Name.ToLower().Contains(searchLower) ||
            (!string.IsNullOrEmpty(e.Description) && e.Description.ToLower().Contains(searchLower)));
    }

    /// <summary>
    /// Get additional cache key factors - can be overridden for more complex caching
    /// </summary>
    protected virtual string GetCacheKey()
    {
        return $"{EntityTypeName}_cache";
    }

    #endregion

    #region Private Cache Management

    /// <summary>
    /// Check if current cache is still valid
    /// </summary>
    protected bool IsCacheValid()
    {
        return _lastCacheUpdate.HasValue &&
               DateTime.UtcNow - _lastCacheUpdate.Value < _cacheValidTime &&
               _cache.Any();
    }

    /// <summary>
    /// Get data from cache with filtering
    /// </summary>
    protected List<T> GetFromCache(bool includeInactive)
    {
        lock (_cacheLock)
        {
            return includeInactive
                ? _cache.ToList()
                : _cache.Where(e => e.IsActive).ToList();
        }
    }

    /// <summary>
    /// Refresh cache from server internally
    /// </summary>
    protected async Task RefreshCacheInternalAsync()
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            var entities = await GetAllFromServiceAsync();
            return entities.ToList();
        }, $"Refresh {EntityTypeName} Cache");

        if (result.Success && result.Data != null)
        {
            lock (_cacheLock)
            {
                _cache.Clear();
                _cache.AddRange(result.Data);
                _lastCacheUpdate = DateTime.UtcNow;
                this.LogInfo($"Cache refreshed: {_cache.Count} {EntityTypeName.ToLower()} entities");
            }
        }
        else
        {
            this.LogError($"Failed to refresh cache: {result.Message}");
        }
    }

    /// <summary>
    /// Invalidate current cache
    /// </summary>
    protected void InvalidateCache()
    {
        lock (_cacheLock)
        {
            _lastCacheUpdate = null;
            this.LogInfo($"{EntityTypeName} cache invalidated");
        }
    }

    #endregion
}