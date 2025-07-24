using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Services.Data;
using System.Diagnostics;

namespace OrchidPro.Services;

/// <summary>
/// ✅ ATUALIZADO: FamilyRepository com método ToggleFavoriteAsync implementado
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

        Debug.WriteLine("✅ [FAMILY_REPO] Initialized with ToggleFavoriteAsync support");
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
        try
        {
            family.Id = Guid.NewGuid();
            family.CreatedAt = DateTime.UtcNow;
            family.UpdatedAt = DateTime.UtcNow;

            // ✅ CORREÇÃO: GetCurrentUserId() retorna string?, não Guid?
            var userIdString = _supabaseService.GetCurrentUserId();
            if (Guid.TryParse(userIdString, out Guid userId))
            {
                family.UserId = userId;
            }
            else
            {
                family.UserId = null; // System default se não conseguir parsear
            }

            Debug.WriteLine($"➕ [FAMILY_REPO] Creating family: {family.Name}");

            var result = await _familyService.CreateAsync(family);
            InvalidateCache();

            Debug.WriteLine($"✅ [FAMILY_REPO] Family created successfully: {result.Name}");
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_REPO] Create failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Atualiza família existente
    /// </summary>
    public async Task<Family> UpdateAsync(Family family)
    {
        try
        {
            family.UpdatedAt = DateTime.UtcNow;

            Debug.WriteLine($"📝 [FAMILY_REPO] Updating family: {family.Name} (Favorite: {family.IsFavorite})");

            var result = await _familyService.UpdateAsync(family);
            InvalidateCache();

            Debug.WriteLine($"✅ [FAMILY_REPO] Family updated successfully: {result.Name}");
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_REPO] Update failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Deleta família por ID
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            Debug.WriteLine($"🗑️ [FAMILY_REPO] Deleting family: {id}");

            var result = await _familyService.DeleteAsync(id);
            InvalidateCache();

            Debug.WriteLine($"✅ [FAMILY_REPO] Family deleted successfully");
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_REPO] Delete failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Deleta múltiplas famílias
    /// </summary>
    public async Task<int> DeleteMultipleAsync(IEnumerable<Guid> ids)
    {
        try
        {
            var idsArray = ids.ToArray();
            Debug.WriteLine($"🗑️ [FAMILY_REPO] Deleting {idsArray.Length} families");

            // ✅ CORREÇÃO: Implementar sem usar SupabaseFamilyService.DeleteMultipleAsync que não existe
            int deletedCount = 0;
            foreach (var id in idsArray)
            {
                try
                {
                    var deleted = await _familyService.DeleteAsync(id);
                    if (deleted) deletedCount++;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ [FAMILY_REPO] Failed to delete {id}: {ex.Message}");
                }
            }

            InvalidateCache();

            Debug.WriteLine($"✅ [FAMILY_REPO] {deletedCount} families deleted successfully");
            return deletedCount;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_REPO] Delete multiple failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Verifica se nome existe
    /// </summary>
    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
    {
        var families = await GetAllAsync(true);
        var exists = families.Any(f =>
            string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase) &&
            f.Id != excludeId);

        Debug.WriteLine($"🔍 [FAMILY_REPO] Name '{name}' exists: {exists}");
        return exists;
    }

    #endregion

    #region ✅ NOVO: IFamilyRepository Specific Methods

    /// <summary>
    /// ✅ NOVO: Toggle favorite status for a family
    /// </summary>
    public async Task<Family> ToggleFavoriteAsync(Guid familyId)
    {
        try
        {
            Debug.WriteLine($"⭐ [FAMILY_REPO] Toggling favorite for family: {familyId}");

            // Buscar família atual
            var family = await GetByIdAsync(familyId);
            if (family == null)
            {
                throw new ArgumentException($"Family with ID {familyId} not found");
            }

            // Toggle favorite
            var originalFavoriteStatus = family.IsFavorite;
            family.ToggleFavorite(); // Método que já existe no modelo Family

            Debug.WriteLine($"⭐ [FAMILY_REPO] Family '{family.Name}' favorite: {originalFavoriteStatus} → {family.IsFavorite}");

            // Salvar no banco
            var result = await UpdateAsync(family);

            Debug.WriteLine($"✅ [FAMILY_REPO] Favorite toggled successfully for: {family.Name}");
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_REPO] ToggleFavorite failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Obtém estatísticas das famílias
    /// </summary>
    public async Task<FamilyStatistics> GetFamilyStatisticsAsync()
    {
        try
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
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_REPO] GetFamilyStatisticsAsync error: {ex.Message}");
            return new FamilyStatistics();
        }
    }

    #endregion

    #region Connection and Maintenance

    /// <summary>
    /// Testa conectividade
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            // ✅ CORREÇÃO: Usar método correto do SupabaseService
            return await _supabaseService.TestSyncConnectionAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_REPO] Connection test failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// ✅ NOVO: Refresh all data with operation result
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
    /// ✅ NOVO: Get cache information
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
    /// ✅ NOVO: Invalidate cache externally
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
    /// Obtém estatísticas gerais (implementação da interface base)
    /// </summary>
    public async Task<BaseStatistics> GetStatisticsAsync()
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

    /// <summary>
    /// Força refresh do cache
    /// </summary>
    public async Task RefreshCacheAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            Debug.WriteLine("🔄 [FAMILY_REPO] Force cache refresh requested");
            await RefreshCacheInternalAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Obtém status do cache
    /// </summary>
    public (bool IsValid, DateTime? LastUpdate, int ItemCount) GetCacheStatus()
    {
        lock (_cacheLock)
        {
            return (IsCacheValid(), _lastCacheUpdate, _cache.Count);
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