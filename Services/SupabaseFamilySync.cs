using OrchidPro.Models;
using OrchidPro.Services.Data;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Diagnostics;

namespace OrchidPro.Services;

/// <summary>
/// Modelo da Family para Supabase (CORRIGIDO para corresponder ao schema)
/// </summary>
[Table("families")]
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
    public bool IsSystemDefault { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

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
            IsSystemDefault = this.IsSystemDefault,
            IsActive = this.IsActive,
            CreatedAt = this.CreatedAt,
            UpdatedAt = this.UpdatedAt,
            SyncStatus = SyncStatus.Synced,
            LastSyncAt = DateTime.UtcNow,
            SyncHash = this.SyncHash
        };
    }
}

/// <summary>
/// Serviço de sincronização - TIPOS CORRIGIDOS
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
            if (_supabaseService.Client == null || !_supabaseService.IsAuthenticated)
            {
                Debug.WriteLine("❌ Supabase not ready for upload");
                return false;
            }

            Debug.WriteLine($"📤 Uploading family: {family.Name}");

            var supabaseFamily = SupabaseFamily.FromFamily(family);

            // Se não tem user_id e não é system default, pegar do usuário atual
            if (supabaseFamily.UserId == null && !supabaseFamily.IsSystemDefault)
            {
                var currentUserId = _supabaseService.GetCurrentUserId();
                if (Guid.TryParse(currentUserId, out var userId))
                {
                    supabaseFamily.UserId = userId;
                }
            }

            // Verificar se já existe - CORRIGIDO
            var existingResponse = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Where(f => f.Id == family.Id)
                .Get();

            var existing = existingResponse?.Models?.FirstOrDefault();

            if (existing != null)
            {
                // Atualizar existente - CORRIGIDO
                Debug.WriteLine($"🔄 Updating existing family: {family.Name}");

                var updateResponse = await _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Where(f => f.Id == family.Id)
                    .Update(supabaseFamily);

                Debug.WriteLine($"✅ Updated family: {family.Name}");
            }
            else
            {
                // Inserir novo - CORRIGIDO
                Debug.WriteLine($"➕ Inserting new family: {family.Name}");

                var insertResponse = await _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Insert(supabaseFamily);

                Debug.WriteLine($"✅ Inserted family: {family.Name}");
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Upload failed for {family.Name}: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
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

            Debug.WriteLine("📥 Downloading families...");

            var currentUserId = _supabaseService.GetCurrentUserId();

            // CORRIGIDO: Usar métodos em sequência separada
            if (!string.IsNullOrEmpty(currentUserId) && Guid.TryParse(currentUserId, out var userGuid))
            {
                // Famílias do usuário OU system defaults (user_id IS NULL)
                Debug.WriteLine($"📥 Downloading for user: {userGuid}");

                var response = await _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Where(f => f.UserId == userGuid || f.UserId == null)
                    .Get();

                var families = response?.Models?
                    .Select(sf => sf.ToFamily())
                    .ToList() ?? new List<Family>();

                Debug.WriteLine($"📥 Downloaded {families.Count} families");
                return families;
            }
            else
            {
                // Apenas system defaults se não autenticado
                Debug.WriteLine("📥 Downloading system defaults only");

                var response = await _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Where(f => f.UserId == null)
                    .Get();

                var families = response?.Models?
                    .Select(sf => sf.ToFamily())
                    .ToList() ?? new List<Family>();

                Debug.WriteLine($"📥 Downloaded {families.Count} system default families");
                return families;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Download failed: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
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
            Debug.WriteLine("🔄 Starting sync...");

            // 1. Download server families
            var serverFamilies = await DownloadFamiliesAsync();

            // 2. Upload pending local families
            var pendingLocal = localFamilies.Where(f =>
                f.SyncStatus == SyncStatus.Local ||
                f.SyncStatus == SyncStatus.Pending ||
                f.SyncStatus == SyncStatus.Error
            ).ToList();

            result.TotalProcessed = pendingLocal.Count;

            foreach (var family in pendingLocal)
            {
                var success = await UploadFamilyAsync(family);
                if (success)
                    result.Successful++;
                else
                    result.Failed++;

                // Small delay to avoid rate limiting
                await Task.Delay(100);
            }

            Debug.WriteLine($"🔄 Sync completed: {result.Successful}/{result.TotalProcessed}");
        }
        catch (Exception ex)
        {
            result.ErrorMessages.Add(ex.Message);
            Debug.WriteLine($"❌ Sync failed: {ex.Message}");
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

            // Teste simples na tabela families
            var response = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Limit(1)
                .Get();

            Debug.WriteLine("✅ Families table connection OK");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Families table connection failed: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// ADICIONADO: Testa se consegue fazer insert simples
    /// </summary>
    public async Task<bool> TestInsertAsync()
    {
        try
        {
            if (_supabaseService.Client == null || !_supabaseService.IsAuthenticated)
            {
                Debug.WriteLine("❌ Cannot test insert - not authenticated");
                return false;
            }

            Debug.WriteLine("🧪 Testing insert capability...");

            var testFamily = new SupabaseFamily
            {
                Id = Guid.NewGuid(),
                Name = $"TEST_DELETE_ME_{DateTime.Now:HHmmss}",
                Description = "Test family for connection testing",
                IsSystemDefault = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UserId = Guid.TryParse(_supabaseService.GetCurrentUserId(), out var uid) ? uid : null
            };

            var insertResponse = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Insert(testFamily);

            Debug.WriteLine("✅ Insert test successful");

            // Cleanup - delete the test family
            try
            {
                await _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Where(f => f.Id == testFamily.Id)
                    .Delete();

                Debug.WriteLine("✅ Test family cleaned up");
            }
            catch (Exception cleanupEx)
            {
                Debug.WriteLine($"⚠️ Cleanup failed (not critical): {cleanupEx.Message}");
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Insert test failed: {ex.Message}");
            return false;
        }
    }
}