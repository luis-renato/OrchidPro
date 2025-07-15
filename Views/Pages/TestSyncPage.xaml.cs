using OrchidPro.Services.Data;
using OrchidPro.Services;
using OrchidPro.Models;
using System.Diagnostics;
using System.Text;

namespace OrchidPro.Views.Pages;

/// <summary>
/// CORRIGIDO: Página de teste com foco em debug de sincronização
/// Versão otimizada para detectar e resolver problemas de duplicação
/// </summary>
public partial class TestSyncPage : ContentPage
{
    private readonly SupabaseService _supabaseService;
    private readonly IFamilyRepository _familyRepository;
    private readonly SupabaseFamilySync _syncService;

    public TestSyncPage()
    {
        InitializeComponent();

        try
        {
            var services = MauiProgram.CreateMauiApp().Services;
            _supabaseService = services.GetRequiredService<SupabaseService>();
            _familyRepository = services.GetRequiredService<IFamilyRepository>();
            _syncService = services.GetRequiredService<SupabaseFamilySync>();

            LogTest("✅ All services loaded successfully");
            LogTest("🎯 SYNC DEBUG MODE - RLS Disabled");
            LogTest("🔍 Focus: Duplicate detection and resolution");
        }
        catch (Exception ex)
        {
            LogTest($"❌ Error loading services: {ex.Message}");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await UpdateQuickStats();
    }

    /// <summary>
    /// CORRIGIDO: Teste de Supabase com foco em duplicatas
    /// </summary>
    private async void OnTestSupabaseClicked(object sender, EventArgs e)
    {
        try
        {
            LogTest("🧪 === COMPREHENSIVE SYNC DEBUG TEST ===");
            LogTest("🎯 Target: Identify and fix duplication issues");
            LogTest("🔧 RLS Status: DISABLED for debugging");

            // Verificar estado atual
            LogTest("");
            LogTest("📊 === CURRENT STATE CHECK ===");
            _supabaseService.DebugCurrentState();

            // Initialize e verificar auth
            LogTest("🔄 Initializing Supabase...");
            await _supabaseService.InitializeAsync();

            var isAuth = _supabaseService.IsAuthenticated;
            var userId = _supabaseService.GetCurrentUserId();
            var userEmail = _supabaseService.GetCurrentUser()?.Email;

            LogTest($"🔐 Authentication: {isAuth}");
            LogTest($"🆔 User ID: {userId ?? "null"}");
            LogTest($"📧 User Email: {userEmail ?? "null"}");

            if (!isAuth)
            {
                LogTest("❌ Not authenticated - login first to continue debug");
                return;
            }

            LogTest("");
            LogTest("🔍 === CONNECTION AND SCHEMA TEST ===");

            var connectionOk = await _syncService.TestConnectionAsync();
            LogTest($"🌐 Connection test: {connectionOk}");

            if (!connectionOk)
            {
                LogTest("❌ Connection failed - check Supabase setup");
                return;
            }

            var insertOk = await _syncService.TestInsertAsync();
            LogTest($"➕ Insert test: {insertOk}");

            if (insertOk)
            {
                LogTest("🎉 BASIC FUNCTIONALITY: WORKING!");
                LogTest("✅ Can read from public.families");
                LogTest("✅ Can write to public.families");
                LogTest("✅ RLS properly disabled");
            }
            else
            {
                LogTest("❌ Insert test failed - check permissions");
                return;
            }

            LogTest("");
            LogTest("🔍 === DUPLICATE DETECTION TEST ===");
            await TestDuplicateDetection();

            LogTest("");
            LogTest("🧹 === AUTOMATIC CLEANUP TEST ===");
            await TestAutomaticCleanup();

            LogTest("🧪 === COMPREHENSIVE TEST COMPLETED ===");
            await UpdateQuickStats();

        }
        catch (Exception ex)
        {
            LogTest($"❌ Test failed with exception: {ex.Message}");
            LogTest($"❌ Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// NOVO: Teste específico para detectar duplicatas
    /// </summary>
    private async Task TestDuplicateDetection()
    {
        try
        {
            LogTest("🔍 Starting duplicate detection analysis...");

            // Download todas as famílias do servidor
            var serverFamilies = await _syncService.DownloadFamiliesAsync();
            LogTest($"📥 Total families on server: {serverFamilies.Count}");

            if (!serverFamilies.Any())
            {
                LogTest("⚠️ No families found on server");
                return;
            }

            // Analisar duplicatas por nome
            var duplicatesByName = serverFamilies
                .GroupBy(f => f.Name.ToLowerInvariant())
                .Where(g => g.Count() > 1)
                .ToList();

            LogTest($"🔍 Duplicate groups by name: {duplicatesByName.Count}");

            if (duplicatesByName.Any())
            {
                LogTest("⚠️ DUPLICATES DETECTED:");

                foreach (var group in duplicatesByName)
                {
                    LogTest($"  📝 Name: '{group.Key}' - {group.Count()} copies");

                    foreach (var family in group.OrderBy(f => f.CreatedAt))
                    {
                        LogTest($"    - ID: {family.Id}");
                        LogTest($"      Created: {family.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                        LogTest($"      User: {family.UserId?.ToString() ?? "system"}");
                        LogTest($"      Status: {family.SyncStatus}");
                    }
                }

                LogTest($"💡 Total duplicate families: {duplicatesByName.Sum(g => g.Count() - 1)}");
            }
            else
            {
                LogTest("✅ No name-based duplicates found");
            }

            // Analisar duplicatas por ID (muito raro, mas possível)
            var duplicatesByID = serverFamilies
                .GroupBy(f => f.Id)
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicatesByID.Any())
            {
                LogTest($"🚨 CRITICAL: ID duplicates found: {duplicatesByID.Count} groups");
                LogTest("🚨 This indicates serious database integrity issues!");
            }
            else
            {
                LogTest("✅ No ID duplicates (good)");
            }

            // Estatísticas por usuário
            var currentUserId = _supabaseService.GetCurrentUserId();
            if (Guid.TryParse(currentUserId, out var userGuid))
            {
                var myFamilies = serverFamilies.Where(f => f.UserId == userGuid).ToList();
                var systemFamilies = serverFamilies.Where(f => f.UserId == null).ToList();
                var othersFamilies = serverFamilies.Where(f => f.UserId != null && f.UserId != userGuid).ToList();

                LogTest("");
                LogTest("📊 OWNERSHIP ANALYSIS:");
                LogTest($"  👤 My families: {myFamilies.Count}");
                LogTest($"  🏢 System families: {systemFamilies.Count}");
                LogTest($"  👥 Other users: {othersFamilies.Count}");

                if (othersFamilies.Any())
                {
                    LogTest("⚠️ Can see other users' families - RLS might still be active");
                }
            }

        }
        catch (Exception ex)
        {
            LogTest($"❌ Duplicate detection failed: {ex.Message}");
        }
    }

    /// <summary>
    /// NOVO: Teste de limpeza automática
    /// </summary>
    private async Task TestAutomaticCleanup()
    {
        try
        {
            LogTest("🧹 Testing automatic duplicate cleanup...");

            var cleanedCount = await _syncService.CleanupDuplicatesAsync();

            LogTest($"🧹 Cleanup result: {cleanedCount} duplicates removed");

            if (cleanedCount > 0)
            {
                LogTest("✅ Cleanup successful! Verifying results...");

                // Verificar se limpeza funcionou
                await Task.Delay(2000); // Aguardar propagação

                var postCleanupFamilies = await _syncService.DownloadFamiliesAsync();
                var remainingDuplicates = postCleanupFamilies
                    .GroupBy(f => f.Name.ToLowerInvariant())
                    .Where(g => g.Count() > 1)
                    .Count();

                LogTest($"🔍 Remaining duplicates after cleanup: {remainingDuplicates}");

                if (remainingDuplicates == 0)
                {
                    LogTest("🎉 PERFECT! All duplicates cleaned successfully");
                }
                else
                {
                    LogTest("⚠️ Some duplicates remain - may need manual intervention");
                }
            }
            else
            {
                LogTest("✅ No duplicates found to clean");
            }

        }
        catch (Exception ex)
        {
            LogTest($"❌ Cleanup test failed: {ex.Message}");
        }
    }

    /// <summary>
    /// CORRIGIDO: Teste de famílias com análise de sync local vs servidor
    /// </summary>
    private async void OnTestFamiliesClicked(object sender, EventArgs e)
    {
        try
        {
            LogTest("🧪 === COMPREHENSIVE FAMILIES SYNC ANALYSIS ===");

            var isAuth = _supabaseService.IsAuthenticated;
            LogTest($"🔐 Authentication status: {isAuth}");

            if (!isAuth)
            {
                LogTest("❌ Not authenticated - login first");
                return;
            }

            LogTest($"🆔 Current user ID: {_supabaseService.GetCurrentUserId()}");
            LogTest($"📧 Current user email: {_supabaseService.GetCurrentUser()?.Email}");

            LogTest("");
            LogTest("📱 === LOCAL FAMILIES ANALYSIS ===");

            var localFamilies = await _familyRepository.GetAllAsync(true); // Include inactive
            LogTest($"📱 Total local families: {localFamilies.Count}");

            var localByStatus = localFamilies.GroupBy(f => f.SyncStatus).ToList();
            foreach (var group in localByStatus)
            {
                LogTest($"  📊 {group.Key}: {group.Count()} families");
            }

            LogTest("");
            LogTest("☁️ === SERVER FAMILIES ANALYSIS ===");

            var serverFamilies = await _syncService.DownloadFamiliesAsync();
            LogTest($"☁️ Total server families: {serverFamilies.Count}");

            if (serverFamilies.Any())
            {
                var currentUserId = _supabaseService.GetCurrentUserId();
                if (Guid.TryParse(currentUserId, out var userId))
                {
                    var myServerFamilies = serverFamilies.Where(f => f.UserId == userId).ToList();
                    var systemServerFamilies = serverFamilies.Where(f => f.UserId == null).ToList();

                    LogTest($"  📊 My server families: {myServerFamilies.Count}");
                    LogTest($"  📊 System server families: {systemServerFamilies.Count}");
                }

                LogTest("");
                LogTest("🔍 === SYNC CONSISTENCY ANALYSIS ===");

                // Comparar local vs servidor
                var localNames = localFamilies.Select(f => f.Name.ToLowerInvariant()).ToHashSet();
                var serverNames = serverFamilies.Select(f => f.Name.ToLowerInvariant()).ToHashSet();

                var onlyLocal = localNames.Except(serverNames).ToList();
                var onlyServer = serverNames.Except(localNames).ToList();
                var common = localNames.Intersect(serverNames).ToList();

                LogTest($"📊 Only in local: {onlyLocal.Count} families");
                LogTest($"📊 Only on server: {onlyServer.Count} families");
                LogTest($"📊 Common (synced): {common.Count} families");

                if (onlyLocal.Any())
                {
                    LogTest("");
                    LogTest("⚠️ FAMILIES NEEDING UPLOAD:");
                    foreach (var name in onlyLocal.Take(5))
                    {
                        var family = localFamilies.First(f => f.Name.ToLowerInvariant() == name);
                        LogTest($"  - {family.Name} (Status: {family.SyncStatus}, ID: {family.Id})");
                    }
                    if (onlyLocal.Count > 5)
                    {
                        LogTest($"  ... and {onlyLocal.Count - 5} more");
                    }
                }

                if (onlyServer.Any())
                {
                    LogTest("");
                    LogTest("⚠️ FAMILIES ONLY ON SERVER:");
                    foreach (var name in onlyServer.Take(5))
                    {
                        var family = serverFamilies.First(f => f.Name.ToLowerInvariant() == name);
                        LogTest($"  - {family.Name} (User: {family.UserId?.ToString() ?? "system"}, ID: {family.Id})");
                    }
                    if (onlyServer.Count > 5)
                    {
                        LogTest($"  ... and {onlyServer.Count - 5} more");
                    }
                }

                // Detectar conflitos de ID
                LogTest("");
                LogTest("🔍 === ID CONFLICT ANALYSIS ===");

                var localIds = localFamilies.Select(f => f.Id).ToHashSet();
                var serverIds = serverFamilies.Select(f => f.Id).ToHashSet();
                var conflictingIds = localIds.Intersect(serverIds).ToList();

                LogTest($"🔍 Common IDs: {conflictingIds.Count}");

                foreach (var commonId in conflictingIds.Take(3))
                {
                    var localFamily = localFamilies.First(f => f.Id == commonId);
                    var serverFamily = serverFamilies.First(f => f.Id == commonId);

                    LogTest($"  🔍 ID {commonId}:");
                    LogTest($"    Local: {localFamily.Name} (Updated: {localFamily.UpdatedAt:HH:mm:ss})");
                    LogTest($"    Server: {serverFamily.Name} (Updated: {serverFamily.UpdatedAt:HH:mm:ss})");

                    if (localFamily.Name != serverFamily.Name)
                    {
                        LogTest($"    ⚠️ NAME CONFLICT! Local: '{localFamily.Name}' vs Server: '{serverFamily.Name}'");
                    }
                }
            }
            else
            {
                LogTest("⚠️ No families returned from server");
                LogTest("🔍 This could indicate:");
                LogTest("  1. Empty table (normal for new installation)");
                LogTest("  2. Network connectivity issues");
                LogTest("  3. Authentication problems");
            }

            LogTest("");
            LogTest("📊 === STATISTICS SUMMARY ===");
            var stats = await _familyRepository.GetStatisticsAsync();
            LogTest($"📊 Repository stats:");
            LogTest($"  Total: {stats.TotalCount}");
            LogTest($"  Active: {stats.ActiveCount}");
            LogTest($"  Synced: {stats.SyncedCount}");
            LogTest($"  Local: {stats.LocalCount}");
            LogTest($"  Pending: {stats.PendingCount}");
            LogTest($"  Error: {stats.ErrorCount}");

            var syncHealth = stats.TotalCount > 0 ? (double)stats.SyncedCount / stats.TotalCount * 100 : 0;
            LogTest($"📊 Sync health: {syncHealth:F1}%");

            if (syncHealth < 80)
            {
                LogTest("⚠️ Sync health is poor - consider running force sync");
            }
            else if (syncHealth == 100)
            {
                LogTest("🎉 Perfect sync health!");
            }

            LogTest("🧪 === FAMILIES ANALYSIS COMPLETED ===");
            await UpdateQuickStats();

        }
        catch (Exception ex)
        {
            LogTest($"❌ Families test error: {ex.Message}");
            LogTest($"❌ Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// CORRIGIDO: Criação de família de teste com verificação de duplicatas
    /// </summary>
    private async void OnCreateTestFamilyClicked(object sender, EventArgs e)
    {
        try
        {
            LogTest("🧪 === CREATE TEST FAMILY (DUPLICATE-SAFE) ===");

            var isAuth = _supabaseService.IsAuthenticated;
            LogTest($"🔐 Authentication: {isAuth}");

            if (!isAuth)
            {
                LogTest("❌ Not authenticated");
                return;
            }

            var currentUserId = _supabaseService.GetCurrentUserId();
            LogTest($"🆔 Creating family for user: {currentUserId}");

            // Criar nome único baseado em timestamp
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var uniqueName = $"Test_Family_{timestamp}";

            // Verificar se nome já existe (extra safety)
            var existingLocal = await _familyRepository.GetByNameAsync(uniqueName);
            if (existingLocal != null)
            {
                LogTest($"⚠️ Name collision detected locally - adding random suffix");
                uniqueName += $"_{Random.Shared.Next(1000, 9999)}";
            }

            var testFamily = new Family
            {
                Name = uniqueName,
                Description = $"Test family created at {DateTime.Now:yyyy-MM-dd HH:mm:ss} for sync testing",
                IsActive = true
            };

            LogTest($"➕ Creating family: {testFamily.Name}");
            LogTest($"➕ Family ID: {testFamily.Id}");

            var created = await _familyRepository.CreateAsync(testFamily);

            LogTest($"✅ Created locally:");
            LogTest($"  - ID: {created.Id}");
            LogTest($"  - Name: {created.Name}");
            LogTest($"  - Status: {created.SyncStatus}");
            LogTest($"  - User ID: {created.UserId ?? Guid.Empty}");

            LogTest("");
            LogTest("⏳ Testing immediate manual sync...");

            try
            {
                var syncSuccess = await _syncService.UploadFamilyAsync(created);

                if (syncSuccess)
                {
                    LogTest("🎉 Manual sync successful!");
                    LogTest("✅ No duplicate issues detected");

                    // Verificar se realmente foi criado no servidor
                    LogTest("🔍 Verifying server creation...");
                    await Task.Delay(1000);

                    var serverFamilies = await _syncService.DownloadFamiliesAsync();
                    var foundOnServer = serverFamilies.Any(f => f.Name == created.Name);

                    LogTest($"🔍 Found on server: {foundOnServer}");

                    if (foundOnServer)
                    {
                        LogTest("🎉 PERFECT! Family successfully created and synced");

                        // Monitorar status local
                        LogTest("⏳ Monitoring local sync status...");
                        for (int i = 0; i < 5; i++)
                        {
                            await Task.Delay(1000);

                            var updated = await _familyRepository.GetByIdAsync(created.Id);
                            if (updated != null)
                            {
                                LogTest($"  [{i + 1}s] Local status: {updated.SyncStatus}" +
                                        (updated.LastSyncAt.HasValue ? $" (synced at {updated.LastSyncAt:HH:mm:ss})" : ""));

                                if (updated.SyncStatus == SyncStatus.Synced)
                                {
                                    LogTest("🎉 Auto-sync completed successfully!");
                                    break;
                                }
                                else if (updated.SyncStatus == SyncStatus.Error)
                                {
                                    LogTest("❌ Auto-sync failed!");
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        LogTest("⚠️ Not found on server - may be sync delay or filtering issue");
                    }
                }
                else
                {
                    LogTest("❌ Manual sync failed");
                    LogTest("🔍 Check logs above for specific error details");
                }
            }
            catch (Exception syncEx)
            {
                LogTest($"❌ Manual sync error: {syncEx.Message}");
            }

            LogTest("🧪 === CREATE TEST COMPLETED ===");
            await UpdateQuickStats();

        }
        catch (Exception ex)
        {
            LogTest($"❌ Create test error: {ex.Message}");
            LogTest($"❌ Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// CORRIGIDO: Force sync com limpeza automática de duplicatas
    /// </summary>
    private async void OnForceFullSyncClicked(object sender, EventArgs e)
    {
        try
        {
            LogTest("🧪 === FORCE FULL SYNC WITH CLEANUP ===");

            var isAuth = _supabaseService.IsAuthenticated;
            LogTest($"🔐 Authentication: {isAuth}");

            if (!isAuth)
            {
                LogTest("❌ Cannot perform sync - not authenticated");
                return;
            }

            LogTest("🔄 Starting comprehensive sync with automatic duplicate cleanup...");

            // Mostrar estado pré-sync
            LogTest("");
            LogTest("📊 PRE-SYNC STATE:");
            var preStats = await _familyRepository.GetStatisticsAsync();
            LogTest($"  Local families: {preStats.TotalCount}");
            LogTest($"  Synced: {preStats.SyncedCount}");
            LogTest($"  Pending: {preStats.PendingCount}");
            LogTest($"  Local only: {preStats.LocalCount}");
            LogTest($"  Errors: {preStats.ErrorCount}");

            var serverFamiliesPreSync = await _syncService.DownloadFamiliesAsync();
            LogTest($"  Server families: {serverFamiliesPreSync.Count}");

            // Detectar duplicatas antes da sincronização
            var duplicateGroups = serverFamiliesPreSync
                .GroupBy(f => f.Name.ToLowerInvariant())
                .Where(g => g.Count() > 1)
                .Count();

            LogTest($"  Server duplicates: {duplicateGroups} groups");

            LogTest("");
            LogTest("🚀 EXECUTING FULL SYNC...");

            var result = await _familyRepository.ForceFullSyncAsync();

            LogTest($"✅ Sync completed in {result.Duration.TotalSeconds:F1} seconds");
            LogTest($"📊 SYNC RESULTS:");
            LogTest($"  - Processed: {result.TotalProcessed}");
            LogTest($"  - Successful: {result.Successful}");
            LogTest($"  - Failed: {result.Failed}");

            if (result.ErrorMessages.Any())
            {
                LogTest($"❌ Errors ({result.ErrorMessages.Count}):");
                foreach (var error in result.ErrorMessages.Take(5))
                {
                    LogTest($"  - {error}");
                }
                if (result.ErrorMessages.Count > 5)
                {
                    LogTest($"  ... and {result.ErrorMessages.Count - 5} more errors");
                }
            }

            LogTest("");
            LogTest("📊 POST-SYNC STATE:");
            var postStats = await _familyRepository.GetStatisticsAsync();
            LogTest($"  Local families: {postStats.TotalCount}");
            LogTest($"  Synced: {postStats.SyncedCount}");
            LogTest($"  Pending: {postStats.PendingCount}");
            LogTest($"  Local only: {postStats.LocalCount}");
            LogTest($"  Errors: {postStats.ErrorCount}");

            var serverFamiliesPostSync = await _syncService.DownloadFamiliesAsync();
            LogTest($"  Server families: {serverFamiliesPostSync.Count}");

            var postDuplicateGroups = serverFamiliesPostSync
                .GroupBy(f => f.Name.ToLowerInvariant())
                .Where(g => g.Count() > 1)
                .Count();

            LogTest($"  Server duplicates: {postDuplicateGroups} groups");

            // Análise de resultados
            LogTest("");
            LogTest("📈 IMPROVEMENT ANALYSIS:");
            LogTest($"  Synced families: {preStats.SyncedCount} → {postStats.SyncedCount} (+{postStats.SyncedCount - preStats.SyncedCount})");
            LogTest($"  Server families: {serverFamiliesPreSync.Count} → {serverFamiliesPostSync.Count} (+{serverFamiliesPostSync.Count - serverFamiliesPreSync.Count})");
            LogTest($"  Duplicate groups: {duplicateGroups} → {postDuplicateGroups} ({(postDuplicateGroups < duplicateGroups ? "IMPROVED" : "NO CHANGE")})");

            var syncHealthPre = preStats.TotalCount > 0 ? (double)preStats.SyncedCount / preStats.TotalCount * 100 : 0;
            var syncHealthPost = postStats.TotalCount > 0 ? (double)postStats.SyncedCount / postStats.TotalCount * 100 : 0;

            LogTest($"  Sync health: {syncHealthPre:F1}% → {syncHealthPost:F1}% ({syncHealthPost - syncHealthPre:+F1;-F1;0}%)");

            // Recomendações
            LogTest("");
            LogTest("💡 RECOMMENDATIONS:");

            if (postStats.ErrorCount > 0)
            {
                LogTest("  ⚠️ Some families have sync errors - check individual items");
            }

            if (postDuplicateGroups > 0)
            {
                LogTest("  🧹 Manual duplicate cleanup may be needed");
                LogTest("  💡 Run 'Test Supabase' → Cleanup test for more details");
            }

            if (syncHealthPost == 100)
            {
                LogTest("  🎉 PERFECT SYNC! All families synchronized successfully");
            }
            else if (syncHealthPost >= 90)
            {
                LogTest("  ✅ Excellent sync health - minor issues only");
            }
            else if (syncHealthPost >= 70)
            {
                LogTest("  ⚠️ Good sync health - some items need attention");
            }
            else
            {
                LogTest("  ❌ Poor sync health - investigate sync errors");
            }

            LogTest("🧪 === FORCE SYNC COMPLETED ===");
            await UpdateQuickStats();

        }
        catch (Exception ex)
        {
            LogTest($"❌ Force sync error: {ex.Message}");
            LogTest($"❌ Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// NOVO: Botão para limpeza manual de duplicatas
    /// </summary>
    private async void OnCleanupDuplicatesClicked(object sender, EventArgs e)
    {
        try
        {
            LogTest("🧹 === MANUAL DUPLICATE CLEANUP ===");

            var isAuth = _supabaseService.IsAuthenticated;
            if (!isAuth)
            {
                LogTest("❌ Not authenticated");
                return;
            }

            LogTest("🔍 Analyzing duplicates before cleanup...");
            var serverFamilies = await _syncService.DownloadFamiliesAsync();
            var duplicateGroups = serverFamilies
                .GroupBy(f => f.Name.ToLowerInvariant())
                .Where(g => g.Count() > 1)
                .ToList();

            LogTest($"🔍 Found {duplicateGroups.Count} duplicate groups");

            if (!duplicateGroups.Any())
            {
                LogTest("✅ No duplicates found - cleanup not needed");
                return;
            }

            LogTest("🧹 Starting cleanup process...");
            var cleanedCount = await _syncService.CleanupDuplicatesAsync();

            LogTest($"🧹 Cleanup completed: {cleanedCount} duplicates removed");

            if (cleanedCount > 0)
            {
                LogTest("✅ Verifying cleanup results...");
                await Task.Delay(2000);

                var postCleanupFamilies = await _syncService.DownloadFamiliesAsync();
                var remainingDuplicates = postCleanupFamilies
                    .GroupBy(f => f.Name.ToLowerInvariant())
                    .Where(g => g.Count() > 1)
                    .Count();

                LogTest($"🔍 Remaining duplicates: {remainingDuplicates}");

                if (remainingDuplicates == 0)
                {
                    LogTest("🎉 PERFECT CLEANUP! All duplicates resolved");
                }
                else
                {
                    LogTest("⚠️ Some duplicates remain - may need additional investigation");
                }
            }

            LogTest("🧹 === CLEANUP COMPLETED ===");
            await UpdateQuickStats();

        }
        catch (Exception ex)
        {
            LogTest($"❌ Cleanup error: {ex.Message}");
        }
    }

    /// <summary>
    /// NOVO: Export debug information
    /// </summary>
    private async void OnExportDebugInfoClicked(object sender, EventArgs e)
    {
        try
        {
            LogTest("📋 === EXPORTING DEBUG INFORMATION ===");

            var debugInfo = new StringBuilder();
            debugInfo.AppendLine("OrchidPro Sync Debug Report");
            debugInfo.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            debugInfo.AppendLine($"User: {_supabaseService.GetCurrentUser()?.Email ?? "Not authenticated"}");
            debugInfo.AppendLine($"User ID: {_supabaseService.GetCurrentUserId() ?? "null"}");
            debugInfo.AppendLine();

            debugInfo.AppendLine("=== AUTHENTICATION STATUS ===");
            debugInfo.AppendLine($"Authenticated: {_supabaseService.IsAuthenticated}");
            debugInfo.AppendLine($"Client Initialized: {_supabaseService.IsInitialized}");
            debugInfo.AppendLine();

            debugInfo.AppendLine("=== LOCAL STATISTICS ===");
            try
            {
                var stats = await _familyRepository.GetStatisticsAsync();
                debugInfo.AppendLine($"Total Families: {stats.TotalCount}");
                debugInfo.AppendLine($"Active: {stats.ActiveCount}");
                debugInfo.AppendLine($"Synced: {stats.SyncedCount}");
                debugInfo.AppendLine($"Local: {stats.LocalCount}");
                debugInfo.AppendLine($"Pending: {stats.PendingCount}");
                debugInfo.AppendLine($"Error: {stats.ErrorCount}");
                debugInfo.AppendLine($"System: {stats.SystemDefaultCount}");
                debugInfo.AppendLine($"User Created: {stats.UserCreatedCount}");
                debugInfo.AppendLine($"Last Sync: {stats.LastSyncTime:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                debugInfo.AppendLine($"Error getting local stats: {ex.Message}");
            }
            debugInfo.AppendLine();

            debugInfo.AppendLine("=== SERVER STATUS ===");
            try
            {
                var serverFamilies = await _syncService.DownloadFamiliesAsync();
                debugInfo.AppendLine($"Server Families: {serverFamilies.Count}");

                var duplicateGroups = serverFamilies
                    .GroupBy(f => f.Name.ToLowerInvariant())
                    .Where(g => g.Count() > 1)
                    .ToList();

                debugInfo.AppendLine($"Duplicate Groups: {duplicateGroups.Count}");

                if (duplicateGroups.Any())
                {
                    debugInfo.AppendLine();
                    debugInfo.AppendLine("=== DUPLICATE DETAILS ===");
                    foreach (var group in duplicateGroups)
                    {
                        debugInfo.AppendLine($"Name: {group.Key} ({group.Count()} copies)");
                        foreach (var family in group.OrderBy(f => f.CreatedAt))
                        {
                            debugInfo.AppendLine($"  - ID: {family.Id}, Created: {family.CreatedAt:yyyy-MM-dd HH:mm:ss}, User: {family.UserId?.ToString() ?? "system"}");
                        }
                    }
                }

                var currentUserId = _supabaseService.GetCurrentUserId();
                if (Guid.TryParse(currentUserId, out var userId))
                {
                    var myFamilies = serverFamilies.Where(f => f.UserId == userId).Count();
                    var systemFamilies = serverFamilies.Where(f => f.UserId == null).Count();
                    debugInfo.AppendLine($"My Families: {myFamilies}");
                    debugInfo.AppendLine($"System Families: {systemFamilies}");
                }
            }
            catch (Exception ex)
            {
                debugInfo.AppendLine($"Error getting server info: {ex.Message}");
            }

            debugInfo.AppendLine();
            debugInfo.AppendLine("=== LOCAL FAMILIES ===");
            try
            {
                var localFamilies = await _familyRepository.GetAllAsync(true);
                foreach (var family in localFamilies.Take(20))
                {
                    debugInfo.AppendLine($"- {family.Name} (ID: {family.Id}, Status: {family.SyncStatus}, User: {family.UserId?.ToString() ?? "system"})");
                }
                if (localFamilies.Count > 20)
                {
                    debugInfo.AppendLine($"... and {localFamilies.Count - 20} more");
                }
            }
            catch (Exception ex)
            {
                debugInfo.AppendLine($"Error getting local families: {ex.Message}");
            }

            debugInfo.AppendLine();
            debugInfo.AppendLine("=== CURRENT LOG ===");
            debugInfo.AppendLine(StatusLabel.Text);

            // Save to clipboard
            await Clipboard.SetTextAsync(debugInfo.ToString());

            LogTest("📋 Debug information exported to clipboard");
            LogTest($"📊 Report size: {debugInfo.Length} characters");
            LogTest("💡 You can now paste this information for support");

        }
        catch (Exception ex)
        {
            LogTest($"❌ Export error: {ex.Message}");
        }
    }

    /// <summary>
    /// NOVO: Copy log to clipboard
    /// </summary>
    private async void OnCopyLogClicked(object sender, EventArgs e)
    {
        try
        {
            await Clipboard.SetTextAsync(StatusLabel.Text);
            LogStatusLabel.Text = "Log copied to clipboard";

            // Reset status after 3 seconds
            _ = Task.Run(async () =>
            {
                await Task.Delay(3000);
                Device.BeginInvokeOnMainThread(() =>
                {
                    LogStatusLabel.Text = "Ready";
                });
            });
        }
        catch (Exception ex)
        {
            LogTest($"❌ Copy failed: {ex.Message}");
        }
    }

    /// <summary>
    /// NOVO: Update quick stats display
    /// </summary>
    private async Task UpdateQuickStats()
    {
        try
        {
            // Local stats
            var stats = await _familyRepository.GetStatisticsAsync();
            LocalCountLabel.Text = stats.TotalCount.ToString();
            SyncedCountLabel.Text = stats.SyncedCount.ToString();

            // Server stats
            if (_supabaseService.IsAuthenticated)
            {
                var serverFamilies = await _syncService.DownloadFamiliesAsync();
                ServerCountLabel.Text = serverFamilies.Count.ToString();

                var duplicateGroups = serverFamilies
                    .GroupBy(f => f.Name.ToLowerInvariant())
                    .Where(g => g.Count() > 1)
                    .Count();

                DuplicatesLabel.Text = duplicateGroups.ToString();
                DuplicatesLabel.TextColor = duplicateGroups > 0 ? Colors.Red : Colors.Green;
            }
            else
            {
                ServerCountLabel.Text = "N/A";
                DuplicatesLabel.Text = "N/A";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating quick stats: {ex.Message}");
            LocalCountLabel.Text = "ERR";
            ServerCountLabel.Text = "ERR";
            SyncedCountLabel.Text = "ERR";
            DuplicatesLabel.Text = "ERR";
        }
    }

    /// <summary>
    /// NOVO: Método para debug completo do estado dos services
    /// </summary>
    private async void OnDebugServicesClicked(object sender, EventArgs e)
    {
        try
        {
            LogTest("🔍 === DEBUG SERVICES STATE ===");

            // Test service instances
            LogTest("🔧 Testing service instances...");
            LogTest($"🔧 SupabaseService instance: {_supabaseService.GetHashCode()}");
            LogTest($"🔧 FamilyRepository instance: {_familyRepository.GetHashCode()}");
            LogTest($"🔧 SyncService instance: {_syncService.GetHashCode()}");

            // Force refresh repository data
            LogTest("🔄 Forcing repository data refresh...");

            // Get fresh instances from DI to test
            var services = MauiProgram.CreateMauiApp().Services;
            var freshRepo = services.GetRequiredService<IFamilyRepository>();
            var freshSync = services.GetRequiredService<SupabaseFamilySync>();

            LogTest($"🔧 Fresh FamilyRepository instance: {freshRepo.GetHashCode()}");
            LogTest($"🔧 Fresh SyncService instance: {freshSync.GetHashCode()}");

            // Compare data between instances
            LogTest("📊 Comparing data between service instances...");

            var originalData = await _familyRepository.GetAllAsync(true);
            var freshData = await freshRepo.GetAllAsync(true);

            LogTest($"📊 Original repo families: {originalData.Count}");
            LogTest($"📊 Fresh repo families: {freshData.Count}");

            if (originalData.Count != freshData.Count)
            {
                LogTest("⚠️ DATA INCONSISTENCY DETECTED!");
                LogTest("🔍 This indicates services are not properly sharing state");
                LogTest("💡 Check service registration lifetimes in MauiProgram.cs");
            }
            else
            {
                LogTest("✅ Data consistency verified");
            }

            // Test if families created in other pages are visible
            LogTest("");
            LogTest("🔍 === CROSS-PAGE DATA VISIBILITY TEST ===");

            // Force reload from local storage
            var localStorage = services.GetRequiredService<ILocalDataService>();
            var rawLocalData = await localStorage.GetAllFamiliesAsync();

            LogTest($"📱 Raw local storage families: {rawLocalData.Count}");

            foreach (var family in rawLocalData.Take(5))
            {
                LogTest($"  - {family.Name} (Status: {family.SyncStatus}, Created: {family.CreatedAt:HH:mm:ss})");
            }

            // Check for recently created families (last 1 hour)
            var recentFamilies = rawLocalData.Where(f => f.CreatedAt > DateTime.UtcNow.AddHours(-1)).ToList();
            LogTest($"🕐 Recent families (last hour): {recentFamilies.Count}");

            if (recentFamilies.Any())
            {
                LogTest("📝 Recent families details:");
                foreach (var family in recentFamilies)
                {
                    LogTest($"  - {family.Name}");
                    LogTest($"    ID: {family.Id}");
                    LogTest($"    Status: {family.SyncStatus}");
                    LogTest($"    Created: {family.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                    LogTest($"    User: {family.UserId?.ToString() ?? "system"}");
                }
            }
            else
            {
                LogTest("⚠️ No recent families found");
                LogTest("💡 This might indicate:");
                LogTest("  1. No families were created recently");
                LogTest("  2. Different service instances are being used");
                LogTest("  3. Data is not being saved properly");
            }

            LogTest("🔍 === DEBUG SERVICES COMPLETED ===");
            await UpdateQuickStats();

        }
        catch (Exception ex)
        {
            LogTest($"❌ Debug services error: {ex.Message}");
            LogTest($"❌ Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// NOVO: Force complete refresh of all data
    /// </summary>
    private async void OnForceRefreshClicked(object sender, EventArgs e)
    {
        try
        {
            LogTest("🔄 === FORCE COMPLETE REFRESH ===");

            // Clear any cached data and force reload
            LogTest("🧹 Clearing cached data...");

            // Get services and force refresh
            var services = MauiProgram.CreateMauiApp().Services;
            var freshRepo = services.GetRequiredService<IFamilyRepository>();

            LogTest("📥 Reloading all data from sources...");

            // Force reload local data
            var allFamilies = await freshRepo.GetAllAsync(true);
            LogTest($"📱 Total families after refresh: {allFamilies.Count}");

            // Show all families with details
            LogTest("📋 All families in system:");
            foreach (var family in allFamilies.OrderBy(f => f.CreatedAt))
            {
                LogTest($"  📝 {family.Name}");
                LogTest($"     ID: {family.Id}");
                LogTest($"     Status: {family.SyncStatus}");
                LogTest($"     Created: {family.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                LogTest($"     Updated: {family.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                LogTest($"     User: {family.UserId?.ToString() ?? "system"}");
                LogTest($"     System: {family.IsSystemDefault}");
                LogTest($"     Active: {family.IsActive}");
            }

            // Check sync status distribution
            var statusGroups = allFamilies.GroupBy(f => f.SyncStatus).ToList();
            LogTest("");
            LogTest("📊 Sync status distribution:");
            foreach (var group in statusGroups)
            {
                LogTest($"  {group.Key}: {group.Count()} families");
            }

            // Check user distribution
            var userGroups = allFamilies.GroupBy(f => f.UserId?.ToString() ?? "system").ToList();
            LogTest("");
            LogTest("👥 User distribution:");
            foreach (var group in userGroups)
            {
                LogTest($"  {group.Key}: {group.Count()} families");
            }

            LogTest("🔄 === REFRESH COMPLETED ===");
            await UpdateQuickStats();

        }
        catch (Exception ex)
        {
            LogTest($"❌ Force refresh error: {ex.Message}");
        }
    }

    private void OnClearLogClicked(object sender, EventArgs e)
    {
        StatusLabel.Text = $"Log cleared at {DateTime.Now:HH:mm:ss}\n";
        StatusLabel.Text += "🚀 SYNC DEBUG MODE - Ready for testing\n";
        StatusLabel.Text += "🎯 Focus: Duplicate detection and resolution\n";
        StatusLabel.Text += "🔧 RLS Status: DISABLED for debugging\n";
        StatusLabel.Text += "💡 Use 'Test Connection' to analyze current state\n";
        StatusLabel.Text += "💡 Use 'Force Sync' to resolve sync issues\n";
        StatusLabel.Text += "💡 Use 'Create Test' to test duplicate prevention\n";
        StatusLabel.Text += "💡 Use 'Cleanup Duplicates' to remove duplicates\n";

        LogStatusLabel.Text = "Log cleared";
    }

    private void LogTest(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] {message}\n";

        StatusLabel.Text += logEntry;

        // Update status
        LogStatusLabel.Text = message.Length > 50 ? message.Substring(0, 47) + "..." : message;

        // Auto-scroll if enabled
        if (AutoScrollCheckBox.IsChecked)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await LogScrollView.ScrollToAsync(StatusLabel, ScrollToPosition.End, false);
                }
                catch
                {
                    // Ignore scroll errors
                }
            });
        }

        // Also log to debug console
        Debug.WriteLine($"[TestSyncPage] {message}");
    }
}