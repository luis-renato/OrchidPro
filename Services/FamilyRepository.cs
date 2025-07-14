using OrchidPro.Models;
using OrchidPro.Services.Data;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;

namespace OrchidPro.Services;

/// <summary>
/// Repository - ALTERAÇÕES MÍNIMAS para corrigir apenas sync
/// </summary>
public class FamilyRepository : IFamilyRepository
{
    private readonly SupabaseService _supabaseService;
    private readonly ILocalDataService _localDataService;
    private readonly SupabaseFamilySync _syncService;
    private readonly List<Family> _localFamilies = new();
    private Guid? _currentUserId;

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
            Debug.WriteLine("🔄 Initializing FamilyRepository...");

            // Get current user ID
            var currentUserIdString = _supabaseService.GetCurrentUserId();
            if (!string.IsNullOrEmpty(currentUserIdString) && Guid.TryParse(currentUserIdString, out var userId))
            {
                _currentUserId = userId;
                Debug.WriteLine($"✅ Current user ID: {_currentUserId}");
            }

            // Load local data
            await LoadLocalDataAsync();

            // Test connection and try sync if possible
            var canSync = await _syncService.TestConnectionAsync();
            Debug.WriteLine($"🌐 Sync available: {canSync}");

            if (canSync && _supabaseService.IsAuthenticated)
            {
                await PerformInitialSyncAsync();
            }
            else if (!_localFamilies.Any())
            {
                // Seed defaults only if no local data
                await SeedMinimalDefaultFamiliesAsync();
            }

            Debug.WriteLine("✅ FamilyRepository initialized");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ FamilyRepository init error: {ex.Message}");
            // Fallback - ensure we have at least Orchidaceae
            if (!_localFamilies.Any())
            {
                await SeedMinimalDefaultFamiliesAsync();
            }
        }
    }

    private async Task PerformInitialSyncAsync()
    {
        try
        {
            Debug.WriteLine("📥 Initial sync...");

            var serverFamilies = await _syncService.DownloadFamiliesAsync();

            if (serverFamilies.Any())
            {
                foreach (var serverFamily in serverFamilies)
                {
                    var localFamily = _localFamilies.FirstOrDefault(f => f.Id == serverFamily.Id);

                    if (localFamily == null)
                    {
                        _localFamilies.Add(serverFamily);
                        await _localDataService.SaveFamilyAsync(serverFamily);
                        Debug.WriteLine($"📥 Added: {serverFamily.Name}");
                    }
                    else if (serverFamily.UpdatedAt > localFamily.UpdatedAt)
                    {
                        var index = _localFamilies.IndexOf(localFamily);
                        _localFamilies[index] = serverFamily;
                        await _localDataService.SaveFamilyAsync(serverFamily);
                        Debug.WriteLine($"📥 Updated: {serverFamily.Name}");
                    }
                }

                // Upload pending local
                var pendingFamilies = _localFamilies.Where(f =>
                    f.SyncStatus == SyncStatus.Local || f.SyncStatus == SyncStatus.Pending
                ).ToList();

                foreach (var pendingFamily in pendingFamilies)
                {
                    var success = await _syncService.UploadFamilyAsync(pendingFamily);
                    if (success)
                    {
                        pendingFamily.SyncStatus = SyncStatus.Synced;
                        pendingFamily.LastSyncAt = DateTime.UtcNow;
                        await _localDataService.SaveFamilyAsync(pendingFamily);
                    }
                }
            }
            else if (!_localFamilies.Any(f => f.Name == "Orchidaceae"))
            {
                await SeedMinimalDefaultFamiliesAsync();
                var orchidFamily = _localFamilies.FirstOrDefault(f => f.Name == "Orchidaceae");
                if (orchidFamily != null)
                {
                    await _syncService.UploadFamilyAsync(orchidFamily);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Initial sync failed: {ex.Message}");
        }
    }

    private async Task LoadLocalDataAsync()
    {
        try
        {
            var localData = await _localDataService.GetAllFamiliesAsync();
            _localFamilies.Clear();
            _localFamilies.AddRange(localData);
            Debug.WriteLine($"💾 Loaded {localData.Count} local families");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Error loading local families: {ex.Message}");
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
        _localFamilies.Add(orchidFamily);
        await _localDataService.SaveFamilyAsync(orchidFamily);

        Debug.WriteLine("🌺 Seeded Orchidaceae");
    }

    // MÉTODOS PRINCIPAIS - mantidos iguais ao original mas com sync automático
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
        family.SyncStatus = SyncStatus.Local;
        family.SyncHash = GenerateHash(family);

        _localFamilies.Add(family);
        await _localDataService.SaveFamilyAsync(family);

        // Auto-sync em background
        _ = Task.Run(async () => await TryAutoSyncFamilyAsync(family));

        Debug.WriteLine($"✅ Created family: {family.Name}");
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
        if (family.SyncStatus == SyncStatus.Synced)
        {
            family.SyncStatus = SyncStatus.Pending;
        }
        family.SyncHash = GenerateHash(family);

        _localFamilies[existingIndex] = family;
        await _localDataService.SaveFamilyAsync(family);

        // Auto-sync em background
        _ = Task.Run(async () => await TryAutoSyncFamilyAsync(family));

        Debug.WriteLine($"✅ Updated family: {family.Name}");
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

        // Auto-sync em background
        _ = Task.Run(async () => await TryAutoSyncFamilyAsync(family));

        Debug.WriteLine($"✅ Deleted family: {family.Name}");
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

    // SYNC AUTOMÁTICO EM BACKGROUND
    private async Task TryAutoSyncFamilyAsync(Family family)
    {
        try
        {
            if (!_supabaseService.IsAuthenticated)
                return;

            var success = await _syncService.UploadFamilyAsync(family);

            if (success)
            {
                family.SyncStatus = SyncStatus.Synced;
                family.LastSyncAt = DateTime.UtcNow;
                await _localDataService.SaveFamilyAsync(family);
                Debug.WriteLine($"🔄 Auto-synced: {family.Name}");
            }
            else
            {
                family.SyncStatus = SyncStatus.Error;
                await _localDataService.SaveFamilyAsync(family);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Auto-sync error: {ex.Message}");
            family.SyncStatus = SyncStatus.Error;
            await _localDataService.SaveFamilyAsync(family);
        }
    }

    public async Task<SyncResult> ForceFullSyncAsync()
    {
        Debug.WriteLine("🔄 Manual sync started...");

        if (!_supabaseService.IsAuthenticated)
        {
            return new SyncResult
            {
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                ErrorMessages = { "User not authenticated" }
            };
        }

        var result = await _syncService.PerformFullSyncAsync(_localFamilies);

        // Update local status
        foreach (var family in _localFamilies.Where(f =>
            f.SyncStatus == SyncStatus.Pending || f.SyncStatus == SyncStatus.Local))
        {
            family.SyncStatus = SyncStatus.Synced;
            family.LastSyncAt = DateTime.UtcNow;
            await _localDataService.SaveFamilyAsync(family);
        }

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