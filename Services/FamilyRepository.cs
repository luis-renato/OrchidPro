using OrchidPro.Models;
using OrchidPro.Services.Data;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;

namespace OrchidPro.Services;

/// <summary>
/// CORRIGIDO: Repository com controle de duplicatas e sync melhorado
/// Versão sem await dentro de lock
/// </summary>
public class FamilyRepository : IFamilyRepository
{
    private readonly SupabaseService _supabaseService;
    private readonly ILocalDataService _localDataService;
    private readonly SupabaseFamilySync _syncService;
    private readonly List<Family> _localFamilies = new();
    private Guid? _currentUserId;
    private readonly object _localLock = new object();
    private readonly HashSet<Guid> _pendingAutoSync = new(); // Previne auto-sync duplicado
    private readonly SemaphoreSlim _semaphore = new(1, 1); // Para operações async thread-safe

    public FamilyRepository(SupabaseService supabaseService, ILocalDataService localDataService, SupabaseFamilySync syncService)
    {
        _supabaseService = supabaseService;
        _localDataService = localDataService;
        _syncService = syncService;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            Debug.WriteLine("🔄 [REPO] Initializing FamilyRepository...");

            // Get current user ID
            var currentUserIdString = _supabaseService.GetCurrentUserId();
            if (!string.IsNullOrEmpty(currentUserIdString) && Guid.TryParse(currentUserIdString, out var userId))
            {
                _currentUserId = userId;
                Debug.WriteLine($"✅ [REPO] Current user ID: {_currentUserId}");
            }

            // Load local data
            await LoadLocalDataAsync();

            // Test connection and try sync if possible
            var canSync = await _syncService.TestConnectionAsync();
            Debug.WriteLine($"🌐 [REPO] Sync available: {canSync}");

            if (canSync && _supabaseService.IsAuthenticated)
            {
                await PerformInitialSyncAsync();
            }
            else
            {
                bool hasOrchidaceae;
                lock (_localLock)
                {
                    hasOrchidaceae = _localFamilies.Any(f => f.Name == "Orchidaceae");
                }

                if (!hasOrchidaceae)
                {
                    await SeedMinimalDefaultFamiliesAsync();
                }
            }

            Debug.WriteLine("✅ [REPO] FamilyRepository initialized");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [REPO] FamilyRepository init error: {ex.Message}");

            // Fallback - ensure we have at least Orchidaceae
            bool hasOrchidaceae;
            lock (_localLock)
            {
                hasOrchidaceae = _localFamilies.Any();
            }

            if (!hasOrchidaceae)
            {
                await SeedMinimalDefaultFamiliesAsync();
            }
        }
    }

    private async Task PerformInitialSyncAsync()
    {
        try
        {
            Debug.WriteLine("📥 [REPO] Initial sync...");

            var serverFamilies = await _syncService.DownloadFamiliesAsync();

            if (serverFamilies.Any())
            {
                Debug.WriteLine($"📥 [REPO] Downloaded {serverFamilies.Count} families from server");

                // Process merges without blocking the lock for async operations
                var mergeOperations = new List<Task>();

                foreach (var serverFamily in serverFamilies)
                {
                    Family? localFamily;
                    Family? localByName;

                    lock (_localLock)
                    {
                        localFamily = _localFamilies.FirstOrDefault(f => f.Id == serverFamily.Id);
                        localByName = localFamily == null ? _localFamilies.FirstOrDefault(f =>
                            string.Equals(f.Name, serverFamily.Name, StringComparison.OrdinalIgnoreCase) &&
                            f.UserId == serverFamily.UserId) : null;
                    }

                    if (localFamily == null)
                    {
                        if (localByName != null)
                        {
                            // Merge by name - replace local with server version
                            Debug.WriteLine($"📥 [REPO] Merging by name: {serverFamily.Name} (Local ID: {localByName.Id} → Server ID: {serverFamily.Id})");

                            lock (_localLock)
                            {
                                _localFamilies.Remove(localByName);
                                serverFamily.SyncStatus = SyncStatus.Synced;
                                serverFamily.LastSyncAt = DateTime.UtcNow;
                                _localFamilies.Add(serverFamily);
                            }

                            mergeOperations.Add(_localDataService.SaveFamilyAsync(serverFamily));
                        }
                        else
                        {
                            // New family from server
                            lock (_localLock)
                            {
                                serverFamily.SyncStatus = SyncStatus.Synced;
                                serverFamily.LastSyncAt = DateTime.UtcNow;
                                _localFamilies.Add(serverFamily);
                            }

                            mergeOperations.Add(_localDataService.SaveFamilyAsync(serverFamily));
                            Debug.WriteLine($"📥 [REPO] Added from server: {serverFamily.Name}");
                        }
                    }
                    else if (serverFamily.UpdatedAt > localFamily.UpdatedAt)
                    {
                        // Update with more recent server data
                        lock (_localLock)
                        {
                            var index = _localFamilies.IndexOf(localFamily);
                            serverFamily.SyncStatus = SyncStatus.Synced;
                            serverFamily.LastSyncAt = DateTime.UtcNow;
                            _localFamilies[index] = serverFamily;
                        }

                        mergeOperations.Add(_localDataService.SaveFamilyAsync(serverFamily));
                        Debug.WriteLine($"📥 [REPO] Updated from server: {serverFamily.Name}");
                    }
                    else
                    {
                        // Local is equal or more recent - mark as synced
                        lock (_localLock)
                        {
                            localFamily.SyncStatus = SyncStatus.Synced;
                            localFamily.LastSyncAt = DateTime.UtcNow;
                        }

                        mergeOperations.Add(_localDataService.SaveFamilyAsync(localFamily));
                    }
                }

                // Wait for all merge operations to complete
                await Task.WhenAll(mergeOperations);

                // Upload pending local families
                List<Family> pendingFamilies;
                lock (_localLock)
                {
                    pendingFamilies = _localFamilies.Where(f =>
                        f.SyncStatus == SyncStatus.Local || f.SyncStatus == SyncStatus.Pending
                    ).ToList();
                }

                Debug.WriteLine($"📤 [REPO] Found {pendingFamilies.Count} local families to upload");

                foreach (var pendingFamily in pendingFamilies)
                {
                    var success = await _syncService.UploadFamilyAsync(pendingFamily);

                    lock (_localLock)
                    {
                        if (success)
                        {
                            pendingFamily.SyncStatus = SyncStatus.Synced;
                            pendingFamily.LastSyncAt = DateTime.UtcNow;
                            Debug.WriteLine($"📤 [REPO] Uploaded: {pendingFamily.Name}");
                        }
                        else
                        {
                            pendingFamily.SyncStatus = SyncStatus.Error;
                            Debug.WriteLine($"❌ [REPO] Upload failed: {pendingFamily.Name}");
                        }
                    }

                    await _localDataService.SaveFamilyAsync(pendingFamily);

                    // Delay para evitar rate limiting
                    await Task.Delay(100);
                }
            }
            else
            {
                bool hasOrchidaceae;
                lock (_localLock)
                {
                    hasOrchidaceae = _localFamilies.Any(f => f.Name == "Orchidaceae");
                }

                if (!hasOrchidaceae)
                {
                    await SeedMinimalDefaultFamiliesAsync();

                    Family? orchidFamily;
                    lock (_localLock)
                    {
                        orchidFamily = _localFamilies.FirstOrDefault(f => f.Name == "Orchidaceae");
                    }

                    if (orchidFamily != null)
                    {
                        await TryAutoSyncFamilyAsync(orchidFamily);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [REPO] Initial sync failed: {ex.Message}");
        }
    }

    private async Task LoadLocalDataAsync()
    {
        try
        {
            var localData = await _localDataService.GetAllFamiliesAsync();

            lock (_localLock)
            {
                _localFamilies.Clear();
                _localFamilies.AddRange(localData);
            }

            Debug.WriteLine($"💾 [REPO] Loaded {localData.Count} local families");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [REPO] Error loading local families: {ex.Message}");
        }
    }

    private async Task SeedMinimalDefaultFamiliesAsync()
    {
        var orchidFamily = new Family
        {
            Name = "Orchidaceae",
            Description = "The orchid family - largest family of flowering plants with over 25,000 species worldwide.",
            IsSystemDefault = true,
            IsActive = true,
            SyncStatus = SyncStatus.Local,
            UserId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        orchidFamily.SyncHash = GenerateHash(orchidFamily);

        lock (_localLock)
        {
            _localFamilies.Add(orchidFamily);
        }

        await _localDataService.SaveFamilyAsync(orchidFamily);
        Debug.WriteLine("🌺 [REPO] Seeded Orchidaceae");
    }

    // MÉTODOS PRINCIPAIS
    public async Task<List<Family>> GetAllAsync(bool includeInactive = false)
    {
        await LoadLocalDataAsync();

        lock (_localLock)
        {
            var families = _localFamilies.Where(f =>
                (f.UserId == _currentUserId || f.UserId == null) &&
                (includeInactive || f.IsActive)
            ).ToList();

            return families.OrderBy(f => f.Name).ToList();
        }
    }

    public async Task<List<Family>> GetFilteredAsync(string? searchText = null, bool? statusFilter = null, SyncStatus? syncFilter = null)
    {
        var families = await GetAllAsync(true);

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            searchText = searchText.ToLowerInvariant();
            families = families.Where(f =>
                f.Name.ToLowerInvariant().Contains(searchText) ||
                (!string.IsNullOrEmpty(f.Description) && f.Description.ToLowerInvariant().Contains(searchText))
            ).ToList();
        }

        if (statusFilter.HasValue)
        {
            families = families.Where(f => f.IsActive == statusFilter.Value).ToList();
        }

        if (syncFilter.HasValue)
        {
            families = families.Where(f => f.SyncStatus == syncFilter.Value).ToList();
        }

        return families.OrderBy(f => f.Name).ToList();
    }

    public async Task<Family?> GetByIdAsync(Guid id)
    {
        await LoadLocalDataAsync();

        lock (_localLock)
        {
            return _localFamilies.FirstOrDefault(f => f.Id == id);
        }
    }

    public async Task<Family?> GetByNameAsync(string name)
    {
        await LoadLocalDataAsync();

        lock (_localLock)
        {
            return _localFamilies.FirstOrDefault(f =>
                string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase) &&
                (f.UserId == _currentUserId || f.UserId == null)
            );
        }
    }

    public async Task<Family> CreateAsync(Family family)
    {
        await _semaphore.WaitAsync();
        try
        {
            // Verificar se nome já existe
            var existing = await GetByNameAsync(family.Name);
            if (existing != null)
            {
                throw new InvalidOperationException($"A family with the name '{family.Name}' already exists");
            }

            // Configurar nova família
            family.Id = Guid.NewGuid();
            family.UserId = _currentUserId;
            family.CreatedAt = DateTime.UtcNow;
            family.UpdatedAt = DateTime.UtcNow;
            family.SyncStatus = SyncStatus.Local;
            family.SyncHash = GenerateHash(family);

            lock (_localLock)
            {
                _localFamilies.Add(family);
            }

            await _localDataService.SaveFamilyAsync(family);

            // Auto-sync em background (com controle de duplicação)
            _ = Task.Run(async () => await TryAutoSyncFamilyAsync(family));

            Debug.WriteLine($"✅ [REPO] Created family: {family.Name} (ID: {family.Id})");
            return family;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Family> UpdateAsync(Family family)
    {
        await _semaphore.WaitAsync();
        try
        {
            Family existingFamily;

            lock (_localLock)
            {
                var existingIndex = _localFamilies.FindIndex(f => f.Id == family.Id);
                if (existingIndex == -1)
                {
                    throw new InvalidOperationException("Family not found");
                }
                existingFamily = _localFamilies[existingIndex];
            }

            // Verificar se nome já existe em outra família
            var nameConflict = await GetByNameAsync(family.Name);
            if (nameConflict != null && nameConflict.Id != family.Id)
            {
                throw new InvalidOperationException($"A family with the name '{family.Name}' already exists");
            }

            family.UpdatedAt = DateTime.UtcNow;
            if (family.SyncStatus == SyncStatus.Synced)
            {
                family.SyncStatus = SyncStatus.Pending;
            }
            family.SyncHash = GenerateHash(family);

            lock (_localLock)
            {
                var existingIndex = _localFamilies.FindIndex(f => f.Id == family.Id);
                _localFamilies[existingIndex] = family;
            }

            await _localDataService.SaveFamilyAsync(family);

            // Auto-sync em background
            _ = Task.Run(async () => await TryAutoSyncFamilyAsync(family));

            Debug.WriteLine($"✅ [REPO] Updated family: {family.Name}");
            return family;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await _semaphore.WaitAsync();
        try
        {
            Family? family;

            lock (_localLock)
            {
                family = _localFamilies.FirstOrDefault(f => f.Id == id);
            }

            if (family == null || family.IsSystemDefault)
            {
                return false;
            }

            family.IsActive = false;
            family.UpdatedAt = DateTime.UtcNow;
            family.SyncStatus = SyncStatus.Pending;

            await _localDataService.SaveFamilyAsync(family);

            // Auto-sync em background
            _ = Task.Run(async () => await TryAutoSyncFamilyAsync(family));

            Debug.WriteLine($"✅ [REPO] Soft deleted family: {family.Name}");
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

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

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
    {
        var families = await GetAllAsync(true);
        return families.Any(f =>
            string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase) &&
            f.Id != excludeId
        );
    }

    public async Task<FamilyStatistics> GetStatisticsAsync()
    {
        var families = await GetAllAsync(true);

        return new FamilyStatistics
        {
            TotalCount = families.Count,
            ActiveCount = families.Count(f => f.IsActive),
            InactiveCount = families.Count(f => !f.IsActive),
            SyncedCount = families.Count(f => f.SyncStatus == SyncStatus.Synced),
            LocalCount = families.Count(f => f.SyncStatus == SyncStatus.Local),
            PendingCount = families.Count(f => f.SyncStatus == SyncStatus.Pending),
            ErrorCount = families.Count(f => f.SyncStatus == SyncStatus.Error),
            SystemDefaultCount = families.Count(f => f.IsSystemDefault),
            UserCreatedCount = families.Count(f => !f.IsSystemDefault),
            LastSyncTime = families.Where(f => f.LastSyncAt.HasValue).Max(f => f.LastSyncAt) ?? DateTime.MinValue
        };
    }

    /// <summary>
    /// CORRIGIDO: Auto-sync com controle de duplicação
    /// </summary>
    private async Task TryAutoSyncFamilyAsync(Family family)
    {
        // Prevenir sync simultâneo da mesma família
        lock (_pendingAutoSync)
        {
            if (_pendingAutoSync.Contains(family.Id))
            {
                Debug.WriteLine($"⏸️ [REPO] Auto-sync already in progress for: {family.Name}");
                return;
            }
            _pendingAutoSync.Add(family.Id);
        }

        try
        {
            if (!_supabaseService.IsAuthenticated)
            {
                Debug.WriteLine($"⏸️ [REPO] Auto-sync skipped (not authenticated): {family.Name}");
                return;
            }

            // Delay inicial para evitar sync imediato
            await Task.Delay(2000);

            Debug.WriteLine($"🔄 [REPO] Auto-syncing: {family.Name}");

            var success = await _syncService.UploadFamilyAsync(family);

            Family? localFamily;
            lock (_localLock)
            {
                localFamily = _localFamilies.FirstOrDefault(f => f.Id == family.Id);
            }

            if (localFamily != null)
            {
                if (success)
                {
                    lock (_localLock)
                    {
                        localFamily.SyncStatus = SyncStatus.Synced;
                        localFamily.LastSyncAt = DateTime.UtcNow;
                    }
                    Debug.WriteLine($"✅ [REPO] Auto-synced: {family.Name}");
                }
                else
                {
                    lock (_localLock)
                    {
                        localFamily.SyncStatus = SyncStatus.Error;
                    }
                    Debug.WriteLine($"❌ [REPO] Auto-sync failed: {family.Name}");
                }

                await _localDataService.SaveFamilyAsync(localFamily);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [REPO] Auto-sync error for {family.Name}: {ex.Message}");

            Family? localFamily;
            lock (_localLock)
            {
                localFamily = _localFamilies.FirstOrDefault(f => f.Id == family.Id);
            }

            if (localFamily != null)
            {
                lock (_localLock)
                {
                    localFamily.SyncStatus = SyncStatus.Error;
                }
                await _localDataService.SaveFamilyAsync(localFamily);
            }
        }
        finally
        {
            lock (_pendingAutoSync)
            {
                _pendingAutoSync.Remove(family.Id);
            }
        }
    }

    public async Task<SyncResult> ForceFullSyncAsync()
    {
        Debug.WriteLine("🔄 [REPO] Manual full sync started...");

        if (!_supabaseService.IsAuthenticated)
        {
            return new SyncResult
            {
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                ErrorMessages = { "User not authenticated" }
            };
        }

        List<Family> localFamiliesCopy;
        lock (_localLock)
        {
            localFamiliesCopy = _localFamilies.ToList();
        }

        var result = await _syncService.PerformFullSyncAsync(localFamiliesCopy);

        // Update local status based on sync results
        if (result.Successful > 0)
        {
            await LoadLocalDataAsync(); // Reload to get updated data

            List<Family> familiesToUpdate;
            lock (_localLock)
            {
                familiesToUpdate = _localFamilies.Where(f =>
                    f.SyncStatus == SyncStatus.Pending || f.SyncStatus == SyncStatus.Local).ToList();
            }

            var updateTasks = new List<Task>();
            foreach (var family in familiesToUpdate)
            {
                lock (_localLock)
                {
                    family.SyncStatus = SyncStatus.Synced;
                    family.LastSyncAt = DateTime.UtcNow;
                }
                updateTasks.Add(_localDataService.SaveFamilyAsync(family));
            }

            await Task.WhenAll(updateTasks);
        }

        Debug.WriteLine($"✅ [REPO] Manual sync completed: {result.Successful}/{result.TotalProcessed} successful");
        return result;
    }

    private static string GenerateHash(Family family)
    {
        var data = $"{family.Name}|{family.Description}|{family.IsActive}|{family.UpdatedAt:O}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}