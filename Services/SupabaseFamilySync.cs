using OrchidPro.Models;
using OrchidPro.Services.Data;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Diagnostics;

namespace OrchidPro.Services;

/// <summary>
/// CORRIGIDO: Modelo da Family para Supabase - SCHEMA CORRETO orchidpro.families
/// </summary>
[Table("orchidpro.families")]
public class SupabaseFamily : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_system_default")]
    public bool? IsSystemDefault { get; set; } = false;

    [Column("is_active")]
    public bool? IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("sync_hash")]
    public string? SyncHash { get; set; }

    public static SupabaseFamily FromFamily(Family family)
    {
        return new SupabaseFamily
        {
            Id = family.Id,
            UserId = family.UserId,
            Name = family.Name,
            Description = family.Description,
            IsSystemDefault = family.IsSystemDefault,
            IsActive = family.IsActive,
            CreatedAt = family.CreatedAt,
            UpdatedAt = family.UpdatedAt,
            SyncHash = family.SyncHash
        };
    }

    public Family ToFamily()
    {
        return new Family
        {
            Id = this.Id,
            UserId = this.UserId,
            Name = this.Name,
            Description = this.Description,
            IsSystemDefault = this.IsSystemDefault ?? false,
            IsActive = this.IsActive ?? true,
            CreatedAt = this.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = this.UpdatedAt ?? DateTime.UtcNow,
            SyncStatus = SyncStatus.Synced,
            LastSyncAt = DateTime.UtcNow,
            SyncHash = this.SyncHash
        };
    }
}

/// <summary>
/// Serviço de sincronização com Supabase - COMPLETO
/// </summary>
public class SupabaseFamilySync
{
    private readonly SupabaseService _supabaseService;

    public SupabaseFamilySync(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    public async Task<bool> UploadFamilyAsync(Family family)
    {
        try
        {
            if (_supabaseService.Client == null)
            {
                Debug.WriteLine("❌ Supabase client is null");
                return false;
            }

            if (!_supabaseService.IsAuthenticated)
            {
                Debug.WriteLine("❌ User not authenticated");
                return false;
            }

            Debug.WriteLine($"📤 Uploading family: {family.Name} (ID: {family.Id})");

            var supabaseFamily = SupabaseFamily.FromFamily(family);

            if (supabaseFamily.UserId == null && !(supabaseFamily.IsSystemDefault ?? false))
            {
                var currentUserId = _supabaseService.GetCurrentUserId();
                if (Guid.TryParse(currentUserId, out var userId))
                {
                    supabaseFamily.UserId = userId;
                    Debug.WriteLine($"📤 Set user_id to: {userId}");
                }
                else
                {
                    Debug.WriteLine("❌ Could not determine current user ID");
                    return false;
                }
            }

            Debug.WriteLine($"🔍 Checking if family exists: {family.Id}");

            var existingQuery = _supabaseService.Client
                .From<SupabaseFamily>()
                .Where(f => f.Id == family.Id);

            var existingResponse = await existingQuery.Get();
            var existing = existingResponse?.Models?.FirstOrDefault();

            if (existing != null)
            {
                Debug.WriteLine($"🔄 Updating existing family: {family.Name}");

                supabaseFamily.CreatedAt = existing.CreatedAt;

                var updateQuery = _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Where(f => f.Id == family.Id);

                var updateResponse = await updateQuery.Update(supabaseFamily);

                if (updateResponse?.Models?.Any() == true)
                {
                    Debug.WriteLine($"✅ Family updated successfully: {family.Name}");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"❌ Update returned no models for: {family.Name}");
                    return false;
                }
            }
            else
            {
                Debug.WriteLine($"➕ Inserting new family: {family.Name}");

                var insertResponse = await _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Insert(supabaseFamily);

                if (insertResponse?.Models?.Any() == true)
                {
                    Debug.WriteLine($"✅ Family inserted successfully: {family.Name}");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"❌ Insert returned no models for: {family.Name}");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Upload failed for {family.Name}: {ex.Message}");

            if (ex.InnerException != null)
            {
                Debug.WriteLine($"❌ Inner exception: {ex.InnerException.Message}");
            }

            Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    public async Task<List<Family>> DownloadFamiliesAsync()
    {
        try
        {
            if (_supabaseService.Client == null)
            {
                Debug.WriteLine("❌ Supabase client not available");
                return new List<Family>();
            }

            Debug.WriteLine("📥 Starting families download...");

            var currentUserId = _supabaseService.GetCurrentUserId();

            if (!string.IsNullOrEmpty(currentUserId) && Guid.TryParse(currentUserId, out var userGuid))
            {
                Debug.WriteLine($"📥 Downloading for authenticated user: {userGuid}");

                var query = _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Where(f => f.UserId == userGuid || f.UserId == null);

                Debug.WriteLine("📥 Executing query...");
                var response = await query.Get();

                if (response?.Models != null)
                {
                    var families = response.Models.Select(sf => sf.ToFamily()).ToList();
                    Debug.WriteLine($"📥 Downloaded {families.Count} families successfully");

                    foreach (var family in families.Take(3))
                    {
                        Debug.WriteLine($"  - {family.Name} (System: {family.IsSystemDefault})");
                    }

                    return families;
                }
                else
                {
                    Debug.WriteLine("❌ Response or models was null");
                    return new List<Family>();
                }
            }
            else
            {
                Debug.WriteLine("📥 Downloading system defaults only (not authenticated)");

                var query = _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Where(f => f.UserId == null);

                var response = await query.Get();

                if (response?.Models != null)
                {
                    var families = response.Models.Select(sf => sf.ToFamily()).ToList();
                    Debug.WriteLine($"📥 Downloaded {families.Count} system default families");
                    return families;
                }
                else
                {
                    Debug.WriteLine("❌ Response or models was null");
                    return new List<Family>();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Download failed: {ex.Message}");

            if (ex.InnerException != null)
            {
                Debug.WriteLine($"❌ Inner exception: {ex.InnerException.Message}");
            }

            Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            return new List<Family>();
        }
    }

    public async Task<SyncResult> PerformFullSyncAsync(List<Family> localFamilies)
    {
        var result = new SyncResult
        {
            StartTime = DateTime.UtcNow
        };

        try
        {
            Debug.WriteLine("🔄 Starting full sync...");

            Debug.WriteLine("📥 Step 1: Downloading server families...");
            var serverFamilies = await DownloadFamiliesAsync();
            Debug.WriteLine($"📥 Found {serverFamilies.Count} families on server");

            var pendingLocal = localFamilies.Where(f =>
                f.SyncStatus == SyncStatus.Local ||
                f.SyncStatus == SyncStatus.Pending ||
                f.SyncStatus == SyncStatus.Error
            ).ToList();

            Debug.WriteLine($"📤 Step 2: Found {pendingLocal.Count} local families to upload");

            result.TotalProcessed = pendingLocal.Count;

            foreach (var family in pendingLocal)
            {
                Debug.WriteLine($"📤 Uploading: {family.Name}");

                var success = await UploadFamilyAsync(family);

                if (success)
                {
                    result.Successful++;
                    Debug.WriteLine($"✅ Uploaded: {family.Name}");
                }
                else
                {
                    result.Failed++;
                    result.ErrorMessages.Add($"Failed to upload: {family.Name}");
                    Debug.WriteLine($"❌ Failed: {family.Name}");
                }

                await Task.Delay(100);
            }

            Debug.WriteLine($"🔄 Sync completed: {result.Successful}/{result.TotalProcessed} successful");
        }
        catch (Exception ex)
        {
            result.ErrorMessages.Add($"Sync error: {ex.Message}");
            Debug.WriteLine($"❌ Sync failed with exception: {ex.Message}");
        }

        result.EndTime = DateTime.UtcNow;
        result.Duration = result.EndTime - result.StartTime;
        return result;
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            if (_supabaseService.Client == null)
            {
                Debug.WriteLine("❌ Supabase client is null");
                return false;
            }

            Debug.WriteLine("🧪 Testing families table connection...");
            Debug.WriteLine($"🔐 User authenticated: {_supabaseService.IsAuthenticated}");
            Debug.WriteLine($"🆔 User ID: {_supabaseService.GetCurrentUserId()}");

            // Teste mais detalhado
            Debug.WriteLine("🧪 Step 1: Testing basic query...");

            var basicQuery = _supabaseService.Client.From<SupabaseFamily>();
            Debug.WriteLine("✅ Basic query object created");

            var limitQuery = basicQuery.Limit(1);
            Debug.WriteLine("✅ Limit applied");

            var response = await limitQuery.Get();
            Debug.WriteLine($"✅ Query executed, response: {(response != null ? "not null" : "null")}");

            if (response != null)
            {
                Debug.WriteLine($"✅ Models count: {response.Models?.Count ?? 0}");

                if (response.Models?.Any() == true)
                {
                    var firstModel = response.Models.First();
                    Debug.WriteLine($"✅ First model - Name: {firstModel.Name}, ID: {firstModel.Id}");
                }

                Debug.WriteLine("✅ Families table connection successful!");
                return true;
            }
            else
            {
                Debug.WriteLine("❌ Response was null");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Families table connection failed: {ex.Message}");
            Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                Debug.WriteLine($"❌ Inner exception: {ex.InnerException.Message}");
            }

            return false;
        }
    }

    public async Task<bool> TestInsertAsync()
    {
        try
        {
            if (_supabaseService.Client == null || !_supabaseService.IsAuthenticated)
            {
                Debug.WriteLine("❌ Cannot test insert - client null or not authenticated");
                return false;
            }

            Debug.WriteLine("🧪 Testing insert capability...");

            var currentUserId = _supabaseService.GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var userId))
            {
                Debug.WriteLine("❌ Cannot test insert - invalid user ID");
                return false;
            }

            var testFamily = new SupabaseFamily
            {
                Id = Guid.NewGuid(),
                Name = $"TEST_DELETE_ME_{DateTime.Now:HHmmss}",
                Description = "Test family for connection testing - will be deleted",
                IsSystemDefault = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UserId = userId
            };

            Debug.WriteLine($"🧪 Inserting test family: {testFamily.Name}");

            var insertResponse = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Insert(testFamily);

            if (insertResponse?.Models?.Any() == true)
            {
                Debug.WriteLine("✅ Insert test successful");

                try
                {
                    Debug.WriteLine("🧹 Cleaning up test family...");

                    await _supabaseService.Client
                        .From<SupabaseFamily>()
                        .Where(f => f.Id == testFamily.Id)
                        .Delete();

                    Debug.WriteLine("✅ Test family cleaned up successfully");
                }
                catch (Exception cleanupEx)
                {
                    Debug.WriteLine($"⚠️ Cleanup failed (not critical): {cleanupEx.Message}");
                }

                return true;
            }
            else
            {
                Debug.WriteLine("❌ Insert test failed - no models returned");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Insert test failed: {ex.Message}");

            if (ex.InnerException != null)
            {
                Debug.WriteLine($"❌ Inner exception: {ex.InnerException.Message}");
            }

            return false;
        }
    }
}