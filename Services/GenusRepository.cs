using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;

namespace OrchidPro.Services;

/// <summary>
/// Repository implementation for genus operations with intelligent caching and family relationships.
/// Provides comprehensive CRUD operations following the established architecture patterns.
/// </summary>
public class GenusRepository : IGenusRepository
{
    #region Private Fields

    private readonly SupabaseService _supabaseService;
    private readonly SupabaseGenusService _genusService;
    private readonly IFamilyRepository _familyRepository;
    private readonly List<Genus> _cache = new();
    private DateTime? _lastCacheUpdate;
    private readonly TimeSpan _cacheValidTime = TimeSpan.FromMinutes(5);
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly object _cacheLock = new object();

    #endregion

    #region Constructor

    /// <summary>
    /// Initialize genus repository with Supabase service and family repository
    /// </summary>
    public GenusRepository(SupabaseService supabaseService, SupabaseGenusService genusService, IFamilyRepository familyRepository)
    {
        _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        _genusService = genusService ?? throw new ArgumentNullException(nameof(genusService));
        _familyRepository = familyRepository ?? throw new ArgumentNullException(nameof(familyRepository));
        this.LogInfo("GenusRepository initialized with caching and family relationships");
    }

    #endregion

    #region IBaseRepository Implementation

    /// <summary>
    /// Gets all genera with optional inactive inclusion
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
    /// Gets filtered genera based on search criteria
    /// </summary>
    public async Task<List<Genus>> GetFilteredAsync(string? searchText = null, bool? statusFilter = null)
    {
        using (this.LogPerformance("Get Filtered Genera"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var allGenera = await GetAllAsync(statusFilter != true);

                var filtered = allGenera.AsEnumerable();

                // Apply search filter
                if (!string.IsNullOrEmpty(searchText))
                {
                    var searchLower = searchText.ToLower();
                    filtered = filtered.Where(g =>
                        g.Name.ToLower().Contains(searchLower) ||
                        (!string.IsNullOrEmpty(g.Description) && g.Description.ToLower().Contains(searchLower)));
                }

                // Apply status filter
                if (statusFilter.HasValue)
                {
                    filtered = filtered.Where(g => g.IsActive == statusFilter.Value);
                }

                var result = filtered.ToList();
                this.LogInfo($"Filtered to {result.Count} genera");
                return result;
            }, "Genera");

            return result.Success && result.Data != null ? result.Data : new List<Genus>();
        }
    }

    /// <summary>
    /// Gets genus by unique identifier
    /// </summary>
    public async Task<Genus?> GetByIdAsync(Guid id)
    {
        using (this.LogPerformance("Get Genus By ID"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var allGenera = await GetAllAsync(true);
                var genus = allGenera.FirstOrDefault(g => g.Id == id);

                if (genus != null)
                {
                    this.LogInfo($"Found genus: {genus.Name}");
                }
                else
                {
                    this.LogWarning($"Genus not found: {id}");
                }

                return genus;
            }, "Genus");

            return result.Success ? result.Data : null;
        }
    }

    /// <summary>
    /// Gets genus by name
    /// </summary>
    public async Task<Genus?> GetByNameAsync(string name)
    {
        using (this.LogPerformance("Get Genus By Name"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                if (string.IsNullOrEmpty(name))
                {
                    this.LogWarning("GetByNameAsync called with empty name");
                    return null;
                }

                var allGenera = await GetAllAsync(true);
                var genus = allGenera.FirstOrDefault(g =>
                    string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase));

                if (genus != null)
                {
                    this.LogInfo($"Found genus: {genus.Name} (ID: {genus.Id})");
                }

                return genus;
            }, "Genus");

            return result.Success ? result.Data : null;
        }
    }

    /// <summary>
    /// Creates new genus
    /// </summary>
    public async Task<Genus> CreateAsync(Genus entity)
    {
        using (this.LogPerformance("Create Genus"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                // Validate before creation
                if (!entity.IsValid(out var errors))
                {
                    var errorMsg = string.Join(", ", errors);
                    throw new ArgumentException($"Invalid genus: {errorMsg}");
                }

                // Validate family exists and is accessible
                if (!await ValidateFamilyAccessAsync(entity.FamilyId))
                {
                    throw new ArgumentException("Invalid or inaccessible family");
                }

                // Check for duplicate name within family
                if (await NameExistsInFamilyAsync(entity.Name, entity.FamilyId))
                {
                    throw new ArgumentException($"Genus '{entity.Name}' already exists in this family");
                }

                entity.Id = Guid.NewGuid();
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                // Set user ID from current session
                var userIdString = _supabaseService.GetCurrentUserId();
                if (Guid.TryParse(userIdString, out Guid userId))
                {
                    entity.UserId = userId;
                }
                else
                {
                    entity.UserId = null; // System default if cannot parse
                }

                this.LogDataOperation("Creating", "Genus", entity.Name);

                var created = await _genusService.CreateAsync(entity);
                InvalidateCache();

                this.LogDataOperation("Created", "Genus", $"{created.Name} successfully");
                return created;
            }, "Genus");

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
    /// Updates existing genus
    /// </summary>
    public async Task<Genus> UpdateAsync(Genus entity)
    {
        using (this.LogPerformance("Update Genus"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                entity.UpdatedAt = DateTime.UtcNow;
                this.LogDataOperation("Updating", "Genus", entity.Name);

                var updated = await _genusService.UpdateAsync(entity);
                InvalidateCache();

                this.LogDataOperation("Updated", "Genus", $"{updated.Name} successfully");
                return updated;
            }, "Genus");

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
    /// Deletes genus by identifier
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        using (this.LogPerformance("Delete Genus"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                this.LogDataOperation("Deleting", "Genus", id);

                var deleted = await _genusService.DeleteAsync(id);
                if (deleted)
                {
                    InvalidateCache();
                    this.LogDataOperation("Deleted", "Genus", $"{id} successfully");
                }

                return deleted;
            }, "Genus");

            return result.Success && result.Data;
        }
    }

    /// <summary>
    /// Deletes multiple genera by identifiers
    /// </summary>
    public async Task<int> DeleteMultipleAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.ToList();
        using (this.LogPerformance($"Bulk Delete {idList.Count} Genera"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var deleted = await _genusService.DeleteMultipleAsync(idList);
                if (deleted > 0)
                {
                    InvalidateCache();
                }
                return deleted;
            }, "Genera");

            return result.Success ? result.Data : 0;
        }
    }

    /// <summary>
    /// Checks if name already exists, optionally excluding specific entity
    /// </summary>
    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
    {
        using (this.LogPerformance("Check Name Exists"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var genera = await GetAllAsync(true);
                var exists = genera.Any(g =>
                    string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase) &&
                    g.Id != excludeId);

                this.LogInfo($"Name '{name}' exists: {exists}");
                return exists;
            }, "Name Check");

            return result.Success && result.Data;
        }
    }

    /// <summary>
    /// Gets comprehensive statistics for genera
    /// </summary>
    public async Task<BaseStatistics> GetStatisticsAsync()
    {
        using (this.LogPerformance("Get Genus Statistics"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var genera = await GetAllAsync(true);

                return new BaseStatistics
                {
                    TotalCount = genera.Count,
                    ActiveCount = genera.Count(g => g.IsActive),
                    InactiveCount = genera.Count(g => !g.IsActive),
                    SystemDefaultCount = genera.Count(g => g.IsSystemDefault),
                    UserCreatedCount = genera.Count(g => !g.IsSystemDefault),
                    LastRefreshTime = _lastCacheUpdate ?? DateTime.UtcNow
                };
            }, "Statistics");

            return result.Success && result.Data != null ? result.Data : new BaseStatistics();
        }
    }

    /// <summary>
    /// Refreshes cached data
    /// </summary>
    public async Task RefreshCacheAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            InvalidateCache();
            await RefreshCacheInternalAsync();
            this.LogInfo("Genus cache refreshed manually");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Tests repository connection
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            return await _genusService.TestConnectionAsync();
        }, "Connection Test");

        return result.Success && result.Data;
    }

    /// <summary>
    /// Gets cache information for debugging
    /// </summary>
    public string GetCacheInfo()
    {
        lock (_cacheLock)
        {
            var cacheAge = _lastCacheUpdate.HasValue
                ? DateTime.UtcNow - _lastCacheUpdate.Value
                : TimeSpan.Zero;

            return $"Genus cache: {_cache.Count} entries, age: {cacheAge.TotalMinutes:F1}min, valid: {IsCacheValid()}";
        }
    }

    /// <summary>
    /// Invalidates external cache
    /// </summary>
    public void InvalidateCacheExternal()
    {
        InvalidateCache();
    }

    #endregion

    #region IGenusRepository Family-Specific Implementation

    /// <summary>
    /// Gets all genera belonging to a specific family
    /// </summary>
    public async Task<List<Genus>> GetByFamilyIdAsync(Guid familyId, bool includeInactive = false)
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            var allGenera = await GetAllAsync(includeInactive);
            return allGenera.Where(g => g.FamilyId == familyId).ToList();
        }, "Genera by Family");

        return result.Success && result.Data != null ? result.Data : new List<Genus>();
    }

    /// <summary>
    /// Gets filtered genera by family with search and status filter
    /// </summary>
    public async Task<List<Genus>> GetFilteredByFamilyAsync(Guid familyId, string? searchText = null, bool? statusFilter = null)
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            var familyGenera = await GetByFamilyIdAsync(familyId, statusFilter != true);

            var filtered = familyGenera.AsEnumerable();

            if (!string.IsNullOrEmpty(searchText))
            {
                var searchLower = searchText.ToLower();
                filtered = filtered.Where(g =>
                    g.Name.ToLower().Contains(searchLower) ||
                    (!string.IsNullOrEmpty(g.Description) && g.Description.ToLower().Contains(searchLower)));
            }

            if (statusFilter.HasValue)
            {
                filtered = filtered.Where(g => g.IsActive == statusFilter.Value);
            }

            return filtered.ToList();
        }, "Filtered Genera by Family");

        return result.Success && result.Data != null ? result.Data : new List<Genus>();
    }

    /// <summary>
    /// Gets count of genera in a specific family
    /// </summary>
    public async Task<int> GetCountByFamilyAsync(Guid familyId, bool includeInactive = false)
    {
        var genera = await GetByFamilyIdAsync(familyId, includeInactive);
        return genera.Count;
    }

    /// <summary>
    /// Checks if genus name exists within a specific family
    /// </summary>
    public async Task<bool> NameExistsInFamilyAsync(string name, Guid familyId, Guid? excludeId = null)
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            var familyGenera = await GetByFamilyIdAsync(familyId, true);
            var exists = familyGenera.Any(g =>
                string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase) &&
                g.Id != excludeId);
            return exists;
        }, "Name Check in Family");

        return result.Success && result.Data;
    }

    /// <summary>
    /// Validates that the family exists and is accessible to current user
    /// </summary>
    public async Task<bool> ValidateFamilyAccessAsync(Guid familyId)
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            var family = await _familyRepository.GetByIdAsync(familyId);
            return family != null;
        }, "Family Validation");

        return result.Success && result.Data;
    }

    /// <summary>
    /// Gets full family information for genera in results
    /// </summary>
    public async Task<List<Genus>> PopulateFamilyDataAsync(List<Genus> genera)
    {
        if (!genera.Any()) return genera;

        var result = await this.SafeDataExecuteAsync(async () =>
        {
            var familyIds = genera.Select(g => g.FamilyId).Distinct().ToList();
            var families = new Dictionary<Guid, Family>();

            foreach (var familyId in familyIds)
            {
                var family = await _familyRepository.GetByIdAsync(familyId);
                if (family != null)
                {
                    families[familyId] = family;
                }
            }

            foreach (var genus in genera)
            {
                if (families.TryGetValue(genus.FamilyId, out var family))
                {
                    genus.Family = family;
                }
            }

            return genera;
        }, "Populate Family Data");

        return result.Success && result.Data != null ? result.Data : genera;
    }

    /// <summary>
    /// Gets genera with their family information in a single query
    /// </summary>
    public async Task<List<Genus>> GetAllWithFamilyAsync(bool includeInactive = false)
    {
        var genera = await GetAllAsync(includeInactive);
        return await PopulateFamilyDataAsync(genera);
    }

    /// <summary>
    /// Gets filtered genera with family information
    /// </summary>
    public async Task<List<Genus>> GetFilteredWithFamilyAsync(string? searchText = null, bool? statusFilter = null, Guid? familyId = null)
    {
        List<Genus> genera;
        if (familyId.HasValue)
        {
            genera = await GetFilteredByFamilyAsync(familyId.Value, searchText, statusFilter);
        }
        else
        {
            genera = await GetFilteredAsync(searchText, statusFilter);
        }

        return await PopulateFamilyDataAsync(genera);
    }

    /// <summary>
    /// Gets genus statistics including family distribution
    /// </summary>
    public async Task<GenusStatistics> GetGenusStatisticsAsync()
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            var genera = await GetAllAsync(true);
            var familyGroups = genera.GroupBy(g => g.FamilyId).ToList();

            return new GenusStatistics
            {
                TotalCount = genera.Count,
                ActiveCount = genera.Count(g => g.IsActive),
                InactiveCount = genera.Count(g => !g.IsActive),
                SystemDefaultCount = genera.Count(g => g.IsSystemDefault),
                UserCreatedCount = genera.Count(g => !g.IsSystemDefault),
                LastRefreshTime = DateTime.UtcNow,
                UniqueFamiliesCount = familyGroups.Count,
                AverageGeneraPerFamily = familyGroups.Count > 0 ? (double)genera.Count / familyGroups.Count : 0,
                OrphanedGeneraCount = 0
            };
        }, "Extended Statistics");

        return result.Success && result.Data != null ? result.Data : new GenusStatistics();
    }

    /// <summary>
    /// Gets statistics for genera within a specific family
    /// </summary>
    public async Task<BaseStatistics> GetStatisticsByFamilyAsync(Guid familyId)
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            var familyGenera = await GetByFamilyIdAsync(familyId, true);

            return new BaseStatistics
            {
                TotalCount = familyGenera.Count,
                ActiveCount = familyGenera.Count(g => g.IsActive),
                InactiveCount = familyGenera.Count(g => !g.IsActive),
                SystemDefaultCount = familyGenera.Count(g => g.IsSystemDefault),
                UserCreatedCount = familyGenera.Count(g => !g.IsSystemDefault),
                LastRefreshTime = DateTime.UtcNow
            };
        }, "Family Statistics");

        return result.Success && result.Data != null ? result.Data : new BaseStatistics();
    }

    /// <summary>
    /// Deletes all genera belonging to a family (cascade operation)
    /// </summary>
    public async Task<int> DeleteByFamilyAsync(Guid familyId)
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            var familyGenera = await GetByFamilyIdAsync(familyId, true);
            if (familyGenera.Any())
            {
                var ids = familyGenera.Select(g => g.Id).ToList();
                return await DeleteMultipleAsync(ids);
            }
            return 0;
        }, "Delete by Family");

        return result.Success ? result.Data : 0;
    }

    /// <summary>
    /// Bulk updates family assignment for multiple genera
    /// </summary>
    public async Task<int> BulkUpdateFamilyAsync(List<Guid> genusIds, Guid newFamilyId)
    {
        // Simplified implementation - would need individual updates
        return 0;
    }

    #endregion

    #region Private Cache Management

    /// <summary>
    /// Check if current cache is still valid
    /// </summary>
    private bool IsCacheValid()
    {
        return _lastCacheUpdate.HasValue &&
               DateTime.UtcNow - _lastCacheUpdate.Value < _cacheValidTime &&
               _cache.Any();
    }

    /// <summary>
    /// Get data from cache with filtering
    /// </summary>
    private List<Genus> GetFromCache(bool includeInactive)
    {
        lock (_cacheLock)
        {
            return includeInactive
                ? new List<Genus>(_cache)
                : _cache.Where(g => g.IsActive).ToList();
        }
    }

    /// <summary>
    /// Refresh cache from server
    /// </summary>
    private async Task RefreshCacheInternalAsync()
    {
        try
        {
            this.LogInfo("Refreshing genus cache from server");
            var allGenera = await _genusService.GetAllAsync(true);

            lock (_cacheLock)
            {
                _cache.Clear();
                _cache.AddRange(allGenera);
                _lastCacheUpdate = DateTime.UtcNow;
            }

            this.LogInfo($"Cache refreshed with {allGenera.Count} genera");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Failed to refresh genus cache");
            throw;
        }
    }

    /// <summary>
    /// Invalidate cache
    /// </summary>
    private void InvalidateCache()
    {
        lock (_cacheLock)
        {
            _lastCacheUpdate = null;
            this.LogInfo("Genus cache invalidated");
        }
    }

    /// <summary>
    /// Toggle favorite status for a genus
    /// </summary>
    public async Task<Genus> ToggleFavoriteAsync(Guid genusId)
    {
        using (this.LogPerformance("Toggle Favorite"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                this.LogDataOperation("Toggling favorite", "Genus", genusId);

                // Find current genus
                var genus = await GetByIdAsync(genusId);
                if (genus == null)
                {
                    throw new ArgumentException($"Genus with ID {genusId} not found");
                }

                // Toggle favorite
                var originalFavoriteStatus = genus.IsFavorite;
                genus.IsFavorite = !genus.IsFavorite;
                genus.UpdatedAt = DateTime.UtcNow;

                this.LogDataOperation("Toggled favorite", "Genus", $"'{genus.Name}' {originalFavoriteStatus} → {genus.IsFavorite}");

                // Save to database
                var result = await UpdateAsync(genus);

                this.LogDataOperation("Favorite toggled", "Genus", $"{genus.Name} successfully");
                return result;
            }, "Genus");

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

    #endregion
}