using OrchidPro.Models;
using OrchidPro.Services.Data;
using System.Security.Cryptography;
using System.Text;

namespace OrchidPro.Services;

/// <summary>
/// Repository implementation for Family entities with local storage and REAL Supabase sync
/// </summary>
public class FamilyRepository : IFamilyRepository
{
    private readonly SupabaseService _supabaseService;
    private readonly ILocalDataService _localDataService;
    private readonly SupabaseFamilySync _syncService;
    private readonly List<Family> _localFamilies = new();
    private Guid? _currentUserId;

    public FamilyRepository(SupabaseService supabaseService, ILocalDataService localDataService)
    {
        _supabaseService = supabaseService;
        _localDataService = localDataService;
        _syncService = new SupabaseFamilySync(supabaseService);
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🔄 Initializing FamilyRepository...");

            // Get current user ID
            if (_supabaseService.Client?.Auth.CurrentUser?.Id != null)
            {
                if (Guid.TryParse(_supabaseService.Client.Auth.CurrentUser.Id, out var userId))
                {
                    _currentUserId = userId;
                    System.Diagnostics.Debug.WriteLine($"✅ Current user ID: {_currentUserId}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ No authenticated user found");
            }

            // Load local data first
            await LoadLocalDataAsync();

            // Test Supabase connection
            var isConnected = await _syncService.TestConnectionAsync();
            System.Diagnostics.Debug.WriteLine($"🌐 Supabase connection: {(isConnected ? "✅ Connected" : "❌ Failed")}");

            if (isConnected)
            {
                // Try to sync with server
                await PerformInitialSyncAsync();
            }
            else
            {
                // Fallback to minimal local setup
                if (!_localFamilies.Any())
                {
                    await SeedMinimalDefaultFamiliesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ FamilyRepository initialization error: {ex.Message}");

            // Fallback to local-only mode
            if (!_localFamilies.Any())
            {
                await SeedMinimalDefaultFamiliesAsync();
            }
        }
    }

    /// <summary>
    /// 🔄 Primeira sincronização - baixa dados do servidor e mescla com local
    /// </summary>
    private async Task PerformInitialSyncAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("📥 Performing initial sync...");

            // Baixar famílias do servidor
            var serverFamilies = await _syncService.DownloadFamiliesAsync();

            if (serverFamilies.Any())
            {
                System.Diagnostics.Debug.WriteLine($"📥 Downloaded {serverFamilies.Count} families from server");

                // Mesclar com dados locais
                foreach (var serverFamily in serverFamilies)
                {
                    var localFamily = _localFamilies.FirstOrDefault(f => f.Id == serverFamily.Id);

                    if (localFamily == null)
                    {
                        // Nova família do servidor
                        _localFamilies.Add(serverFamily);
                        await _localDataService.SaveFamilyAsync(serverFamily);
                        System.Diagnostics.Debug.WriteLine($"📥 Added from server: {serverFamily.Name}");
                    }
                    else
                    {
                        // Verificar qual é mais recente
                        if (serverFamily.UpdatedAt > localFamily.UpdatedAt)
                        {
                            // Servidor é mais recente
                            var index = _localFamilies.IndexOf(localFamily);
                            _localFamilies[index] = serverFamily;
                            await _localDataService.SaveFamilyAsync(serverFamily);
                            System.Diagnostics.Debug.WriteLine($"📥 Updated from server: {serverFamily.Name}");
                        }
                    }
                }

                // Enviar famílias locais pendentes
                var pendingFamilies = _localFamilies.Where(f =>
                    f.SyncStatus == SyncStatus.Local ||
                    f.SyncStatus == SyncStatus.Pending
                ).ToList();

                foreach (var pendingFamily in pendingFamilies)
                {
                    var success = await _syncService.UploadFamilyAsync(pendingFamily);
                    if (success)
                    {
                        pendingFamily.SyncStatus = SyncStatus.Synced;
                        pendingFamily.LastSyncAt = DateTime.UtcNow;
                        await _localDataService.SaveFamilyAsync(pendingFamily);
                        System.Diagnostics.Debug.WriteLine($"📤 Uploaded to server: {pendingFamily.Name}");
                    }
                }
            }
            else
            {
                // Servidor vazio, criar Orchidaceae default se não existir localmente
                if (!_localFamilies.Any(f => f.Name == "Orchidaceae"))
                {
                    await SeedMinimalDefaultFamiliesAsync();

                    // Tentar enviar para o servidor
                    var orchidFamily = _localFamilies.First(f => f.Name == "Orchidaceae");
                    var success = await _syncService.UploadFamilyAsync(orchidFamily);
                    if (success)
                    {
                        orchidFamily.SyncStatus = SyncStatus.Synced;
                        orchidFamily.LastSyncAt = DateTime.UtcNow;
                        await _localDataService.SaveFamilyAsync(orchidFamily);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Initial sync failed: {ex.Message}");
        }
    }

    private async Task LoadLocalDataAsync()
    {
        try
        {
            var localData = await _localDataService.GetAllFamiliesAsync();
            _localFamilies.Clear();
            _localFamilies.AddRange(localData);
            System.Diagnostics.Debug.WriteLine($"💾 Loaded {localData.Count} families from local storage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading local families: {ex.Message}");
        }
    }

    private async Task SeedMinimalDefaultFamiliesAsync()
    {
        // APENAS Orchidaceae como padrão - afinal é um app de orquídeas! 🌺
        var orchidFamily = new Family
        {
            Name = "Orchidaceae",
            Description = "The orchid family - largest family of flowering plants with over 25,000 species worldwide. Known for their complex flowers and diverse growth habits.",
            IsSystemDefault = true,
            IsActive = true,
            SyncStatus = SyncStatus.Local, // Será sincronizado depois
            UserId = null, // System default
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        orchidFamily.SyncHash = GenerateHash(orchidFamily);
        _localFamilies.Add(orchidFamily);
        await _localDataService.SaveFamilyAsync(orchidFamily);

        System.Diagnostics.Debug.WriteLine("🌺 Seeded default family: Orchidaceae");
    }

    public async Task<List<Family>> GetAllAsync(bool includeInactive = false)
    {
        await LoadLocalDataAsync();

        var families = _localFamilies.Where(f =>
            (f.UserId == _currentUserId || f.UserId == null) &&
            (includeInactive || f.IsActive)
        ).ToList();

        return families.OrderBy(f => f.Name).ToList();
    }

    public async Task<List<Family>> GetFilteredAsync(string? searchText = null, bool? statusFilter = null, SyncStatus? syncFilter = null)
    {
        var families = await GetAllAsync(true);

        // Apply text search
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            searchText = searchText.ToLowerInvariant();
            families = families.Where(f =>
                f.Name.ToLowerInvariant().Contains(searchText) ||
                (!string.IsNullOrEmpty(f.Description) && f.Description.ToLowerInvariant().Contains(searchText))
            ).ToList();
        }

        // Apply status filter
        if (statusFilter.HasValue)
        {
            families = families.Where(f => f.IsActive == statusFilter.Value).ToList();
        }

        // Apply sync filter
        if (syncFilter.HasValue)
        {
            families = families.Where(f => f.SyncStatus == syncFilter.Value).ToList();
        }

        return families.OrderBy(f => f.Name).ToList();
    }

    public async Task<Family?> GetByIdAsync(Guid id)
    {
        await LoadLocalDataAsync();
        return _localFamilies.FirstOrDefault(f => f.Id == id);
    }

    public async Task<Family?> GetByNameAsync(string name)
    {
        await LoadLocalDataAsync();
        return _localFamilies.FirstOrDefault(f =>
            string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase) &&
            (f.UserId == _currentUserId || f.UserId == null)
        );
    }

    public async Task<Family> CreateAsync(Family family)
    {
        family.Id = Guid.NewGuid();
        family.UserId = _currentUserId;
        family.CreatedAt = DateTime.UtcNow;
        family.UpdatedAt = DateTime.UtcNow;
        family.SyncStatus = SyncStatus.Local; // Marca como local até sincronizar
        family.SyncHash = GenerateHash(family);

        _localFamilies.Add(family);
        await _localDataService.SaveFamilyAsync(family);

        // 🔄 Tentar sincronizar automaticamente em background
        _ = Task.Run(async () => await TryAutoSyncFamilyAsync(family));

        System.Diagnostics.Debug.WriteLine($"✅ Created family: {family.Name} (Status: {family.SyncStatus})");
        return family;
    }

    public async Task<Family> UpdateAsync(Family family)
    {
        var existingIndex = _localFamilies.FindIndex(f => f.Id == family.Id);
        if (existingIndex == -1)
        {
            throw new InvalidOperationException("Family not found");
        }

        family.UpdatedAt = DateTime.UtcNow;

        // Se estava sincronizado, marca como pendente
        if (family.SyncStatus == SyncStatus.Synced)
        {
            family.SyncStatus = SyncStatus.Pending;
        }

        family.SyncHash = GenerateHash(family);

        _localFamilies[existingIndex] = family;
        await _localDataService.SaveFamilyAsync(family);

        // 🔄 Tentar sincronizar automaticamente em background
        _ = Task.Run(async () => await TryAutoSyncFamilyAsync(family));

        System.Diagnostics.Debug.WriteLine($"✅ Updated family: {family.Name} (Status: {family.SyncStatus})");
        return family;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var family = await GetByIdAsync(id);
        if (family == null || family.IsSystemDefault)
        {
            return false;
        }

        family.IsActive = false;
        family.UpdatedAt = DateTime.UtcNow;
        family.SyncStatus = SyncStatus.Pending;

        await _localDataService.SaveFamilyAsync(family);

        // 🔄 Tentar sincronizar automaticamente em background
        _ = Task.Run(async () => await TryAutoSyncFamilyAsync(family));

        System.Diagnostics.Debug.WriteLine($"✅ Soft deleted family: {family.Name}");
        return true;
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
    /// 🔄 NOVA: Sincronização automática em background
    /// </summary>
    private async Task TryAutoSyncFamilyAsync(Family family)
    {
        try
        {
            var success = await _syncService.UploadFamilyAsync(family);

            if (success)
            {
                family.SyncStatus = SyncStatus.Synced;
                family.LastSyncAt = DateTime.UtcNow;
                await _localDataService.SaveFamilyAsync(family);
                System.Diagnostics.Debug.WriteLine($"🔄 Auto-synced family: {family.Name}");
            }
            else
            {
                family.SyncStatus = SyncStatus.Error;
                await _localDataService.SaveFamilyAsync(family);
                System.Diagnostics.Debug.WriteLine($"❌ Auto-sync failed for: {family.Name}");
            }
        }
        catch (Exception ex)
        {
            family.SyncStatus = SyncStatus.Error;
            await _localDataService.SaveFamilyAsync(family);
            System.Diagnostics.Debug.WriteLine($"❌ Auto-sync error for {family.Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// 🔄 NOVA: Força sincronização manual de todas as famílias pendentes
    /// </summary>
    public async Task<SyncResult> ForceFullSyncAsync()
    {
        System.Diagnostics.Debug.WriteLine("🔄 Starting manual full sync...");

        var result = await _syncService.PerformFullSyncAsync(_localFamilies);

        // Atualizar status local das famílias sincronizadas
        foreach (var family in _localFamilies.Where(f => f.SyncStatus == SyncStatus.Pending || f.SyncStatus == SyncStatus.Local))
        {
            family.SyncStatus = SyncStatus.Synced;
            family.LastSyncAt = DateTime.UtcNow;
            await _localDataService.SaveFamilyAsync(family);
        }

        System.Diagnostics.Debug.WriteLine($"🔄 Manual sync completed: {result.Successful}/{result.TotalProcessed}");
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