using OrchidPro.Models;
using OrchidPro.Services.Data;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Diagnostics;

namespace OrchidPro.Services;

/// <summary>
/// CORRIGIDO: Modelo da Family para Supabase - SCHEMA PUBLIC (families)
/// Versão com prevenção de duplicatas e sync melhorado
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
/// CORRIGIDO: Serviço de sincronização com prevenção de duplicatas
/// </summary>
public class SupabaseFamilySync
{
    private readonly SupabaseService _supabaseService;
    private readonly HashSet<Guid> _syncingFamilies = new(); // Previne sync simultâneo
    private readonly object _syncLock = new object();

    public SupabaseFamilySync(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    /// <summary>
    /// CORRIGIDO: Upload com verificação de duplicatas e controle de concorrência
    /// </summary>
    public async Task<bool> UploadFamilyAsync(Family family)
    {
        lock (_syncLock)
        {
            if (_syncingFamilies.Contains(family.Id))
            {
                Debug.WriteLine($"⏸️ Family {family.Name} already syncing - skipping");
                return false;
            }
            _syncingFamilies.Add(family.Id);
        }

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

            Debug.WriteLine($"📤 [SYNC] Starting upload for: {family.Name} (ID: {family.Id})");

            var supabaseFamily = SupabaseFamily.FromFamily(family);

            // Garantir user_id para famílias não-system
            if (supabaseFamily.UserId == null && !(supabaseFamily.IsSystemDefault ?? false))
            {
                var currentUserId = _supabaseService.GetCurrentUserId();
                if (Guid.TryParse(currentUserId, out var userId))
                {
                    supabaseFamily.UserId = userId;
                    Debug.WriteLine($"📤 [SYNC] Set user_id to: {userId}");
                }
                else
                {
                    Debug.WriteLine("❌ [SYNC] Could not determine current user ID");
                    return false;
                }
            }

            // 🔍 PASSO 1: Verificar se já existe no servidor
            Debug.WriteLine($"🔍 [SYNC] Checking server for existing family: {family.Id}");

            var existingQuery = _supabaseService.Client
                .From<SupabaseFamily>()
                .Where(f => f.Id == family.Id)
                .Limit(1);

            var existingResponse = await existingQuery.Get();
            var existing = existingResponse?.Models?.FirstOrDefault();

            // 🔍 PASSO 2: Verificar duplicatas por nome
            Debug.WriteLine($"🔍 [SYNC] Checking for name duplicates: {family.Name}");

            var duplicateQuery = _supabaseService.Client
                .From<SupabaseFamily>()
                .Where(f => f.Name == family.Name && f.UserId == supabaseFamily.UserId)
                .Limit(5); // Verificar até 5 possíveis duplicatas

            var duplicateResponse = await duplicateQuery.Get();
            var duplicates = duplicateResponse?.Models?.Where(f => f.Id != family.Id).ToList() ?? new List<SupabaseFamily>();

            if (duplicates.Any())
            {
                Debug.WriteLine($"⚠️ [SYNC] Found {duplicates.Count} duplicate(s) by name for: {family.Name}");

                foreach (var dup in duplicates)
                {
                    Debug.WriteLine($"  - Duplicate ID: {dup.Id}, Created: {dup.CreatedAt}");
                }

                // 🧹 LIMPAR DUPLICATAS ANTIGAS (manter a mais antiga por segurança)
                var oldestDuplicate = duplicates.OrderBy(d => d.CreatedAt).First();
                var toDelete = duplicates.Where(d => d.Id != oldestDuplicate.Id).ToList();

                if (toDelete.Any())
                {
                    Debug.WriteLine($"🧹 [SYNC] Cleaning {toDelete.Count} duplicate(s)...");

                    foreach (var deleteItem in toDelete)
                    {
                        try
                        {
                            await _supabaseService.Client
                                .From<SupabaseFamily>()
                                .Where(f => f.Id == deleteItem.Id)
                                .Delete();

                            Debug.WriteLine($"🧹 [SYNC] Deleted duplicate: {deleteItem.Id}");
                        }
                        catch (Exception deleteEx)
                        {
                            Debug.WriteLine($"❌ [SYNC] Failed to delete duplicate {deleteItem.Id}: {deleteEx.Message}");
                        }
                    }
                }

                // Se o ID atual não é o mesmo que o mais antigo, usar o ID do mais antigo
                if (family.Id != oldestDuplicate.Id)
                {
                    Debug.WriteLine($"🔄 [SYNC] Using existing ID {oldestDuplicate.Id} instead of {family.Id}");
                    existing = oldestDuplicate;
                    supabaseFamily.Id = oldestDuplicate.Id;
                }
            }

            // 🔄 PASSO 3: INSERT ou UPDATE
            if (existing != null)
            {
                Debug.WriteLine($"🔄 [SYNC] Updating existing family: {family.Name} (Server ID: {existing.Id})");

                // Preservar timestamps de criação
                supabaseFamily.CreatedAt = existing.CreatedAt;
                supabaseFamily.UpdatedAt = DateTime.UtcNow;

                var updateQuery = _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Where(f => f.Id == existing.Id);

                var updateResponse = await updateQuery.Update(supabaseFamily);

                if (updateResponse?.Models?.Any() == true)
                {
                    Debug.WriteLine($"✅ [SYNC] Family updated successfully: {family.Name}");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"❌ [SYNC] Update returned no models for: {family.Name}");
                    return false;
                }
            }
            else
            {
                Debug.WriteLine($"➕ [SYNC] Inserting new family: {family.Name}");

                // Garantir timestamps
                var now = DateTime.UtcNow;
                supabaseFamily.CreatedAt = now;
                supabaseFamily.UpdatedAt = now;

                var insertResponse = await _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Insert(supabaseFamily);

                if (insertResponse?.Models?.Any() == true)
                {
                    var inserted = insertResponse.Models.First();
                    Debug.WriteLine($"✅ [SYNC] Family inserted successfully: {family.Name} (Server ID: {inserted.Id})");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"❌ [SYNC] Insert returned no models for: {family.Name}");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [SYNC] Upload failed for {family.Name}: {ex.Message}");

            if (ex.InnerException != null)
            {
                Debug.WriteLine($"❌ [SYNC] Inner exception: {ex.InnerException.Message}");
            }

            // Se erro de unique constraint, pode ser duplicata
            if (ex.Message.Contains("unique") || ex.Message.Contains("duplicate"))
            {
                Debug.WriteLine($"🔍 [SYNC] Detected duplicate constraint error - family may already exist");

                // Tentar buscar novamente e marcar como sincronizado
                try
                {
                    var recheckQuery = _supabaseService.Client
                        .From<SupabaseFamily>()
                        .Where(f => f.Name == family.Name && f.UserId == family.UserId)
                        .Limit(1);

                    var recheckResponse = await recheckQuery.Get();

                    if (recheckResponse?.Models?.Any() == true)
                    {
                        Debug.WriteLine($"✅ [SYNC] Found existing family after constraint error - treating as success");
                        return true;
                    }
                }
                catch
                {
                    // Ignore recheck errors
                }
            }

            return false;
        }
        finally
        {
            lock (_syncLock)
            {
                _syncingFamilies.Remove(family.Id);
            }
        }
    }

    /// <summary>
    /// CORRIGIDO: Download com melhor tratamento de conflitos
    /// </summary>
    public async Task<List<Family>> DownloadFamiliesAsync()
    {
        try
        {
            if (_supabaseService.Client == null)
            {
                Debug.WriteLine("❌ [DOWNLOAD] Supabase client not available");
                return new List<Family>();
            }

            Debug.WriteLine("📥 [DOWNLOAD] Starting families download from public.families...");

            var currentUserId = _supabaseService.GetCurrentUserId();
            Debug.WriteLine($"📥 [DOWNLOAD] Current user ID: {currentUserId ?? "null"}");

            // Query combinada mais robusta
            var query = _supabaseService.Client.From<SupabaseFamily>();

            // Se autenticado, buscar minhas + system
            if (!string.IsNullOrEmpty(currentUserId) && Guid.TryParse(currentUserId, out var userGuid))
            {
                Debug.WriteLine("📥 [DOWNLOAD] Authenticated - fetching user + system families...");

                // Buscar em duas queries separadas para evitar problemas com OR
                var userQuery = _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Where(f => f.UserId == userGuid);

                var systemQuery = _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Where(f => f.UserId == null);

                var userResponse = await userQuery.Get();
                var systemResponse = await systemQuery.Get();

                var allFamilies = new List<SupabaseFamily>();

                if (userResponse?.Models != null)
                {
                    allFamilies.AddRange(userResponse.Models);
                    Debug.WriteLine($"📥 [DOWNLOAD] User families: {userResponse.Models.Count}");
                }

                if (systemResponse?.Models != null)
                {
                    allFamilies.AddRange(systemResponse.Models);
                    Debug.WriteLine($"📥 [DOWNLOAD] System families: {systemResponse.Models.Count}");
                }

                // 🧹 DETECTAR E LIMPAR DUPLICATAS
                var groupedByName = allFamilies.GroupBy(f => f.Name.ToLowerInvariant()).ToList();
                var duplicateGroups = groupedByName.Where(g => g.Count() > 1).ToList();

                if (duplicateGroups.Any())
                {
                    Debug.WriteLine($"🧹 [DOWNLOAD] Found {duplicateGroups.Count} groups with duplicates");

                    foreach (var group in duplicateGroups)
                    {
                        var items = group.OrderBy(f => f.CreatedAt).ToList();
                        var keepItem = items.First(); // Manter o mais antigo
                        var deleteItems = items.Skip(1).ToList();

                        Debug.WriteLine($"🧹 [DOWNLOAD] Group '{group.Key}': keeping {keepItem.Id}, deleting {deleteItems.Count} duplicates");

                        foreach (var deleteItem in deleteItems)
                        {
                            try
                            {
                                await _supabaseService.Client
                                    .From<SupabaseFamily>()
                                    .Where(f => f.Id == deleteItem.Id)
                                    .Delete();

                                Debug.WriteLine($"🧹 [DOWNLOAD] Deleted duplicate: {deleteItem.Id}");
                                allFamilies.Remove(deleteItem);
                            }
                            catch (Exception deleteEx)
                            {
                                Debug.WriteLine($"❌ [DOWNLOAD] Failed to delete duplicate {deleteItem.Id}: {deleteEx.Message}");
                            }
                        }
                    }
                }

                var families = allFamilies.Select(sf => sf.ToFamily()).ToList();
                Debug.WriteLine($"📥 [DOWNLOAD] Successfully downloaded {families.Count} unique families");
                return families;
            }
            else
            {
                Debug.WriteLine("📥 [DOWNLOAD] Not authenticated - fetching system families only...");

                var systemQuery = _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Where(f => f.UserId == null);

                var response = await systemQuery.Get();

                if (response?.Models != null)
                {
                    var families = response.Models.Select(sf => sf.ToFamily()).ToList();
                    Debug.WriteLine($"📥 [DOWNLOAD] Downloaded {families.Count} system default families");
                    return families;
                }
            }

            Debug.WriteLine("📥 [DOWNLOAD] No families found");
            return new List<Family>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [DOWNLOAD] Download failed: {ex.Message}");

            if (ex.InnerException != null)
            {
                Debug.WriteLine($"❌ [DOWNLOAD] Inner exception: {ex.InnerException.Message}");
            }

            return new List<Family>();
        }
    }

    /// <summary>
    /// NOVO: Detecta e limpa todas as duplicatas
    /// </summary>
    public async Task<int> CleanupDuplicatesAsync()
    {
        try
        {
            Debug.WriteLine("🧹 [CLEANUP] Starting duplicate cleanup...");

            if (_supabaseService.Client == null || !_supabaseService.IsAuthenticated)
            {
                Debug.WriteLine("❌ [CLEANUP] Cannot cleanup - not authenticated");
                return 0;
            }

            // Buscar todas as famílias
            var allQuery = _supabaseService.Client.From<SupabaseFamily>();
            var allResponse = await allQuery.Get();

            if (allResponse?.Models == null || !allResponse.Models.Any())
            {
                Debug.WriteLine("🧹 [CLEANUP] No families found for cleanup");
                return 0;
            }

            Debug.WriteLine($"🧹 [CLEANUP] Found {allResponse.Models.Count} total families");

            // Agrupar por nome (case-insensitive) e user_id
            var groups = allResponse.Models
                .GroupBy(f => new {
                    Name = f.Name.ToLowerInvariant(),
                    UserId = f.UserId
                })
                .Where(g => g.Count() > 1)
                .ToList();

            if (!groups.Any())
            {
                Debug.WriteLine("🧹 [CLEANUP] No duplicates found");
                return 0;
            }

            Debug.WriteLine($"🧹 [CLEANUP] Found {groups.Count} groups with duplicates");

            int deletedCount = 0;

            foreach (var group in groups)
            {
                var duplicates = group.OrderBy(f => f.CreatedAt).ToList();
                var keepItem = duplicates.First(); // Manter o mais antigo
                var deleteItems = duplicates.Skip(1).ToList();

                Debug.WriteLine($"🧹 [CLEANUP] Processing group '{group.Key.Name}' (User: {group.Key.UserId?.ToString() ?? "system"})");
                Debug.WriteLine($"    Keeping: {keepItem.Id} (created: {keepItem.CreatedAt})");
                Debug.WriteLine($"    Deleting: {deleteItems.Count} duplicates");

                foreach (var deleteItem in deleteItems)
                {
                    try
                    {
                        await _supabaseService.Client
                            .From<SupabaseFamily>()
                            .Where(f => f.Id == deleteItem.Id)
                            .Delete();

                        Debug.WriteLine($"    ✅ Deleted: {deleteItem.Id}");
                        deletedCount++;
                    }
                    catch (Exception deleteEx)
                    {
                        Debug.WriteLine($"    ❌ Failed to delete {deleteItem.Id}: {deleteEx.Message}");
                    }
                }
            }

            Debug.WriteLine($"🧹 [CLEANUP] Cleanup completed - deleted {deletedCount} duplicates");
            return deletedCount;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [CLEANUP] Cleanup failed: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// CORRIGIDO: Sync completo com cleanup automático
    /// </summary>
    public async Task<SyncResult> PerformFullSyncAsync(List<Family> localFamilies)
    {
        var result = new SyncResult
        {
            StartTime = DateTime.UtcNow
        };

        try
        {
            Debug.WriteLine("🔄 [FULL_SYNC] Starting comprehensive sync with cleanup...");

            // Passo 1: Limpar duplicatas no servidor
            Debug.WriteLine("🧹 [FULL_SYNC] Step 1: Cleanup duplicates...");
            var cleanedCount = await CleanupDuplicatesAsync();
            Debug.WriteLine($"🧹 [FULL_SYNC] Cleaned {cleanedCount} duplicates");

            // Passo 2: Download servidor atualizado
            Debug.WriteLine("📥 [FULL_SYNC] Step 2: Download updated server data...");
            var serverFamilies = await DownloadFamiliesAsync();
            Debug.WriteLine($"📥 [FULL_SYNC] Found {serverFamilies.Count} families on server");

            // Passo 3: Identificar families locais que precisam sync
            var pendingLocal = localFamilies.Where(f =>
                f.SyncStatus == SyncStatus.Local ||
                f.SyncStatus == SyncStatus.Pending ||
                f.SyncStatus == SyncStatus.Error
            ).ToList();

            Debug.WriteLine($"📤 [FULL_SYNC] Step 3: Found {pendingLocal.Count} local families to upload");

            result.TotalProcessed = pendingLocal.Count;

            // Passo 4: Upload com delays para evitar rate limiting
            foreach (var family in pendingLocal)
            {
                Debug.WriteLine($"📤 [FULL_SYNC] Uploading: {family.Name}");

                var success = await UploadFamilyAsync(family);

                if (success)
                {
                    result.Successful++;
                    Debug.WriteLine($"✅ [FULL_SYNC] Uploaded: {family.Name}");
                }
                else
                {
                    result.Failed++;
                    result.ErrorMessages.Add($"Failed to upload: {family.Name}");
                    Debug.WriteLine($"❌ [FULL_SYNC] Failed: {family.Name}");
                }

                // Delay entre uploads para evitar rate limiting
                await Task.Delay(200);
            }

            Debug.WriteLine($"🔄 [FULL_SYNC] Sync completed: {result.Successful}/{result.TotalProcessed} successful");
        }
        catch (Exception ex)
        {
            result.ErrorMessages.Add($"Full sync error: {ex.Message}");
            Debug.WriteLine($"❌ [FULL_SYNC] Sync failed with exception: {ex.Message}");
        }

        result.EndTime = DateTime.UtcNow;
        result.Duration = result.EndTime - result.StartTime;
        return result;
    }

    public async Task<bool> TestSchemaAndPermissionsAsync()
    {
        try
        {
            Debug.WriteLine("🔍 [TEST] Testing public.families access...");

            if (_supabaseService.Client == null || !_supabaseService.IsAuthenticated)
            {
                Debug.WriteLine("❌ [TEST] Client null or not authenticated");
                return false;
            }

            var query = _supabaseService.Client.From<SupabaseFamily>().Limit(1);
            var response = await query.Get();

            var success = response?.Models != null;
            Debug.WriteLine($"✅ [TEST] Schema test result: {success}");

            return success;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [TEST] Schema test failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        return await TestSchemaAndPermissionsAsync();
    }

    public async Task<bool> TestInsertAsync()
    {
        try
        {
            if (_supabaseService.Client == null || !_supabaseService.IsAuthenticated)
            {
                return false;
            }

            var testId = Guid.NewGuid();
            var currentUserId = _supabaseService.GetCurrentUserId();

            if (!Guid.TryParse(currentUserId, out var userId))
            {
                return false;
            }

            var testFamily = new SupabaseFamily
            {
                Id = testId,
                Name = $"TEST_DELETE_ME_{DateTime.Now:HHmmss}",
                Description = "Test family - will be deleted",
                IsSystemDefault = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UserId = userId
            };

            var insertResponse = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Insert(testFamily);

            if (insertResponse?.Models?.Any() == true)
            {
                try
                {
                    await _supabaseService.Client
                        .From<SupabaseFamily>()
                        .Where(f => f.Id == testId)
                        .Delete();
                }
                catch
                {
                    // Ignore cleanup errors
                }

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [TEST] Insert test failed: {ex.Message}");
            return false;
        }
    }
}