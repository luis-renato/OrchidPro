using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;

namespace OrchidPro.Services;

/// <summary>
/// Repository for Family entities providing caching, data access, and business operations.
/// Implements comprehensive CRUD operations with intelligent caching and offline support.
/// </summary>
public class FamilyRepository : IFamilyRepository
{
    #region Private Fields

    private readonly SupabaseService _supabaseService;
    private readonly SupabaseFamilyService _familyService;
    private readonly List<Family> _cache = new();
    private DateTime? _lastCacheUpdate;
    private readonly TimeSpan _cacheValidTime = TimeSpan.FromMinutes(5);
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly object _cacheLock = new object();

    #endregion

    #region Constructor

    public FamilyRepository(SupabaseService supabaseService, SupabaseFamilyService familyService)
    {
        _supabaseService = supabaseService;
        _familyService = familyService;

        this.LogInfo("FamilyRepository initialized with ToggleFavoriteAsync support");
    }

    #endregion

    #region IBaseRepository<Family> Implementation

    /// <summary>
    /// Retrieve all families with intelligent caching
    /// </summary>
    public async Task<List<Family>> GetAllAsync(bool includeInactive = false)
    {
        using (this.LogPerformance("Get All Families"))
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
                }, "Families");

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
    /// Retrieve families with search and status filters
    /// </summary>
    public async Task<List<Family>> GetFilteredAsync(string? searchText = null, bool? statusFilter = null)
    {
        using (this.LogPerformance("Get Filtered Families"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var families = await GetAllAsync(true); // Include inactive for filtering

                // Apply text filter
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var searchLower = searchText.ToLowerInvariant();
                    families = families.Where(f =>
                        f.Name.ToLowerInvariant().Contains(searchLower) ||
                        (!string.IsNullOrEmpty(f.Description) && f.Description.ToLowerInvariant().Contains(searchLower))
                    ).ToList();
                }

                // Apply status filter
                if (statusFilter.HasValue)
                {
                    families = families.Where(f => f.IsActive == statusFilter.Value).ToList();
                }

                this.LogDataOperation("Filtered", "Families", $"{families.Count} results");
                return families.OrderBy(f => f.Name).ToList();
            }, "Filtered Families");

            return result.Data ?? new List<Family>();
        }
    }

    /// <summary>
    /// Retrieve family by unique identifier
    /// </summary>
    public async Task<Family?> GetByIdAsync(Guid id)
    {
        using (this.LogPerformance("Get Family By ID"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var families = await GetAllAsync(true);
                var family = families.FirstOrDefault(f => f.Id == id);

                if (family != null)
                {
                    this.LogDataOperation("Found", "Family", $"{family.Name} by ID");
                }
                else
                {
                    this.LogWarning($"Family not found by ID: {id}");
                }

                return family;
            }, "Family");

            return result.Data;
        }
    }

    /// <summary>
    /// Retrieve family by name with case-insensitive matching
    /// </summary>
    public async Task<Family?> GetByNameAsync(string name)
    {
        using (this.LogPerformance("Get Family By Name"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var families = await GetAllAsync(true);
                var family = families.FirstOrDefault(f =>
                    string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));

                if (family != null)
                {
                    this.LogDataOperation("Found", "Family", $"{family.Name} by name");
                }

                return family;
            }, "Family");

            return result.Data;
        }
    }

    /// <summary>
    /// Create new family entity
    /// </summary>
    public async Task<Family> CreateAsync(Family family)
    {
        using (this.LogPerformance("Create Family"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                family.Id = Guid.NewGuid();
                family.CreatedAt = DateTime.UtcNow;
                family.UpdatedAt = DateTime.UtcNow;

                // Set user ID from current session
                var userIdString = _supabaseService.GetCurrentUserId();
                if (Guid.TryParse(userIdString, out Guid userId))
                {
                    family.UserId = userId;
                }
                else
                {
                    family.UserId = null; // System default if cannot parse
                }

                this.LogDataOperation("Creating", "Family", family.Name);

                var result = await _familyService.CreateAsync(family);
                InvalidateCache();

                this.LogDataOperation("Created", "Family", $"{result.Name} successfully");
                return result;
            }, "Family");

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
    /// Update existing family entity
    /// </summary>
    public async Task<Family> UpdateAsync(Family family)
    {
        using (this.LogPerformance("Update Family"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                family.UpdatedAt = DateTime.UtcNow;

                this.LogDataOperation("Updating", "Family", $"{family.Name} (Favorite: {family.IsFavorite})");

                var result = await _familyService.UpdateAsync(family);
                InvalidateCache();

                this.LogDataOperation("Updated", "Family", $"{result.Name} successfully");
                return result;
            }, "Family");

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
    /// Delete family by unique identifier
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        using (this.LogPerformance("Delete Family"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                this.LogDataOperation("Deleting", "Family", id);

                var success = await _familyService.DeleteAsync(id);
                InvalidateCache();

                this.LogDataOperation("Deleted", "Family", "successfully");
                return success;
            }, "Family");

            if (result.Success)
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
    /// Delete multiple families by IDs - now using proper bulk operation
    /// </summary>
    public async Task<int> DeleteMultipleAsync(IEnumerable<Guid> ids)
    {
        using (this.LogPerformance("Delete Multiple Families"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var idsArray = ids.ToList();
                this.LogDataOperation("Deleting", "Families", $"{idsArray.Count} items");

                // ✅ FIXED: Use proper bulk delete from SupabaseFamilyService
                var deletedCount = await _familyService.DeleteMultipleAsync(idsArray);

                InvalidateCache();

                this.LogDataOperation("Deleted", "Families", $"{deletedCount} successfully");
                return deletedCount;
            }, "Multiple Families");

            if (result.Success)
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
    /// Check if family name already exists
    /// </summary>
    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
    {
        using (this.LogPerformance("Check Name Exists"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var families = await GetAllAsync(true);
                var exists = families.Any(f =>
                    string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase) &&
                    f.Id != excludeId);

                this.LogInfo($"Name '{name}' exists: {exists}");
                return exists;
            }, "Name Check");

            return result.Success && result.Data;
        }
    }

    #endregion

    #region IFamilyRepository Specific Methods

    /// <summary>
    /// Toggle favorite status for a family
    /// </summary>
    public async Task<Family> ToggleFavoriteAsync(Guid familyId)
    {
        using (this.LogPerformance("Toggle Favorite"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                this.LogDataOperation("Toggling favorite", "Family", familyId);

                // Find current family
                var family = await GetByIdAsync(familyId);
                if (family == null)
                {
                    throw new ArgumentException($"Family with ID {familyId} not found");
                }

                // Toggle favorite
                var originalFavoriteStatus = family.IsFavorite;
                family.ToggleFavorite(); // Method that already exists in Family model

                this.LogDataOperation("Toggled favorite", "Family", $"'{family.Name}' {originalFavoriteStatus} → {family.IsFavorite}");

                // Save to database
                var result = await UpdateAsync(family);

                this.LogDataOperation("Favorite toggled", "Family", $"{family.Name} successfully");
                return result;
            }, "Family");

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
    /// Get comprehensive family statistics
    /// </summary>
    public async Task<FamilyStatistics> GetFamilyStatisticsAsync()
    {
        using (this.LogPerformance("Get Family Statistics"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var families = await GetAllAsync(true);

                return new FamilyStatistics
                {
                    TotalCount = families.Count,
                    ActiveCount = families.Count(f => f.IsActive),
                    InactiveCount = families.Count(f => !f.IsActive),
                    SystemDefaultCount = families.Count(f => f.IsSystemDefault),
                    UserCreatedCount = families.Count(f => !f.IsSystemDefault),
                    LastRefreshTime = _lastCacheUpdate ?? DateTime.UtcNow
                };
            }, "Statistics");

            if (result.Success && result.Data != null)
            {
                return result.Data;
            }
            else
            {
                this.LogError($"GetFamilyStatisticsAsync error: {result.Message}");
                return new FamilyStatistics();
            }
        }
    }

    #endregion

    #region Connection and Maintenance

    /// <summary>
    /// Test database connectivity
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        return await this.SafeNetworkExecuteAsync(async () =>
        {
            return await _supabaseService.TestSyncConnectionAsync();
        }, "Connection Test");
    }

    /// <summary>
    /// Refresh all data with comprehensive operation tracking
    /// </summary>
    public async Task<OperationResult> RefreshAllDataAsync()
    {
        var startTime = DateTime.UtcNow;

        using (this.LogPerformance("Refresh All Data"))
        {
            this.LogInfo("Refreshing all data from server");

            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var isConnected = await TestConnectionAsync();
                if (!isConnected)
                {
                    throw new InvalidOperationException("Cannot refresh data - no internet connection available");
                }

                await RefreshCacheAsync();

                var families = GetFromCache(true);
                var endTime = DateTime.UtcNow;

                return new OperationResult
                {
                    StartTime = startTime,
                    EndTime = endTime,
                    Duration = endTime - startTime,
                    TotalProcessed = families.Count,
                    Successful = families.Count,
                    Failed = 0,
                    IsSuccess = true,
                    ErrorMessages = new List<string>()
                };
            }, "Refresh Operation");

            if (result.Success && result.Data != null)
            {
                return result.Data;
            }
            else
            {
                this.LogError($"Refresh all data failed: {result.Message}");

                return new OperationResult
                {
                    StartTime = startTime,
                    EndTime = DateTime.UtcNow,
                    Duration = DateTime.UtcNow - startTime,
                    TotalProcessed = 0,
                    Successful = 0,
                    Failed = 1,
                    IsSuccess = false,
                    ErrorMessages = new List<string> { result.Message }
                };
            }
        }
    }

    /// <summary>
    /// Get current cache information for diagnostics
    /// </summary>
    public string GetCacheInfo()
    {
        return this.SafeExecute(() =>
        {
            lock (_cacheLock)
            {
                if (_lastCacheUpdate == null)
                {
                    return "Cache empty";
                }

                var age = DateTime.UtcNow - _lastCacheUpdate.Value;
                var isValid = age < _cacheValidTime;
                var status = isValid ? "VALID" : "EXPIRED";

                return $"Cache: {_cache.Count} families, {age.TotalMinutes:F1}min old, {status}";
            }
        }, fallbackValue: "Cache info unavailable", "Get Cache Info");
    }

    /// <summary>
    /// Invalidate cache externally for forced refresh
    /// </summary>
    public void InvalidateCacheExternal()
    {
        this.SafeExecute(() =>
        {
            lock (_cacheLock)
            {
                _lastCacheUpdate = null;
                _cache.Clear();
                this.LogInfo("Cache invalidated externally");
            }

            _supabaseService.InvalidateConnectionCache();
        }, "Invalidate Cache External");
    }

    /// <summary>
    /// Get general statistics (base interface implementation)
    /// </summary>
    public async Task<BaseStatistics> GetStatisticsAsync()
    {
        using (this.LogPerformance("Get Base Statistics"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var familyStats = await GetFamilyStatisticsAsync();

                // Convert FamilyStatistics to BaseStatistics
                return new BaseStatistics
                {
                    TotalCount = familyStats.TotalCount,
                    ActiveCount = familyStats.ActiveCount,
                    InactiveCount = familyStats.InactiveCount,
                    SystemDefaultCount = familyStats.SystemDefaultCount,
                    UserCreatedCount = familyStats.UserCreatedCount,
                    LastRefreshTime = familyStats.LastRefreshTime
                };
            }, "Base Statistics");

            return result.Data ?? new BaseStatistics();
        }
    }

    /// <summary>
    /// Force cache refresh
    /// </summary>
    public async Task RefreshCacheAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            await this.SafeExecuteAsync(async () =>
            {
                this.LogInfo("Force cache refresh requested");
                await RefreshCacheInternalAsync();
            }, "Refresh Cache");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Get cache status for monitoring
    /// </summary>
    public (bool IsValid, DateTime? LastUpdate, int ItemCount) GetCacheStatus()
    {
        return this.SafeExecute(() =>
        {
            lock (_cacheLock)
            {
                return (IsCacheValid(), _lastCacheUpdate, _cache.Count);
            }
        }, fallbackValue: (false, null, 0), "Get Cache Status");
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Check if cache is valid based on age and content
    /// </summary>
    private bool IsCacheValid()
    {
        return this.SafeExecute(() =>
        {
            lock (_cacheLock)
            {
                return _lastCacheUpdate.HasValue &&
                       DateTime.UtcNow - _lastCacheUpdate.Value < _cacheValidTime &&
                       _cache.Any();
            }
        }, fallbackValue: false, "Check Cache Valid");
    }

    /// <summary>
    /// Internal cache refresh with error handling
    /// </summary>
    private async Task RefreshCacheInternalAsync()
    {
        await this.SafeDataExecuteAsync(async () =>
        {
            var families = await _familyService.GetAllAsync();

            lock (_cacheLock)
            {
                _cache.Clear();
                _cache.AddRange(families);
                _lastCacheUpdate = DateTime.UtcNow;

                this.LogInfo($"Cache refreshed with {families.Count} families");
            }

            return families;
        }, "Cache Refresh");
    }

    /// <summary>
    /// Get data from cache with filtering
    /// </summary>
    private List<Family> GetFromCache(bool includeInactive)
    {
        return this.SafeExecute(() =>
        {
            lock (_cacheLock)
            {
                var families = _cache.AsEnumerable();

                if (!includeInactive)
                {
                    families = families.Where(f => f.IsActive);
                }

                return families.OrderBy(f => f.Name).ToList();
            }
        }, fallbackValue: new List<Family>(), "Get From Cache");
    }

    /// <summary>
    /// Invalidate cache and connection cache
    /// </summary>
    private void InvalidateCache()
    {
        this.SafeExecute(() =>
        {
            lock (_cacheLock)
            {
                _lastCacheUpdate = null;
                this.LogInfo("Cache invalidated");
            }

            _supabaseService.InvalidateConnectionCache();
        }, "Invalidate Cache");
    }

    #endregion
}