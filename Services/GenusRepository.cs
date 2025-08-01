using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;

namespace OrchidPro.Services;

/// <summary>
/// Repository for Genus entities providing caching, data access, and business operations
/// Implements comprehensive CRUD operations with intelligent caching and offline support
/// Follows exact pattern from FamilyRepository
/// </summary>
public class GenusRepository : IGenusRepository
{
    #region Private Fields

    private readonly SupabaseService _supabaseService;
    private readonly SupabaseGenusService _genusService;
    private readonly List<Genus> _cache = new();
    private DateTime? _lastCacheUpdate;
    private readonly TimeSpan _cacheValidTime = TimeSpan.FromMinutes(5);
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly object _cacheLock = new object();

    #endregion

    #region Constructor

    public GenusRepository(SupabaseService supabaseService, SupabaseGenusService genusService)
    {
        _supabaseService = supabaseService;
        _genusService = genusService;

        this.LogInfo("GenusRepository initialized with ToggleFavoriteAsync support");
    }

    #endregion

    #region IBaseRepository<Genus> Implementation

    /// <summary>
    /// Retrieve all genera with intelligent caching
    /// </summary>
    public async Task<List<Genus>> GetAllAsync(bool includeInactive = false)
    {
        using (this.LogPerformance("Get All Genera"))
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
                }, "Genera");

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
    /// Retrieve genera with search and status filters (corrected signature)
    /// </summary>
    public async Task<List<Genus>> GetFilteredAsync(string? searchText = null, bool? isActive = null)
    {
        using (this.LogPerformance("Get Filtered Genera"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                // Try server first if connected
                var isConnected = await TestConnectionAsync();
                if (isConnected)
                {
                    this.LogInfo("Connected - fetching filtered data from server");
                    var serverData = await _genusService.GetFilteredAsync(searchText, isActive, null);
                    if (serverData != null)
                    {
                        return serverData;
                    }
                }

                // Fallback to cache with local filtering
                this.LogInfo("Using cached data with local filtering");
                var cached = GetFromCache(true); // Get all from cache
                return ApplyLocalFilters(cached, searchText, isActive, null);
            }, "Filtered Genera");

            return result.Data ?? new List<Genus>();
        }
    }

    /// <summary>
    /// Get genus by ID with caching support
    /// </summary>
    public async Task<Genus?> GetByIdAsync(Guid id)
    {
        using (this.LogPerformance($"Get Genus By ID: {id}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                // Check cache first
                lock (_cacheLock)
                {
                    var cached = _cache.FirstOrDefault(g => g.Id == id);
                    if (cached != null)
                    {
                        this.LogInfo("Found in cache");
                        return cached;
                    }
                }

                // Try server if connected
                var isConnected = await TestConnectionAsync();
                if (isConnected)
                {
                    this.LogInfo("Not in cache - fetching from server");
                    var serverData = await _genusService.GetByIdAsync(id);
                    if (serverData != null)
                    {
                        // Add to cache
                        lock (_cacheLock)
                        {
                            var existingIndex = _cache.FindIndex(g => g.Id == id);
                            if (existingIndex >= 0)
                            {
                                _cache[existingIndex] = serverData;
                            }
                            else
                            {
                                _cache.Add(serverData);
                            }
                        }
                        return serverData;
                    }
                }

                this.LogWarning($"Genus {id} not found");
                return null;
            }, "Genus By ID");

            return result.Data;
        }
    }

    /// <summary>
    /// Get genus by name
    /// </summary>
    public async Task<Genus?> GetByNameAsync(string name)
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            // Check cache first
            lock (_cacheLock)
            {
                var cached = _cache.FirstOrDefault(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (cached != null)
                {
                    return cached;
                }
            }

            // Try server if connected
            var isConnected = await TestConnectionAsync();
            if (isConnected)
            {
                var filtered = await _genusService.GetFilteredAsync(name, true, null);
                return filtered?.FirstOrDefault(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }, "Genus By Name");

        return result.Data;
    }

    /// <summary>
    /// Create new genus with cache update
    /// </summary>
    public async Task<Genus?> CreateAsync(Genus entity)
    {
        using (this.LogPerformance($"Create Genus: {entity.Name}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                // Set user ID and timestamps
                entity.UserId = _supabaseService.GetCurrentUserId();
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                // Validate before creating
                if (!entity.IsValid(out var errors))
                {
                    throw new ArgumentException($"Invalid genus: {string.Join(", ", errors)}");
                }

                var created = await _genusService.CreateAsync(entity);
                if (created != null)
                {
                    // Add to cache
                    lock (_cacheLock)
                    {
                        _cache.Add(created);
                    }
                    this.LogSuccess($"Created genus: {created.Name}");
                }

                return created;
            }, "Create Genus");

            return result.Data;
        }
    }

    /// <summary>
    /// Update existing genus with cache update
    /// </summary>
    public async Task<Genus?> UpdateAsync(Genus entity)
    {
        using (this.LogPerformance($"Update Genus: {entity.Name}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                // Set update timestamp
                entity.UpdatedAt = DateTime.UtcNow;

                // Validate before updating
                if (!entity.IsValid(out var errors))
                {
                    throw new ArgumentException($"Invalid genus: {string.Join(", ", errors)}");
                }

                var updated = await _genusService.UpdateAsync(entity);
                if (updated != null)
                {
                    // Update in cache
                    lock (_cacheLock)
                    {
                        var index = _cache.FindIndex(g => g.Id == entity.Id);
                        if (index >= 0)
                        {
                            _cache[index] = updated;
                        }
                    }
                    this.LogSuccess($"Updated genus: {updated.Name}");
                }

                return updated;
            }, "Update Genus");

            return result.Data;
        }
    }

    /// <summary>
    /// Delete genus with cache removal
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        using (this.LogPerformance($"Delete Genus: {id}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var success = await _genusService.DeleteAsync(id);
                if (success)
                {
                    // Remove from cache
                    lock (_cacheLock)
                    {
                        _cache.RemoveAll(g => g.Id == id);
                    }
                    this.LogSuccess($"Deleted genus: {id}");
                }

                return success;
            }, "Delete Genus");

            return result.Data;
        }
    }

    /// <summary>
    /// Delete multiple genera with cache update
    /// </summary>
    public async Task<bool> DeleteMultipleAsync(IEnumerable<Guid> ids)
    {
        using (this.LogPerformance($"Delete {ids.Count()} Genera"))
        {
            var results = new List<bool>();
            foreach (var id in ids)
            {
                var success = await DeleteAsync(id);
                results.Add(success);
            }

            var allSuccessful = results.All(r => r);
            this.LogInfo($"Deleted {results.Count(r => r)}/{ids.Count()} genera");

            return allSuccessful;
        }
    }

    /// <summary>
    /// Check if name exists
    /// </summary>
    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
    {
        var genus = await GetByNameAsync(name);
        return genus != null && (!excludeId.HasValue || genus.Id != excludeId.Value);
    }

    /// <summary>
    /// Get statistics
    /// </summary>
    public async Task<RepositoryStatistics> GetStatisticsAsync()
    {
        var all = await GetAllAsync(true);
        return new RepositoryStatistics
        {
            TotalCount = all.Count,
            ActiveCount = all.Count(g => g.IsActive),
            InactiveCount = all.Count(g => !g.IsActive),
            FavoriteCount = all.Count(g => g.IsFavorite),
            LastUpdated = all.Any() ? all.Max(g => g.UpdatedAt) : DateTime.MinValue
        };
    }

    /// <summary>
    /// Get cache info
    /// </summary>
    public CacheInfo GetCacheInfo()
    {
        return new CacheInfo
        {
            Count = _cache.Count,
            LastUpdate = _lastCacheUpdate,
            IsValid = IsCacheValid(),
            ValidUntil = _lastCacheUpdate?.Add(_cacheValidTime)
        };
    }

    /// <summary>
    /// Refresh cache
    /// </summary>
    public async Task RefreshCacheAsync()
    {
        await RefreshCacheInternalAsync();
    }

    /// <summary>
    /// Invalidate cache
    /// </summary>
    public void InvalidateCacheExternal()
    {
        lock (_cacheLock)
        {
            _cache.Clear();
            _lastCacheUpdate = null;
        }
        this.LogInfo("Cache invalidated externally");
    }

    /// <summary>
    /// Test connection (public implementation)
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        return await this.SafeExecuteAsync(async () =>
        {
            return _supabaseService.IsAuthenticated;
        }, false, "Test Connection");
    }

    #endregion

    #region IGenusRepository Implementation

    /// <summary>
    /// Get all genera for a specific family
    /// </summary>
    public async Task<List<Genus>> GetByFamilyAsync(Guid familyId, bool includeInactive = false)
    {
        using (this.LogPerformance($"Get Genera By Family: {familyId}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                // Try server first if connected
                var isConnected = await TestConnectionAsync();
                if (isConnected)
                {
                    this.LogInfo("Connected - fetching from server by family");
                    var serverData = await _genusService.GetByFamilyAsync(familyId, includeInactive);
                    if (serverData != null)
                    {
                        return serverData;
                    }
                }

                // Fallback to cache
                this.LogInfo("Using cached data filtered by family");
                var cached = GetFromCache(true);
                return cached.Where(g => g.FamilyId == familyId && (includeInactive || g.IsActive)).ToList();
            }, "Genera By Family");

            return result.Data ?? new List<Genus>();
        }
    }

    /// <summary>
    /// Get filtered genera with family name included
    /// </summary>
    public async Task<List<Genus>> GetFilteredWithFamilyAsync(string? searchText = null, bool? isActive = null, bool? isFavorite = null, Guid? familyId = null)
    {
        using (this.LogPerformance("Get Filtered Genera With Family"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                // Try server first if connected
                var isConnected = await TestConnectionAsync();
                if (isConnected)
                {
                    this.LogInfo("Connected - fetching filtered data with family from server");
                    var serverData = await _genusService.GetFilteredAsync(searchText, isActive, isFavorite, familyId);
                    if (serverData != null)
                    {
                        return serverData;
                    }
                }

                // Fallback to cache with local filtering
                this.LogInfo("Using cached data with local filtering including family filter");
                var cached = GetFromCache(true);
                var filtered = ApplyLocalFilters(cached, searchText, isActive, isFavorite);

                if (familyId.HasValue)
                {
                    filtered = filtered.Where(g => g.FamilyId == familyId.Value).ToList();
                }

                return filtered;
            }, "Filtered Genera With Family");

            return result.Data ?? new List<Genus>();
        }
    }

    /// <summary>
    /// Toggle favorite status of a genus
    /// </summary>
    public async Task<Genus?> ToggleFavoriteAsync(Guid genusId)
    {
        using (this.LogPerformance($"Toggle Genus Favorite: {genusId}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var updated = await _genusService.ToggleFavoriteAsync(genusId);
                if (updated != null)
                {
                    // Update in cache
                    lock (_cacheLock)
                    {
                        var index = _cache.FindIndex(g => g.Id == genusId);
                        if (index >= 0)
                        {
                            _cache[index] = updated;
                        }
                    }
                    this.LogSuccess($"Toggled favorite for genus: {updated.Name} -> {updated.IsFavorite}");
                }

                return updated;
            }, "Toggle Genus Favorite");

            return result.Data;
        }
    }

    /// <summary>
    /// Get genera count for a specific family
    /// </summary>
    public async Task<int> GetCountByFamilyAsync(Guid familyId, bool includeInactive = false)
    {
        using (this.LogPerformance($"Get Genus Count By Family: {familyId}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                // Try server first if connected
                var isConnected = await TestConnectionAsync();
                if (isConnected)
                {
                    var count = await _genusService.GetCountByFamilyAsync(familyId, includeInactive);
                    return count;
                }

                // Fallback to cache count
                var cached = GetFromCache(true);
                return cached.Count(g => g.FamilyId == familyId && (includeInactive || g.IsActive));
            }, "Genus Count By Family");

            return result.Data;
        }
    }

    /// <summary>
    /// Bulk delete genera by family ID (when family is deleted)
    /// </summary>
    public async Task<bool> DeleteByFamilyAsync(Guid familyId)
    {
        using (this.LogPerformance($"Delete Genera By Family: {familyId}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var success = await _genusService.DeleteByFamilyAsync(familyId);
                if (success)
                {
                    // Remove from cache
                    lock (_cacheLock)
                    {
                        _cache.RemoveAll(g => g.FamilyId == familyId);
                    }
                    this.LogSuccess($"Deleted all genera for family: {familyId}");
                }

                return success;
            }, "Delete Genera By Family");

            return result.Data;
        }
    }

    /// <summary>
    /// Check if genus name exists within the same family
    /// </summary>
    public async Task<bool> ExistsInFamilyAsync(string name, Guid familyId, Guid? excludeId = null)
    {
        using (this.LogPerformance($"Check Genus Exists In Family: {name}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                // Try server first if connected
                var isConnected = await TestConnectionAsync();
                if (isConnected)
                {
                    return await _genusService.ExistsInFamilyAsync(name, familyId, excludeId);
                }

                // Fallback to cache check
                var cached = GetFromCache(true);
                return cached.Any(g =>
                    g.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                    g.FamilyId == familyId &&
                    (!excludeId.HasValue || g.Id != excludeId.Value));
            }, "Check Genus Exists In Family");

            return result.Data;
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Check if cache is still valid
    /// </summary>
    private bool IsCacheValid()
    {
        return _lastCacheUpdate.HasValue &&
               DateTime.UtcNow - _lastCacheUpdate.Value < _cacheValidTime;
    }

    /// <summary>
    /// Get data from cache with filtering
    /// </summary>
    private List<Genus> GetFromCache(bool includeInactive)
    {
        lock (_cacheLock)
        {
            return _cache.Where(g => includeInactive || g.IsActive).ToList();
        }
    }

    /// <summary>
    /// Apply local filters to genus list
    /// </summary>
    private List<Genus> ApplyLocalFilters(List<Genus> genera, string? searchText, bool? isActive, bool? isFavorite)
    {
        var filtered = genera.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var search = searchText.ToLowerInvariant();
            filtered = filtered.Where(g =>
                g.Name.ToLowerInvariant().Contains(search) ||
                (g.Description?.ToLowerInvariant().Contains(search) ?? false) ||
                g.FamilyName.ToLowerInvariant().Contains(search));
        }

        if (isActive.HasValue)
        {
            filtered = filtered.Where(g => g.IsActive == isActive.Value);
        }

        if (isFavorite.HasValue)
        {
            filtered = filtered.Where(g => g.IsFavorite == isFavorite.Value);
        }

        return filtered.ToList();
    }

    /// <summary>
    /// Refresh cache from server
    /// </summary>
    private async Task RefreshCacheInternalAsync()
    {
        var serverData = await _genusService.GetAllWithFamilyAsync(true);
        if (serverData != null)
        {
            lock (_cacheLock)
            {
                _cache.Clear();
                _cache.AddRange(serverData);
                _lastCacheUpdate = DateTime.UtcNow;
            }
            this.LogInfo($"Cache refreshed with {serverData.Count} genera");
        }
    }

    #endregion
}

#region Support Classes

public class RepositoryStatistics
{
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int FavoriteCount { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class CacheInfo
{
    public int Count { get; set; }
    public DateTime? LastUpdate { get; set; }
    public bool IsValid { get; set; }
    public DateTime? ValidUntil { get; set; }
}

#endregion

    /// <summary>
    /// Get genus by ID with caching support
    /// </summary>
    public async Task<Genus?> GetByIdAsync(Guid id)
    {
        using (this.LogPerformance($"Get Genus By ID: {id}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                // Check cache first
                lock (_cacheLock)
                {
                    var cached = _cache.FirstOrDefault(g => g.Id == id);
                    if (cached != null)
                    {
                        this.LogInfo("Found in cache");
                        return cached;
                    }
                }

                // Try server if connected
                var isConnected = await TestConnectionAsync();
                if (isConnected)
                {
                    this.LogInfo("Not in cache - fetching from server");
                    var serverData = await _genusService.GetByIdAsync(id);
                    if (serverData != null)
                    {
                        // Add to cache
                        lock (_cacheLock)
                        {
                            var existingIndex = _cache.FindIndex(g => g.Id == id);
                            if (existingIndex >= 0)
                            {
                                _cache[existingIndex] = serverData;
                            }
                            else
                            {
                                _cache.Add(serverData);
                            }
                        }
                        return serverData;
                    }
                }

                this.LogWarning($"Genus {id} not found");
                return null;
            }, "Genus By ID");

            return result.Data;
        }
    }

    /// <summary>
    /// Create new genus with cache update
    /// </summary>
    public async Task<Genus?> CreateAsync(Genus entity)
    {
        using (this.LogPerformance($"Create Genus: {entity.Name}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                // Set user ID and timestamps
                entity.UserId = _supabaseService.GetCurrentUserId();
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                // Validate before creating
                if (!entity.IsValid(out var errors))
                {
                    throw new ArgumentException($"Invalid genus: {string.Join(", ", errors)}");
                }

                var created = await _genusService.CreateAsync(entity);
                if (created != null)
                {
                    // Add to cache
                    lock (_cacheLock)
                    {
                        _cache.Add(created);
                    }
                    this.LogSuccess($"Created genus: {created.Name}");
                }

                return created;
            }, "Create Genus");

            return result.Data;
        }
    }

    /// <summary>
    /// Update existing genus with cache update
    /// </summary>
    public async Task<Genus?> UpdateAsync(Genus entity)
    {
        using (this.LogPerformance($"Update Genus: {entity.Name}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                // Set update timestamp
                entity.UpdatedAt = DateTime.UtcNow;

                // Validate before updating
                if (!entity.IsValid(out var errors))
                {
                    throw new ArgumentException($"Invalid genus: {string.Join(", ", errors)}");
                }

                var updated = await _genusService.UpdateAsync(entity);
                if (updated != null)
                {
                    // Update in cache
                    lock (_cacheLock)
                    {
                        var index = _cache.FindIndex(g => g.Id == entity.Id);
                        if (index >= 0)
                        {
                            _cache[index] = updated;
                        }
                    }
                    this.LogSuccess($"Updated genus: {updated.Name}");
                }

                return updated;
            }, "Update Genus");

            return result.Data;
        }
    }

    /// <summary>
    /// Delete genus with cache removal
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        using (this.LogPerformance($"Delete Genus: {id}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var success = await _genusService.DeleteAsync(id);
                if (success)
                {
                    // Remove from cache
                    lock (_cacheLock)
                    {
                        _cache.RemoveAll(g => g.Id == id);
                    }
                    this.LogSuccess($"Deleted genus: {id}");
                }

                return success;
            }, "Delete Genus");

            return result.Data;
        }
    }

    /// <summary>
    /// Delete multiple genera with cache update
    /// </summary>
    public async Task<bool> DeleteManyAsync(List<Guid> ids)
    {
        using (this.LogPerformance($"Delete {ids.Count} Genera"))
        {
            var results = new List<bool>();
            foreach (var id in ids)
            {
                var success = await DeleteAsync(id);
                results.Add(success);
            }

            var allSuccessful = results.All(r => r);
            this.LogInfo($"Deleted {results.Count(r => r)}/{ids.Count} genera");

            return allSuccessful;
        }
    }

#endregion

    #region IGenusRepository Implementation

    /// <summary>
    /// Get all genera for a specific family
    /// </summary>
    public async Task<List<Genus>> GetByFamilyAsync(Guid familyId, bool includeInactive = false)
    {
        using (this.LogPerformance($"Get Genera By Family: {familyId}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                // Try server first if connected
                var isConnected = await TestConnectionAsync();
                if (isConnected)
                {
                    this.LogInfo("Connected - fetching from server by family");
                    var serverData = await _genusService.GetByFamilyAsync(familyId, includeInactive);
                    if (serverData != null)
                    {
                        return serverData;
                    }
                }

                // Fallback to cache
                this.LogInfo("Using cached data filtered by family");
                var cached = GetFromCache(true);
                return cached.Where(g => g.FamilyId == familyId && (includeInactive || g.IsActive)).ToList();
            }, "Genera By Family");

            return result.Data ?? new List<Genus>();
        }
    }

    /// <summary>
    /// Get filtered genera with family name included
    /// </summary>
    public async Task<List<Genus>> GetFilteredWithFamilyAsync(string? searchText = null, bool? isActive = null, bool? isFavorite = null, Guid? familyId = null)
    {
        using (this.LogPerformance("Get Filtered Genera With Family"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                // Try server first if connected
                var isConnected = await TestConnectionAsync();
                if (isConnected)
                {
                    this.LogInfo("Connected - fetching filtered data with family from server");
                    var serverData = await _genusService.GetFilteredAsync(searchText, isActive, isFavorite, familyId);
                    if (serverData != null)
                    {
                        return serverData;
                    }
                }

                // Fallback to cache with local filtering
                this.LogInfo("Using cached data with local filtering including family filter");
                var cached = GetFromCache(true);
                var filtered = ApplyLocalFilters(cached, searchText, isActive, isFavorite);

                if (familyId.HasValue)
                {
                    filtered = filtered.Where(g => g.FamilyId == familyId.Value).ToList();
                }

                return filtered;
            }, "Filtered Genera With Family");

            return result.Data ?? new List<Genus>();
        }
    }

    /// <summary>
    /// Toggle favorite status of a genus
    /// </summary>
    public async Task<Genus?> ToggleFavoriteAsync(Guid genusId)
    {
        using (this.LogPerformance($"Toggle Genus Favorite: {genusId}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var updated = await _genusService.ToggleFavoriteAsync(genusId);
                if (updated != null)
                {
                    // Update in cache
                    lock (_cacheLock)
                    {
                        var index = _cache.FindIndex(g => g.Id == genusId);
                        if (index >= 0)
                        {
                            _cache[index] = updated;
                        }
                    }
                    this.LogSuccess($"Toggled favorite for genus: {updated.Name} -> {updated.IsFavorite}");
                }

                return updated;
            }, "Toggle Genus Favorite");

            return result.Data;
        }
    }

    /// <summary>
    /// Get genera count for a specific family
    /// </summary>
    public async Task<int> GetCountByFamilyAsync(Guid familyId, bool includeInactive = false)
    {
        using (this.LogPerformance($"Get Genus Count By Family: {familyId}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                // Try server first if connected
                var isConnected = await TestConnectionAsync();
                if (isConnected)
                {
                    var count = await _genusService.GetCountByFamilyAsync(familyId, includeInactive);
                    return count;
                }

                // Fallback to cache count
                var cached = GetFromCache(true);
                return cached.Count(g => g.FamilyId == familyId && (includeInactive || g.IsActive));
            }, "Genus Count By Family");

            return result.Data;
        }
    }

    /// <summary>
    /// Bulk delete genera by family ID (when family is deleted)
    /// </summary>
    public async Task<bool> DeleteByFamilyAsync(Guid familyId)
    {
        using (this.LogPerformance($"Delete Genera By Family: {familyId}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var success = await _genusService.DeleteByFamilyAsync(familyId);
                if (success)
                {
                    // Remove from cache
                    lock (_cacheLock)
                    {
                        _cache.RemoveAll(g => g.FamilyId == familyId);
                    }
                    this.LogSuccess($"Deleted all genera for family: {familyId}");
                }

                return success;
            }, "Delete Genera By Family");

            return result.Data;
        }
    }

    /// <summary>
    /// Check if genus name exists within the same family
    /// </summary>
    public async Task<bool> ExistsInFamilyAsync(string name, Guid familyId, Guid? excludeId = null)
    {
        using (this.LogPerformance($"Check Genus Exists In Family: {name}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                // Try server first if connected
                var isConnected = await TestConnectionAsync();
                if (isConnected)
                {
                    return await _genusService.ExistsInFamilyAsync(name, familyId, excludeId);
                }

                // Fallback to cache check
                var cached = GetFromCache(true);
                return cached.Any(g =>
                    g.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                    g.FamilyId == familyId &&
                    (!excludeId.HasValue || g.Id != excludeId.Value));
            }, "Check Genus Exists In Family");

            return result.Data;
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Check if cache is still valid
    /// </summary>
    private bool IsCacheValid()
    {
        return _lastCacheUpdate.HasValue &&
               DateTime.UtcNow - _lastCacheUpdate.Value < _cacheValidTime;
    }

    /// <summary>
    /// Get data from cache with filtering
    /// </summary>
    private List<Genus> GetFromCache(bool includeInactive)
    {
        lock (_cacheLock)
        {
            return _cache.Where(g => includeInactive || g.IsActive).ToList();
        }
    }

    /// <summary>
    /// Apply local filters to genus list
    /// </summary>
    private List<Genus> ApplyLocalFilters(List<Genus> genera, string? searchText, bool? isActive, bool? isFavorite)
    {
        var filtered = genera.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var search = searchText.ToLowerInvariant();
            filtered = filtered.Where(g =>
                g.Name.ToLowerInvariant().Contains(search) ||
                (g.Description?.ToLowerInvariant().Contains(search) ?? false) ||
                g.FamilyName.ToLowerInvariant().Contains(search));
        }

        if (isActive.HasValue)
        {
            filtered = filtered.Where(g => g.IsActive == isActive.Value);
        }

        if (isFavorite.HasValue)
        {
            filtered = filtered.Where(g => g.IsFavorite == isFavorite.Value);
        }

        return filtered.ToList();
    }

    /// <summary>
    /// Refresh cache from server
    /// </summary>
    private async Task RefreshCacheInternalAsync()
    {
        var serverData = await _genusService.GetAllWithFamilyAsync(true);
        if (serverData != null)
        {
            lock (_cacheLock)
            {
                _cache.Clear();
                _cache.AddRange(serverData);
                _lastCacheUpdate = DateTime.UtcNow;
            }
            this.LogInfo($"Cache refreshed with {serverData.Count} genera");
        }
    }

    /// <summary>
    /// Test connection to Supabase
    /// </summary>
    private async Task<bool> TestConnectionAsync()
    {
        return await this.SafeExecuteAsync(async () =>
        {
            return _supabaseService.IsAuthenticated;
        }, false, "Test Connection");
    }

    #endregion
}