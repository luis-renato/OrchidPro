using OrchidPro.Models;
using OrchidPro.Services.Data;
using System.Diagnostics;

namespace OrchidPro.Services;

/// <summary>
/// MIGRADO: Repository simplificado com cache inteligente
/// Remove complexidade de sincronização, usa Supabase direto com cache para performance
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

        Debug.WriteLine("✅ [FAMILY_REPO] Initialized with simplified architecture and intelligent cache");
    }

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

            // Cache inválido - buscar do servidor
            Debug.WriteLine("🔄 [FAMILY_REPO] Cache expired - fetching from server");
            await RefreshCacheInternalAsync();

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
    public async Task<List<Family>> GetFilteredAsync(string? searchText = null, bool? statusFilter = null, SyncStatus? syncFilter = null)
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

        // SyncFilter ignorado na nova arquitetura (todos sempre synced)

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
    /// Soft delete de família
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        await _semaphore.WaitAsync();
        try
        {
            Debug.WriteLine($"🗑️ [FAMILY_REPO] Deleting family: {id}");

            var success = await _familyService.DeleteAsync(id);

            if (success)
            {
                // Invalidar cache
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
        int count = 0;
        foreach (var id in ids)
        {
            if (await DeleteAsync(id))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Verifica se nome existe
    /// </summary>
    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
    {
        return await _familyService.NameExistsAsync(name, excludeId);
    }

    /// <summary>
    /// Obtém estatísticas
    /// </summary>
    public async Task<FamilyStatistics> GetStatisticsAsync()
    {
        return await _familyService.GetStatisticsAsync();
    }

    /// <summary>
    /// Force refresh do cache
    /// </summary>
    public async Task<SyncResult> ForceFullSyncAsync()
    {
        var startTime = DateTime.UtcNow;
        Debug.WriteLine("🔄 [FAMILY_REPO] Force refresh cache from server...");

        try
        {
            await RefreshCacheAsync();

            var families = GetFromCache(true);
            var endTime = DateTime.UtcNow;

            return new SyncResult
            {
                StartTime = startTime,
                EndTime = endTime,
                Duration = endTime - startTime,
                TotalProcessed = families.Count,
                Successful = families.Count,
                Failed = 0
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_REPO] Force refresh failed: {ex.Message}");

            return new SyncResult
            {
                StartTime = startTime,
                EndTime = DateTime.UtcNow,
                Duration = DateTime.UtcNow - startTime,
                TotalProcessed = 0,
                Successful = 0,
                Failed = 1,
                ErrorMessages = { ex.Message }
            };
        }
    }

    /// <summary>
    /// Refresh manual do cache
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
    /// Testa conectividade
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        return await _familyService.TestConnectionAsync();
    }

    /// <summary>
    /// Obtém informações do cache
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
    /// Refresh interno do cache
    /// </summary>
    private async Task RefreshCacheInternalAsync()
    {
        try
        {
            Debug.WriteLine("📥 [FAMILY_REPO] Refreshing cache from server...");

            var families = await _familyService.GetAllAsync();

            lock (_cacheLock)
            {
                _cache.Clear();
                _cache.AddRange(families);
                _lastCacheUpdate = DateTime.UtcNow;
            }

            Debug.WriteLine($"💾 [FAMILY_REPO] Cache refreshed with {families.Count} families");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_REPO] Cache refresh failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Verifica se cache é válido
    /// </summary>
    private bool IsCacheValid()
    {
        lock (_cacheLock)
        {
            if (_lastCacheUpdate == null || !_cache.Any())
            {
                return false;
            }

            var age = DateTime.UtcNow - _lastCacheUpdate.Value;
            return age < _cacheValidTime;
        }
    }

    /// <summary>
    /// Obtém dados do cache com filtro
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
    }
}