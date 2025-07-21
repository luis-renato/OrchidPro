using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Services.Data;
using System.Diagnostics;

namespace OrchidPro.Services;

/// <summary>
/// ✅ CORRIGIDO: FamilyRepository com implementação completa sem duplicação
/// </summary>
public class FamilyRepository : IFamilyRepository
{
    private readonly SupabaseService _supabaseService;
    private readonly SupabaseFamilyService _familyService;
    private readonly List<Family> _cache = new();
    private DateTime? _lastCacheUpdate;
    private readonly TimeSpan _cacheValidTime = TimeSpan.FromMinutes(5);
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly object _cacheLock = new object();

    public FamilyRepository(SupabaseService supabaseService, SupabaseFamilyService familyService)
    {
        _supabaseService = supabaseService;
        _familyService = familyService;

        Debug.WriteLine("✅ [FAMILY_REPO] Initialized with complete IFamilyRepository implementation");
    }

    #region IBaseRepository<Family> Implementation

    /// <summary>
    /// Busca todas as famílias com cache inteligente
    /// </summary>
    public async Task<List<Family>> GetAllAsync(bool includeInactive = false)
    {
        await _semaphore.WaitAsync();
        try
        {
            // Verificar se cache é válido
            if (IsCacheValid())
            {
                Debug.WriteLine("💾 [FAMILY_REPO] Using cached data");
                return GetFromCache(includeInactive);
            }

            // Verificar conectividade antes de tentar servidor
            var isConnected = await TestConnectionAsync();
            if (!isConnected)
            {
                Debug.WriteLine("📡 [FAMILY_REPO] Offline - returning cached data");
                return GetFromCache(includeInactive);
            }

            // Cache inválido e conectado - buscar do servidor
            Debug.WriteLine("🔄 [FAMILY_REPO] Cache expired - fetching from server");
            await RefreshCacheInternalAsync();

            return GetFromCache(includeInactive);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_REPO] GetAllAsync error: {ex.Message}");
            Debug.WriteLine("🆘 [FAMILY_REPO] Using cache as fallback");
            return GetFromCache(includeInactive);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Busca famílias com filtros
    /// </summary>
    public async Task<List<Family>> GetFilteredAsync(string? searchText = null, bool? statusFilter = null)
    {
        var families = await GetAllAsync(true); // Include inactive for filtering

        // Aplicar filtro de texto
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var searchLower = searchText.ToLowerInvariant();
            families = families.Where(f =>
                f.Name.ToLowerInvariant().Contains(searchLower) ||
                (!string.IsNullOrEmpty(f.Description) && f.Description.ToLowerInvariant().Contains(searchLower))
            ).ToList();
        }

        // Aplicar filtro de status
        if (statusFilter.HasValue)
        {
            families = families.Where(f => f.IsActive == statusFilter.Value).ToList();
        }

        Debug.WriteLine($"🔍 [FAMILY_REPO] Filtered results: {families.Count} families");
        return families.OrderBy(f => f.Name).ToList();
    }

    /// <summary>
    /// Busca família por ID
    /// </summary>
    public async Task<Family?> GetByIdAsync(Guid id)
    {
        var families = await GetAllAsync(true);
        var family = families.FirstOrDefault(f => f.Id == id);

        if (family != null)
        {
            Debug.WriteLine($"✅ [FAMILY_REPO] Found family by ID: {family.Name}");
        }
        else
        {
            Debug.WriteLine($"❌ [FAMILY_REPO] Family not found by ID: {id}");
        }

        return family;
    }

    /// <summary>
    /// Busca família por nome
    /// </summary>
    public async Task<Family?> GetByNameAsync(string name)
    {
        var families = await GetAllAsync(true);
        var family = families.FirstOrDefault(f =>
            string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));

        if (family != null)
        {
            Debug.WriteLine($"✅ [FAMILY_REPO] Found family by name: {family.Name}");
        }

        return family;
    }

    /// <summary>
    /// Cria nova família
    /// </summary>
    public async Task<Family> CreateAsync(Family family)
    {
        var isConnected = await TestConnectionAsync();
        if (!isConnected)
        {
            throw new InvalidOperationException("Cannot create family - no internet connection available");
        }

        await _semaphore.WaitAsync();
        try
        {
            Debug.WriteLine($"➕ [FAMILY_REPO] Creating family: {family.Name}");

            var created = await _familyService.CreateAsync(family);

            // Invalidar cache para forçar refresh na próxima consulta
            InvalidateCache();

            Debug.WriteLine($"✅ [FAMILY_REPO] Created and cache invalidated: {created.Name}");
            return created;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Atualiza família existente
    /// </summary>
    public async Task<Family> UpdateAsync(Family family)
    {
        var isConnected = await TestConnectionAsync();
        if (!isConnected)
        {
            throw new InvalidOperationException("Cannot update family - no internet connection available");
        }

        await _semaphore.WaitAsync();
        try
        {
            Debug.WriteLine($"📝 [FAMILY_REPO] Updating family: {family.Name}");

            var updated = await _familyService.UpdateAsync(family);

            // Invalidar cache
            InvalidateCache();

            Debug.WriteLine($"✅ [FAMILY_REPO] Updated and cache invalidated: {updated.Name}");
            return updated;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Delete de família
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var isConnected = await TestConnectionAsync();
        if (!isConnected)
        {
            throw new InvalidOperationException("Cannot delete family - no internet connection available");
        }

        await _semaphore.WaitAsync();
        try
        {
            Debug.WriteLine($"🗑️ [FAMILY_REPO] Deleting family: {id}");

            var success = await _familyService.DeleteAsync(id);

            if (success)
            {
                Debug.WriteLine("🗑️ [FAMILY_REPO] Delete successful - invalidating cache");
                InvalidateCache();
                Debug.WriteLine($"✅ [FAMILY_REPO] Deleted and cache invalidated");
            }

            return success;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Delete múltiplo
    /// </summary>
    public async Task<int> DeleteMultipleAsync(IEnumerable<Guid> ids)
    {
        var isConnected = await TestConnectionAsync();
        if (!isConnected)
        {
            throw new InvalidOperationException("Cannot delete families - no internet connection available");
        }

        int count = 0;
        foreach (var id in ids)
        {
            if (await DeleteAsync(id))
            {
                count++;
            }
        }

        if (count > 0)
        {
            Debug.WriteLine($"🗑️ [FAMILY_REPO] Invalidating cache after deleting {count} families");
            InvalidateCache();
        }

        return count;
    }

    /// <summary>
    /// Testa conectividade
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            return await _familyService.TestConnectionAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_REPO] Connection test error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Refresh cache async
    /// </summary>
    public async Task RefreshCacheAsync()
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

    /// <summary>
    /// Refresh all data with operation result
    /// </summary>
    public async Task<OperationResult> RefreshAllDataAsync()
    {
        var startTime = DateTime.UtcNow;
        Debug.WriteLine("🔄 [FAMILY_REPO] Refreshing all data from server...");

        try
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
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_REPO] Refresh all data failed: {ex.Message}");

            return new OperationResult
            {
                StartTime = startTime,
                EndTime = DateTime.UtcNow,
                Duration = DateTime.UtcNow - startTime,
                TotalProcessed = 0,
                Successful = 0,
                Failed = 1,
                IsSuccess = false,
                ErrorMessages = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// Get cache information
    /// </summary>
    public string GetCacheInfo()
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
    }

    /// <summary>
    /// Invalidate cache externally
    /// </summary>
    public void InvalidateCacheExternal()
    {
        lock (_cacheLock)
        {
            _lastCacheUpdate = null;
            _cache.Clear();
            Debug.WriteLine("🗑️ [FAMILY_REPO] Cache invalidated externally");
        }

        _supabaseService.InvalidateConnectionCache();
    }

    /// <summary>
    /// Get statistics with base interface implementation
    /// </summary>
    async Task<BaseStatistics> IBaseRepository<Family>.GetStatisticsAsync()
    {
        var familyStats = await GetFamilyStatisticsAsync();

        // Converte FamilyStatistics para BaseStatistics
        return new BaseStatistics
        {
            TotalCount = familyStats.TotalCount,
            ActiveCount = familyStats.ActiveCount,
            InactiveCount = familyStats.InactiveCount,
            SystemDefaultCount = familyStats.SystemDefaultCount,
            UserCreatedCount = familyStats.UserCreatedCount,
            LastRefreshTime = familyStats.LastRefreshTime
        };
    }

    #endregion

    #region IFamilyRepository Specific Implementation

    /// <summary>
    /// Verifica se nome já existe
    /// </summary>
    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
    {
        try
        {
            var isConnected = await TestConnectionAsync();
            if (isConnected)
            {
                return await _familyService.NameExistsAsync(name, excludeId);
            }
            else
            {
                Debug.WriteLine("📡 [FAMILY_REPO] Offline - checking name in cache");
                var cachedFamilies = GetFromCache(true);

                var exists = cachedFamilies.Any(f =>
                    string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase) &&
                    f.Id != excludeId);

                Debug.WriteLine($"💾 [FAMILY_REPO] Cache name check for '{name}': {exists}");
                return exists;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_REPO] NameExists error: {ex.Message}");

            var cachedFamilies = GetFromCache(true);
            return cachedFamilies.Any(f =>
                string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase) &&
                f.Id != excludeId);
        }
    }

    /// <summary>
    /// Toggle favorite status
    /// </summary>
    public async Task<Family> ToggleFavoriteAsync(Guid familyId)
    {
        var isConnected = await TestConnectionAsync();
        if (!isConnected)
        {
            throw new InvalidOperationException("Cannot toggle favorite - no internet connection available");
        }

        await _semaphore.WaitAsync();
        try
        {
            Debug.WriteLine($"⭐ [FAMILY_REPO] Toggling favorite for family: {familyId}");

            var updatedFamily = await _familyService.ToggleFavoriteAsync(familyId);

            // Invalidar cache
            InvalidateCache();

            Debug.WriteLine($"✅ [FAMILY_REPO] Favorite toggled and cache invalidated: {updatedFamily.Name} -> {updatedFamily.IsFavorite}");
            return updatedFamily;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Get family statistics (specific method)
    /// </summary>
    public async Task<FamilyStatistics> GetFamilyStatisticsAsync()
    {
        try
        {
            var isConnected = await TestConnectionAsync();
            if (isConnected)
            {
                var serviceStats = await _familyService.GetStatisticsAsync();

                // Converte para FamilyStatistics
                return new FamilyStatistics
                {
                    TotalCount = serviceStats.TotalCount,
                    ActiveCount = serviceStats.ActiveCount,
                    InactiveCount = serviceStats.InactiveCount,
                    SystemDefaultCount = serviceStats.SystemDefaultCount,
                    UserCreatedCount = serviceStats.UserCreatedCount,
                    LastRefreshTime = serviceStats.LastRefreshTime
                };
            }
            else
            {
                Debug.WriteLine("📡 [FAMILY_REPO] Offline - calculating stats from cache");
                var cachedFamilies = GetFromCache(true);

                return new FamilyStatistics
                {
                    TotalCount = cachedFamilies.Count,
                    ActiveCount = cachedFamilies.Count(f => f.IsActive),
                    InactiveCount = cachedFamilies.Count(f => !f.IsActive),
                    SystemDefaultCount = cachedFamilies.Count(f => f.IsSystemDefault),
                    UserCreatedCount = cachedFamilies.Count(f => !f.IsSystemDefault),
                    LastRefreshTime = _lastCacheUpdate ?? DateTime.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_REPO] GetFamilyStatisticsAsync error: {ex.Message}");
            return new FamilyStatistics();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Verifica se cache é válido
    /// </summary>
    private bool IsCacheValid()
    {
        lock (_cacheLock)
        {
            return _lastCacheUpdate.HasValue &&
                   DateTime.UtcNow - _lastCacheUpdate.Value < _cacheValidTime &&
                   _cache.Any();
        }
    }

    /// <summary>
    /// Refresh interno do cache
    /// </summary>
    private async Task RefreshCacheInternalAsync()
    {
        try
        {
            var families = await _familyService.GetAllAsync();

            lock (_cacheLock)
            {
                _cache.Clear();
                _cache.AddRange(families);
                _lastCacheUpdate = DateTime.UtcNow;

                Debug.WriteLine($"💾 [FAMILY_REPO] Cache refreshed with {families.Count} families");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_REPO] Cache refresh error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Obtém dados do cache com filtros
    /// </summary>
    private List<Family> GetFromCache(bool includeInactive)
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
    }

    /// <summary>
    /// Invalida o cache
    /// </summary>
    private void InvalidateCache()
    {
        lock (_cacheLock)
        {
            _lastCacheUpdate = null;
            Debug.WriteLine("🗑️ [FAMILY_REPO] Cache invalidated");
        }

        _supabaseService.InvalidateConnectionCache();
    }

    #endregion
}