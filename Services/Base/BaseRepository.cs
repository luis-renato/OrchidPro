using OrchidPro.Models.Base;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;
using System.Collections.Concurrent;

namespace OrchidPro.Services.Base;

/// <summary>
/// Performance optimized base repository with smart caching and background refresh.
/// Fixed version with proper syntax and all interface implementations.
/// </summary>
public abstract class BaseRepository<T> : IBaseRepository<T>
    where T : class, IBaseEntity, new()
{
    #region Protected Fields

    protected readonly SupabaseService _supabaseService;
    protected readonly ConcurrentDictionary<Guid, T> _entityCache = new();
    protected DateTime? _lastCacheUpdate;
    protected readonly TimeSpan _cacheValidTime = TimeSpan.FromMinutes(8);
    protected readonly SemaphoreSlim _semaphore = new(1, 1);

    #endregion

    #region Performance Smart Background Refresh

    /// <summary>
    /// Background refresh timer for proactive cache updates
    /// </summary>
    private Timer? _backgroundRefreshTimer;
    private readonly TimeSpan _backgroundRefreshInterval = TimeSpan.FromMinutes(4);

    /// <summary>
    /// Track cache access patterns for intelligent pre-loading
    /// </summary>
    private readonly ConcurrentDictionary<string, DateTime> _accessPatterns = new();

    #endregion

    #region Abstract Methods

    protected abstract string EntityTypeName { get; }
    protected abstract Task<IEnumerable<T>> GetAllFromServiceAsync();
    protected abstract Task<T?> GetByIdFromServiceAsync(Guid id);
    protected abstract Task<T?> CreateInServiceAsync(T entity);
    protected abstract Task<T?> UpdateInServiceAsync(T entity);
    protected abstract Task<bool> DeleteInServiceAsync(Guid id);
    protected abstract Task<bool> NameExistsInServiceAsync(string name, Guid? excludeId);

    #endregion

    #region Constructor

    protected BaseRepository(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));

        // Start background refresh timer
        InitializeBackgroundRefresh();

        this.LogInfo($"OPTIMIZED {EntityTypeName}Repository initialized with smart caching");
    }

    #endregion

    #region Performance Optimized Core Methods

    /// <summary>
    /// Optimized GetAll with smart cache management and background refresh
    /// </summary>
    public virtual async Task<List<T>> GetAllAsync(bool includeInactive = false)
    {
        using (this.LogPerformance($"Get All {EntityTypeName}"))
        {
            TrackAccess("GetAll");

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

                    // Start background refresh if cache is stale but not empty
                    if (!_entityCache.IsEmpty && IsCacheStale())
                    {
                        _ = Task.Run(RefreshCacheInternalAsync);
                        this.LogInfo("Using stale cache while refreshing in background");
                        return GetFromCache(includeInactive);
                    }

                    // Cache invalid or empty - fetch from server
                    var isConnected = await TestConnectionAsync();
                    if (!isConnected)
                    {
                        this.LogWarning("Offline - returning cached data");
                        return GetFromCache(includeInactive);
                    }

                    this.LogInfo("Cache invalid - fetching from server");
                    await RefreshCacheInternalAsync();

                    return GetFromCache(includeInactive);
                }, EntityTypeName);

                if (result.Success && result.Data != null)
                {
                    return result.Data;
                }
                else
                {
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
    /// Optimized GetById with cache-first approach
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        using (this.LogPerformance($"Get {EntityTypeName} by ID"))
        {
            TrackAccess($"GetById-{id}");

            // Try cache first
            if (_entityCache.TryGetValue(id, out var cachedEntity))
            {
                this.LogInfo("Found in cache");
                return cachedEntity;
            }

            // Not in cache - get all (which populates cache) and try again
            await GetAllAsync(true);

            return _entityCache.TryGetValue(id, out var entity) ? entity : null;
        }
    }

    /// <summary>
    /// Optimized Create with immediate cache update
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
                    // Update cache immediately
                    _entityCache.TryAdd(created.Id, created);
                    this.LogDataOperation("Created and cached", EntityTypeName, $"{created.Name} successfully");
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
    /// Optimized Update with immediate cache sync
    /// </summary>
    public virtual async Task<T> UpdateAsync(T entity)
    {
        using (this.LogPerformance($"Update {EntityTypeName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                entity.UpdatedAt = DateTime.UtcNow;

                this.LogDataOperation("Updating", EntityTypeName, entity.Id);

                var updated = await UpdateInServiceAsync(entity);
                if (updated != null)
                {
                    // Update cache immediately
                    _entityCache.AddOrUpdate(updated.Id, updated, (key, oldValue) => updated);
                    this.LogDataOperation("Updated and cached", EntityTypeName, $"{updated.Name} successfully");
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
    /// Optimized Delete with immediate cache removal
    /// </summary>
    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        using (this.LogPerformance($"Delete {EntityTypeName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                this.LogDataOperation("Deleting", EntityTypeName, id);

                var success = await DeleteInServiceAsync(id);
                if (success)
                {
                    // Remove from cache immediately
                    _entityCache.TryRemove(id, out _);
                    this.LogDataOperation("Deleted and removed from cache", EntityTypeName, "successfully");
                    return true;
                }

                throw new InvalidOperationException($"Failed to delete {EntityTypeName}");
            }, $"Delete {EntityTypeName}");

            return result.Success && result.Data;
        }
    }

    #endregion

    #region Performance Optimized Cache Management

    /// <summary>
    /// Check if cache is valid (not expired)
    /// </summary>
    private bool IsCacheValid()
    {
        return _lastCacheUpdate.HasValue &&
               DateTime.UtcNow - _lastCacheUpdate.Value < _cacheValidTime &&
               !_entityCache.IsEmpty;
    }

    /// <summary>
    /// Check if cache is stale but still usable
    /// </summary>
    private bool IsCacheStale()
    {
        return _lastCacheUpdate.HasValue &&
               DateTime.UtcNow - _lastCacheUpdate.Value > TimeSpan.FromMinutes(6) &&
               !_entityCache.IsEmpty;
    }

    /// <summary>
    /// Get entities from cache with filtering
    /// </summary>
    private List<T> GetFromCache(bool includeInactive)
    {
        var entities = _entityCache.Values.ToList();
        return includeInactive
            ? entities
            : [.. entities.Where(e => e.IsActive)];
    }

    /// <summary>
    /// Optimized refresh cache with parallel processing
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
            // Update cache in parallel batches
            await Task.Run(() =>
            {
                _entityCache.Clear();

                // Add entities in parallel for better performance
                var entities = result.Data;
                Parallel.ForEach(entities, entity =>
                {
                    _entityCache.TryAdd(entity.Id, entity);
                });

                _lastCacheUpdate = DateTime.UtcNow;
            });

            this.LogInfo($"Cache refreshed: {result.Data.Count} {EntityTypeName.ToLower()} entities");
        }
        else
        {
            this.LogError($"Failed to refresh cache: {result.Message}");
        }
    }

    /// <summary>
    /// Initialize background refresh system
    /// </summary>
    private void InitializeBackgroundRefresh()
    {
        _backgroundRefreshTimer = new Timer(async _ =>
        {
            try
            {
                // Only refresh if cache is getting stale and there's recent access
                if (ShouldBackgroundRefresh())
                {
                    this.LogInfo($"Background refresh triggered for {EntityTypeName}");
                    await RefreshCacheInternalAsync();
                }
            }
            catch (Exception ex)
            {
                this.LogError(ex, $"Background refresh failed for {EntityTypeName}");
            }
        }, null, _backgroundRefreshInterval, _backgroundRefreshInterval);
    }

    /// <summary>
    /// Determine if background refresh should occur
    /// </summary>
    private bool ShouldBackgroundRefresh()
    {
        // Don't refresh if cache is fresh
        if (IsCacheValid()) return false;

        // Don't refresh if no recent access
        var recentAccess = _accessPatterns.Values.Any(time =>
            DateTime.UtcNow - time < TimeSpan.FromMinutes(10));

        return recentAccess;
    }

    /// <summary>
    /// Track access patterns for intelligent caching
    /// </summary>
    private void TrackAccess(string operation)
    {
        _accessPatterns.AddOrUpdate(operation, DateTime.UtcNow, (key, oldValue) => DateTime.UtcNow);

        // Cleanup old access patterns (keep last 50)
        if (_accessPatterns.Count > 50)
        {
            var oldestEntries = _accessPatterns
                .OrderBy(kvp => kvp.Value)
                .Take(_accessPatterns.Count - 40)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldestEntries)
            {
                _accessPatterns.TryRemove(key, out _);
            }
        }
    }

    #endregion

    #region Standard Repository Methods (Optimized)

    public virtual async Task<List<T>> GetFilteredAsync(string? searchText = null, bool? statusFilter = null)
    {
        using (this.LogPerformance($"Get Filtered {EntityTypeName}"))
        {
            TrackAccess("GetFiltered");

            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var allEntities = await GetAllAsync(statusFilter != true);

                return await Task.Run(() =>
                {
                    var filtered = allEntities.AsParallel();

                    if (!string.IsNullOrEmpty(searchText))
                    {
                        filtered = ApplyTextSearchParallel(filtered, searchText);
                    }

                    if (statusFilter.HasValue)
                    {
                        filtered = filtered.Where(e => e.IsActive == statusFilter.Value);
                    }

                    return filtered.ToList();
                });
            }, $"Filtered {EntityTypeName}");

            return result.Success && result.Data != null ? result.Data : [];
        }
    }

    public virtual async Task<T?> GetByNameAsync(string name)
    {
        using (this.LogPerformance($"Get {EntityTypeName} by Name"))
        {
            TrackAccess($"GetByName-{name}");

            // Try cache first
            var cachedEntity = _entityCache.Values.FirstOrDefault(e =>
                string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));

            if (cachedEntity != null)
            {
                this.LogInfo("Found by name in cache");
                return cachedEntity;
            }

            // Load all and try again
            await GetAllAsync(true);
            return _entityCache.Values.FirstOrDefault(e =>
                string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }

    public virtual async Task<int> DeleteMultipleAsync(IEnumerable<Guid> ids)
    {
        var idsList = ids.ToList();
        using (this.LogPerformance($"Delete Multiple {EntityTypeName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                this.LogDataOperation("Bulk deleting", EntityTypeName, $"{idsList.Count} items");

                // Delete in parallel batches for better performance
                var deleteTasks = idsList.Select(async id =>
                {
                    var success = await DeleteInServiceAsync(id);
                    if (success)
                    {
                        _entityCache.TryRemove(id, out _);
                    }
                    return success;
                });

                var results = await Task.WhenAll(deleteTasks);
                var deletedCount = results.Count(success => success);

                this.LogDataOperation("Bulk deleted", EntityTypeName, $"{deletedCount} items successfully");
                return deletedCount;
            }, $"Bulk Delete {EntityTypeName}");

            return result.Success ? result.Data : 0;
        }
    }

    public virtual async Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
    {
        using (this.LogPerformance($"Check Name Exists {EntityTypeName}"))
        {
            TrackAccess($"NameExists-{name}");

            // Use cache if available
            if (!_entityCache.IsEmpty)
            {
                var exists = _entityCache.Values.Any(e =>
                    string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase) &&
                    e.Id != excludeId);

                this.LogInfo($"Name '{name}' exists (cached): {exists}");
                return exists;
            }

            // Fallback to service
            return await NameExistsInServiceAsync(name, excludeId);
        }
    }

    public virtual async Task<T> ToggleFavoriteAsync(Guid entityId)
    {
        using (this.LogPerformance($"Toggle Favorite {EntityTypeName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var entity = await GetByIdAsync(entityId) ?? throw new ArgumentException($"{EntityTypeName} with ID {entityId} not found");
                var originalStatus = entity.IsFavorite;
                entity.IsFavorite = !entity.IsFavorite;
                entity.UpdatedAt = DateTime.UtcNow;

                var updatedEntity = await UpdateAsync(entity);
                this.LogDataOperation("Favorite toggled", EntityTypeName, $"{entity.Name} {originalStatus} → {updatedEntity.IsFavorite}");

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

    public virtual async Task RefreshCacheAsync()
    {
        using (this.LogPerformance($"Manual Refresh Cache {EntityTypeName}"))
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

    public virtual async Task<BaseStatistics> GetStatisticsAsync()
    {
        using (this.LogPerformance($"Get {EntityTypeName} Statistics"))
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
        }
    }

    // BaseRepository.cs - Adicionar campos estáticos
    private static bool? _globalConnectionState;
    private static DateTime? _globalConnectionTest;
    private static readonly TimeSpan _connectionCacheTime = TimeSpan.FromMinutes(2);

    public virtual async Task<bool> TestConnectionAsync()
    {
        // 🚀 PERFORMANCE: Use global connection state se disponível
        if (_globalConnectionTest.HasValue &&
            DateTime.UtcNow - _globalConnectionTest.Value < _connectionCacheTime &&
            _globalConnectionState.HasValue)
        {
            this.LogInfo($"Using global cached connection state: {_globalConnectionState.Value}");
            return _globalConnectionState.Value;
        }

        var result = await this.SafeDataExecuteAsync(async () =>
        {
            try
            {
                var entities = await GetAllFromServiceAsync();
                var isConnected = entities != null;

                // 🚀 PERFORMANCE: Cache globalmente para todos os repositórios
                _globalConnectionState = isConnected;
                _globalConnectionTest = DateTime.UtcNow;

                return isConnected;
            }
            catch
            {
                _globalConnectionState = false;
                _globalConnectionTest = DateTime.UtcNow;
                return false;
            }
        }, "Connection Test");

        return result.Success && result.Data;
    }

    public virtual string GetCacheInfo()
    {
        var cacheAge = _lastCacheUpdate.HasValue
            ? DateTime.UtcNow - _lastCacheUpdate.Value
            : TimeSpan.Zero;

        var accessCount = _accessPatterns.Count;
        var recentAccess = _accessPatterns.Values.Count(time =>
            DateTime.UtcNow - time < TimeSpan.FromMinutes(5));

        return $"{EntityTypeName} cache: {_entityCache.Count} entities, age: {cacheAge.TotalMinutes:F1}min, " +
               $"valid: {IsCacheValid()}, access patterns: {accessCount} (recent: {recentAccess})";
    }

    public virtual void InvalidateCacheExternal()
    {
        _lastCacheUpdate = null;
        this.LogInfo($"{EntityTypeName} cache invalidated externally");
    }

    #endregion

    #region Performance Optimized Helper Methods

    /// <summary>
    /// Optimized parallel text search with improved string comparisons
    /// </summary>
    protected virtual ParallelQuery<T> ApplyTextSearchParallel(ParallelQuery<T> entities, string searchText)
    {
        return entities.Where(e =>
            e.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(e.Description) && e.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase)));
    }

    #endregion

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _backgroundRefreshTimer?.Dispose();
            _semaphore?.Dispose();
        }
    }
    /*
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Performance Extensions

    /// <summary>
    /// Performance optimized: Get multiple entities by IDs in a single efficient operation
    /// </summary>
    public virtual async Task<List<T>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idsList = ids.ToList();
        if (idsList.Count == 0) return [];

        using (this.LogPerformance($"Batch Get {EntityTypeName} by {idsList.Count} IDs"))
        {
            TrackAccess($"GetByIds-{idsList.Count}");

            // Try to get from cache first
            var cachedEntities = new List<T>();
            var missingIds = new List<Guid>();

            foreach (var id in idsList)
            {
                if (_entityCache.TryGetValue(id, out var cachedEntity))
                {
                    cachedEntities.Add(cachedEntity);
                }
                else
                {
                    missingIds.Add(id);
                }
            }

            // If we have all entities cached, return immediately
            if (missingIds.Count == 0)
            {
                this.LogInfo($"All {idsList.Count} entities found in cache");
                return cachedEntities;
            }

            // Load missing entities efficiently
            if (missingIds.Count < idsList.Count / 2)
            {
                // Less than half missing - load individually
                foreach (var id in missingIds)
                {
                    var entity = await GetByIdFromServiceAsync(id);
                    if (entity != null)
                    {
                        cachedEntities.Add(entity);
                        _entityCache.TryAdd(entity.Id, entity);
                    }
                }
            }
            else
            {
                // More than half missing - refresh entire cache
                await RefreshCacheInternalAsync();

                // Get all requested entities from refreshed cache
                cachedEntities.Clear();
                foreach (var id in idsList)
                {
                    if (_entityCache.TryGetValue(id, out var entity))
                    {
                        cachedEntities.Add(entity);
                    }
                }
            }

            return cachedEntities;
        }
    }

    /// <summary>
    /// Performance optimized: Preload related entities to warm cache
    /// </summary>
    public virtual async Task WarmCacheAsync(TimeSpan? maxAge = null)
    {
        var cacheAge = _lastCacheUpdate.HasValue ? DateTime.UtcNow - _lastCacheUpdate.Value : TimeSpan.MaxValue;
        var maxCacheAge = maxAge ?? TimeSpan.FromMinutes(5);

        if (cacheAge < maxCacheAge && _entityCache.Count > 0)
        {
            this.LogInfo($"Cache is warm (age: {cacheAge.TotalMinutes:F1}min) - skipping");
            return;
        }

        using (this.LogPerformance($"Warm Cache {EntityTypeName}"))
        {
            await _semaphore.WaitAsync();
            try
            {
                this.LogInfo($"Warming cache - current age: {cacheAge.TotalMinutes:F1}min");
                await RefreshCacheInternalAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    /// <summary>
    /// Performance monitoring: Get detailed cache statistics
    /// </summary>
    public virtual Dictionary<string, object> GetCacheStatistics()
    {
        var cacheAge = _lastCacheUpdate.HasValue ? DateTime.UtcNow - _lastCacheUpdate.Value : TimeSpan.Zero;
        var recentAccessCount = _accessPatterns.Values.Count(time =>
            DateTime.UtcNow - time < TimeSpan.FromMinutes(5));

        return new Dictionary<string, object>
        {
            ["EntityCount"] = _entityCache.Count,
            ["CacheAgeMinutes"] = cacheAge.TotalMinutes,
            ["IsValid"] = IsCacheValid(),
            ["IsStale"] = IsCacheStale(),
            ["AccessPatternsCount"] = _accessPatterns.Count,
            ["RecentAccessCount"] = recentAccessCount,
            ["LastUpdate"] = _lastCacheUpdate?.ToString() ?? "Never",
            ["MemoryPressure"] = GC.GetTotalMemory(false)
        };
    }
    */
    #endregion
}