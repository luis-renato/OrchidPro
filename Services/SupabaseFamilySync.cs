using OrchidPro.Models;
using OrchidPro.Services.Data;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Diagnostics;

namespace OrchidPro.Services;

/// <summary>
/// FINAL: Modelo da Family para Supabase - SCHEMA PUBLIC (families)
/// </summary>
[Table("families")] // ✅ Schema public - padrão do Supabase
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
/// Serviço de sincronização com Supabase - OTIMIZADO para schema public
/// </summary>
public class SupabaseFamilySync
{
    private readonly SupabaseService _supabaseService;

    public SupabaseFamilySync(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    /// <summary>
    /// FINAL: Teste de schema public - deve funcionar perfeitamente agora
    /// </summary>
    public async Task<bool> TestSchemaAndPermissionsAsync()
    {
        try
        {
            Debug.WriteLine("🔍 === SCHEMA TEST: public.families ===");

            if (_supabaseService.Client == null)
            {
                Debug.WriteLine("❌ Client is null");
                return false;
            }

            if (!_supabaseService.IsAuthenticated)
            {
                Debug.WriteLine("❌ Not authenticated");
                return false;
            }

            var userId = _supabaseService.GetCurrentUserId();
            var userEmail = _supabaseService.GetCurrentUser()?.Email;
            Debug.WriteLine($"🔐 User ID: {userId ?? "null"}");
            Debug.WriteLine($"📧 User Email: {userEmail ?? "null"}");

            try
            {
                Debug.WriteLine("🧪 Testing public.families access...");

                var query = _supabaseService.Client.From<SupabaseFamily>();
                Debug.WriteLine("✅ Query object created");

                var limitedQuery = query.Limit(5); // Buscar mais para teste
                Debug.WriteLine("✅ Limit added");

                var response = await limitedQuery.Get();
                Debug.WriteLine($"✅ Query executed, response: {response != null}");

                if (response?.Models != null)
                {
                    Debug.WriteLine($"✅ Models count: {response.Models.Count}");

                    if (response.Models.Any())
                    {
                        foreach (var family in response.Models.Take(3))
                        {
                            Debug.WriteLine($"  - {family.Name} (ID: {family.Id}, User: {family.UserId?.ToString() ?? "system"})");
                        }

                        Debug.WriteLine("🎉 SUCCESS: public.families fully accessible!");

                        // Teste adicional: verificar se consegue filtrar por user
                        if (Guid.TryParse(userId, out var userGuid))
                        {
                            var userFamilies = response.Models.Where(f => f.UserId == userGuid).ToList();
                            var systemFamilies = response.Models.Where(f => f.UserId == null).ToList();

                            Debug.WriteLine($"📊 My families: {userFamilies.Count}");
                            Debug.WriteLine($"📊 System families: {systemFamilies.Count}");
                            Debug.WriteLine($"📊 Total accessible: {response.Models.Count}");
                        }

                        return true;
                    }
                    else
                    {
                        Debug.WriteLine("⚠️ No families found - table is empty or RLS blocking all");
                        Debug.WriteLine("💡 This could mean successful connection but empty data");
                        return true; // Conexão funcionou, mas sem dados
                    }
                }
                else
                {
                    Debug.WriteLine("❌ Response.Models was null");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Query failed: {ex.Message}");
                Debug.WriteLine($"❌ Exception type: {ex.GetType().Name}");

                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"❌ Inner exception: {ex.InnerException.Message}");
                }

                if (ex.Message.Contains("permission denied"))
                {
                    Debug.WriteLine("🔍 DIAGNOSIS: RLS policy blocking access");
                    Debug.WriteLine("💡 Check RLS policies on public.families");
                }
                else if (ex.Message.Contains("does not exist"))
                {
                    Debug.WriteLine("🔍 DIAGNOSIS: public.families table not found");
                    Debug.WriteLine("💡 Migration may not have completed successfully");
                }

                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Test failed completely: {ex.Message}");
            return false;
        }
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

            Debug.WriteLine($"📤 Uploading family to public.families: {family.Name} (ID: {family.Id})");

            var supabaseFamily = SupabaseFamily.FromFamily(family);

            // Garantir user_id para famílias não-system
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

            // Verificar se existe
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

            return false;
        }
    }

    /// <summary>
    /// VERSÃO DEBUG do DownloadFamiliesAsync - para identificar problema RLS
    /// </summary>
    public async Task<List<Family>> DownloadFamiliesAsync()
    {
        try
        {
            if (_supabaseService.Client == null)
            {
                Debug.WriteLine("❌ Supabase client not available");
                return new List<Family>();
            }

            Debug.WriteLine("📥 Starting families download from public.families...");

            var currentUserId = _supabaseService.GetCurrentUserId();
            Debug.WriteLine($"📥 Current user ID: {currentUserId ?? "null"}");

            // 🧪 TESTE 1: Query sem filtros (para ver se RLS permite acesso básico)
            try
            {
                Debug.WriteLine("🧪 TEST 1: Query without filters...");
                var basicQuery = _supabaseService.Client.From<SupabaseFamily>();
                var basicResponse = await basicQuery.Get();

                Debug.WriteLine($"✅ Basic query result: {basicResponse?.Models?.Count ?? 0} families");

                if (basicResponse?.Models?.Any() == true)
                {
                    foreach (var family in basicResponse.Models.Take(3))
                    {
                        Debug.WriteLine($"  - {family.Name} (User: {family.UserId?.ToString() ?? "system"})");
                    }
                }
            }
            catch (Exception basicEx)
            {
                Debug.WriteLine($"❌ Basic query failed: {basicEx.Message}");
                Debug.WriteLine("🔍 This indicates RLS is blocking ALL access");
            }

            // 🧪 TESTE 2: Query só para system families (user_id IS NULL)
            try
            {
                Debug.WriteLine("🧪 TEST 2: System families only...");
                var systemQuery = _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Where(f => f.UserId == null);

                var systemResponse = await systemQuery.Get();
                Debug.WriteLine($"✅ System families: {systemResponse?.Models?.Count ?? 0}");

                if (systemResponse?.Models?.Any() == true)
                {
                    foreach (var family in systemResponse.Models.Take(3))
                    {
                        Debug.WriteLine($"  - {family.Name} (System: {family.IsSystemDefault})");
                    }
                }
            }
            catch (Exception systemEx)
            {
                Debug.WriteLine($"❌ System families query failed: {systemEx.Message}");
            }

            // 🧪 TESTE 3: Query só para minhas families (se autenticado)
            if (!string.IsNullOrEmpty(currentUserId) && Guid.TryParse(currentUserId, out var userGuid))
            {
                try
                {
                    Debug.WriteLine("🧪 TEST 3: My families only...");
                    var myQuery = _supabaseService.Client
                        .From<SupabaseFamily>()
                        .Where(f => f.UserId == userGuid);

                    var myResponse = await myQuery.Get();
                    Debug.WriteLine($"✅ My families: {myResponse?.Models?.Count ?? 0}");

                    if (myResponse?.Models?.Any() == true)
                    {
                        foreach (var family in myResponse.Models)
                        {
                            Debug.WriteLine($"  - {family.Name} (My family)");
                        }
                    }
                }
                catch (Exception myEx)
                {
                    Debug.WriteLine($"❌ My families query failed: {myEx.Message}");
                }

                // 🧪 TESTE 4: Query combinada (como o app faz normalmente)
                try
                {
                    Debug.WriteLine("🧪 TEST 4: Combined query (system OR mine)...");
                    var combinedQuery = _supabaseService.Client
                        .From<SupabaseFamily>()
                        .Where(f => f.UserId == userGuid || f.UserId == null);

                    var combinedResponse = await combinedQuery.Get();
                    Debug.WriteLine($"✅ Combined query: {combinedResponse?.Models?.Count ?? 0} families");

                    if (combinedResponse?.Models?.Any() == true)
                    {
                        var systemCount = combinedResponse.Models.Count(f => f.UserId == null);
                        var userCount = combinedResponse.Models.Count(f => f.UserId != null);

                        Debug.WriteLine($"  📊 System families: {systemCount}");
                        Debug.WriteLine($"  📊 User families: {userCount}");
                        Debug.WriteLine($"  📊 Total accessible: {combinedResponse.Models.Count}");

                        // Esta é a query que deveria funcionar!
                        var families = combinedResponse.Models.Select(sf => sf.ToFamily()).ToList();
                        Debug.WriteLine("🎉 SUCCESS: Combined query worked!");
                        return families;
                    }
                    else
                    {
                        Debug.WriteLine("⚠️ Combined query returned no families");
                        Debug.WriteLine("🔍 RLS policy is blocking the combined WHERE clause");
                    }
                }
                catch (Exception combinedEx)
                {
                    Debug.WriteLine($"❌ Combined query failed: {combinedEx.Message}");
                    Debug.WriteLine("🔍 This is the main problem - RLS blocking combined query");

                    if (combinedEx.Message.Contains("permission denied"))
                    {
                        Debug.WriteLine("🎯 CONFIRMED: RLS policy blocking combined query");
                        Debug.WriteLine("💡 Need to fix RLS policy for: (user_id = auth.uid() OR user_id IS NULL)");
                    }
                }
            }
            else
            {
                Debug.WriteLine("⚠️ Not authenticated - trying system families only");

                try
                {
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
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ System families download failed: {ex.Message}");
                }
            }

            Debug.WriteLine("❌ All download attempts failed - returning empty list");
            return new List<Family>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Download failed completely: {ex.Message}");

            if (ex.InnerException != null)
            {
                Debug.WriteLine($"❌ Inner exception: {ex.InnerException.Message}");
            }

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
            Debug.WriteLine("🔄 Starting full sync with public.families...");

            // Primeiro teste schema/permissões
            var schemaOk = await TestSchemaAndPermissionsAsync();
            if (!schemaOk)
            {
                result.ErrorMessages.Add("Schema or permissions test failed");
                result.EndTime = DateTime.UtcNow;
                result.Duration = result.EndTime - result.StartTime;
                return result;
            }

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

            Debug.WriteLine("🧪 Testing public.families table connection...");
            Debug.WriteLine($"🔐 User authenticated: {_supabaseService.IsAuthenticated}");
            Debug.WriteLine($"🆔 User ID: {_supabaseService.GetCurrentUserId()}");

            // Teste schema
            var schemaOk = await TestSchemaAndPermissionsAsync();
            return schemaOk;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Connection test failed: {ex.Message}");
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

            Debug.WriteLine("🧪 Testing insert capability on public.families...");

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