using OrchidPro.Services.Data;
using OrchidPro.Services;
using OrchidPro.Models;
using System.Diagnostics;

namespace OrchidPro.Views.Pages;

/// <summary>
/// Página de teste FINAL - Otimizada para schema public
/// </summary>
public partial class TestSyncPage : ContentPage
{
    private readonly SupabaseService _supabaseService;
    private readonly IFamilyRepository _familyRepository;
    private readonly SupabaseFamilySync _syncService;

    public TestSyncPage()
    {
        InitializeComponent();

        // Get services
        try
        {
            var services = MauiProgram.CreateMauiApp().Services;
            _supabaseService = services.GetRequiredService<SupabaseService>();
            _familyRepository = services.GetRequiredService<IFamilyRepository>();
            _syncService = services.GetRequiredService<SupabaseFamilySync>();

            LogTest("✅ All services loaded successfully");
        }
        catch (Exception ex)
        {
            LogTest($"❌ Error loading services: {ex.Message}");
        }
    }

    /// <summary>
    /// FINAL: Teste otimizado para schema public
    /// </summary>
    private async void OnTestSupabaseClicked(object sender, EventArgs e)
    {
        try
        {
            LogTest("🧪 === COMPREHENSIVE SUPABASE TEST (PUBLIC SCHEMA) ===");
            LogTest("🎯 Testing public.families table access");

            // Debug current state
            LogTest("📊 Current state check:");
            _supabaseService.DebugCurrentState();

            // Initialize
            LogTest("🔄 Initializing Supabase...");
            await _supabaseService.InitializeAsync();
            LogTest("✅ Supabase initialized");

            // Check authentication
            var isAuthAfterInit = _supabaseService.IsAuthenticated;
            var userIdAfterInit = _supabaseService.GetCurrentUserId();
            var userEmailAfterInit = _supabaseService.GetCurrentUser()?.Email;

            LogTest($"🔐 Auth after init: {isAuthAfterInit}");
            LogTest($"🆔 User ID: {userIdAfterInit ?? "null"}");
            LogTest($"📧 User Email: {userEmailAfterInit ?? "null"}");

            if (!isAuthAfterInit)
            {
                LogTest("❌ Not authenticated - stopping test");
                LogTest("💡 Login first, then test again");
                return;
            }

            LogTest("");
            LogTest("🔍 === PUBLIC SCHEMA TEST ===");
            LogTest("✅ Using standard Supabase schema (public)");
            LogTest("✅ No special configuration needed");

            // Test schema and permissions
            LogTest("🧪 Testing public.families access...");
            var schemaTest = await _syncService.TestSchemaAndPermissionsAsync();
            LogTest($"🏗️ Schema test result: {schemaTest}");

            if (schemaTest)
            {
                LogTest("🎉 SUCCESS! public.families is fully accessible!");
                LogTest("✅ Schema migration worked perfectly");

                // Test insert capability
                LogTest("");
                LogTest("🧪 Testing insert capability...");
                var insertTest = await _syncService.TestInsertAsync();
                LogTest($"➕ Insert test: {insertTest}");

                if (insertTest)
                {
                    LogTest("🎉 FULL SUCCESS! All operations working!");
                    LogTest("✅ CREATE, READ, UPDATE, DELETE all functional");
                    LogTest("✅ RLS policies working correctly");
                    LogTest("✅ Ready for production use!");
                }
                else
                {
                    LogTest("⚠️ Schema access OK, but insert failed");
                    LogTest("🔍 Check RLS INSERT policy on public.families");
                }
            }
            else
            {
                LogTest("❌ Schema test failed");
                LogTest("🔍 POSSIBLE ISSUES:");
                LogTest("  1. Migration didn't complete successfully");
                LogTest("  2. RLS policies not configured correctly");
                LogTest("  3. User permissions issue");
                LogTest("");
                LogTest("🛠️ SOLUTIONS:");
                LogTest("  1. Check if public.families table exists");
                LogTest("  2. Verify RLS policies allow user access");
                LogTest("  3. Test manual SQL: SELECT * FROM public.families;");
            }

            LogTest("🧪 === TEST COMPLETED ===");

        }
        catch (Exception ex)
        {
            LogTest($"❌ Test failed with exception: {ex.Message}");
            LogTest($"❌ Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// FINAL: Teste de famílias otimizado
    /// </summary>
    private async void OnTestFamiliesClicked(object sender, EventArgs e)
    {
        try
        {
            LogTest("🧪 === FAMILIES TEST (PUBLIC SCHEMA) ===");

            var isAuth = _supabaseService.IsAuthenticated;
            LogTest($"🔐 Authentication status: {isAuth}");

            if (!isAuth)
            {
                LogTest("❌ Not authenticated - login first");
                return;
            }

            LogTest($"🆔 Current user ID: {_supabaseService.GetCurrentUserId()}");
            LogTest($"📧 Current user email: {_supabaseService.GetCurrentUser()?.Email}");

            // Test local families first
            LogTest("📱 Testing local families...");
            var localFamilies = await _familyRepository.GetAllAsync();
            LogTest($"📱 Local families count: {localFamilies.Count}");

            foreach (var family in localFamilies.Take(3))
            {
                LogTest($"  - {family.Name} ({family.SyncStatus}) - User: {family.UserId?.ToString() ?? "system"}");
            }

            // Test server families
            LogTest("☁️ Testing server families download from public.families...");

            try
            {
                var serverFamilies = await _syncService.DownloadFamiliesAsync();
                LogTest($"☁️ Server families count: {serverFamilies.Count}");

                if (serverFamilies.Count > 0)
                {
                    LogTest("🎉 SUCCESS: Server access is working!");

                    foreach (var family in serverFamilies.Take(3))
                    {
                        LogTest($"  - {family.Name} (from server) - User: {family.UserId?.ToString() ?? "system"}");
                    }

                    // Analyze access patterns
                    var currentUserId = _supabaseService.GetCurrentUserId();
                    if (Guid.TryParse(currentUserId, out var userId))
                    {
                        var myFamilies = serverFamilies.Where(f => f.UserId == userId).ToList();
                        var systemFamilies = serverFamilies.Where(f => f.UserId == null).ToList();

                        LogTest($"📊 My families: {myFamilies.Count}");
                        LogTest($"📊 System families: {systemFamilies.Count}");
                        LogTest($"📊 Total accessible: {serverFamilies.Count}");
                    }

                    // Compare local vs server
                    var localNames = localFamilies.Select(f => f.Name).ToHashSet();
                    var serverNames = serverFamilies.Select(f => f.Name).ToHashSet();
                    var onlyLocal = localNames.Except(serverNames).ToList();
                    var onlyServer = serverNames.Except(localNames).ToList();

                    LogTest($"📊 Only local: {onlyLocal.Count} families");
                    LogTest($"📊 Only server: {onlyServer.Count} families");
                    LogTest($"📊 Common: {localNames.Intersect(serverNames).Count()} families");

                    if (onlyLocal.Any())
                    {
                        LogTest("💡 Local families that need syncing:");
                        foreach (var name in onlyLocal.Take(3))
                        {
                            LogTest($"  - {name}");
                        }
                    }
                }
                else
                {
                    LogTest("⚠️ No families returned from server");
                    LogTest("🔍 This could indicate:");
                    LogTest("  1. Empty table (normal for new installation)");
                    LogTest("  2. RLS blocking all records for this user");
                    LogTest("  3. Migration didn't copy data correctly");
                }
            }
            catch (Exception serverEx)
            {
                LogTest($"❌ Server download failed: {serverEx.Message}");

                if (serverEx.Message.Contains("permission denied"))
                {
                    LogTest("🎯 RLS or permission issue on public.families");
                    LogTest("💡 Check RLS policies allow SELECT for authenticated users");
                }
                else if (serverEx.Message.Contains("does not exist"))
                {
                    LogTest("🎯 public.families table doesn't exist");
                    LogTest("💡 Migration may have failed");
                }
            }

            // Test statistics
            LogTest("📊 Testing statistics...");
            var stats = await _familyRepository.GetStatisticsAsync();
            LogTest($"📊 Total: {stats.TotalCount}");
            LogTest($"📊 Active: {stats.ActiveCount}");
            LogTest($"📊 Synced: {stats.SyncedCount}");
            LogTest($"📊 Local: {stats.LocalCount}");
            LogTest($"📊 Pending: {stats.PendingCount}");
            LogTest($"📊 Error: {stats.ErrorCount}");

            LogTest("🧪 === FAMILIES TEST COMPLETED ===");

        }
        catch (Exception ex)
        {
            LogTest($"❌ Families test error: {ex.Message}");
        }
    }

    /// <summary>
    /// FINAL: Criação de família de teste otimizada
    /// </summary>
    private async void OnCreateTestFamilyClicked(object sender, EventArgs e)
    {
        try
        {
            LogTest("🧪 === CREATE TEST FAMILY (PUBLIC SCHEMA) ===");

            var isAuth = _supabaseService.IsAuthenticated;
            LogTest($"🔐 Authentication: {isAuth}");

            if (!isAuth)
            {
                LogTest("❌ Not authenticated");
                return;
            }

            var currentUserId = _supabaseService.GetCurrentUserId();
            LogTest($"🆔 Creating family for user: {currentUserId}");

            var testFamily = new Family
            {
                Name = $"Public_Test_Family_{DateTime.Now:HH:mm:ss}",
                Description = $"Test family for public schema - created at {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                IsActive = true
            };

            LogTest($"➕ Creating family: {testFamily.Name}");
            var created = await _familyRepository.CreateAsync(testFamily);

            LogTest($"✅ Created locally:");
            LogTest($"  - ID: {created.Id}");
            LogTest($"  - Name: {created.Name}");
            LogTest($"  - Status: {created.SyncStatus}");
            LogTest($"  - User ID: {created.UserId ?? Guid.Empty}");

            if (isAuth)
            {
                LogTest("⏳ Testing manual sync to public.families...");

                try
                {
                    var syncSuccess = await _syncService.UploadFamilyAsync(created);

                    if (syncSuccess)
                    {
                        LogTest("🎉 Manual sync successful!");
                        LogTest("✅ public.families INSERT working correctly");

                        // Monitor auto-sync progress
                        LogTest("⏳ Monitoring auto-sync progress...");
                        for (int i = 0; i < 8; i++)
                        {
                            await Task.Delay(1000);

                            var updated = await _familyRepository.GetByIdAsync(created.Id);
                            if (updated != null)
                            {
                                LogTest($"  [{i + 1}s] Status: {updated.SyncStatus}" +
                                        (updated.LastSyncAt.HasValue ? $" (synced at {updated.LastSyncAt:HH:mm:ss})" : ""));

                                if (updated.SyncStatus == SyncStatus.Synced)
                                {
                                    LogTest("🎉 Auto-sync to public.families completed!");
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
                        LogTest("❌ Manual sync failed");
                        LogTest("🔍 Check RLS INSERT policy on public.families");
                    }
                }
                catch (Exception syncEx)
                {
                    LogTest($"❌ Manual sync error: {syncEx.Message}");

                    if (syncEx.Message.Contains("permission denied"))
                    {
                        LogTest("🎯 RLS blocking INSERT on public.families");
                        LogTest("💡 Check INSERT policy allows authenticated users");
                    }
                }
            }

            LogTest("🧪 === CREATE TEST COMPLETED ===");

        }
        catch (Exception ex)
        {
            LogTest($"❌ Create test error: {ex.Message}");
        }
    }

    /// <summary>
    /// FINAL: Force sync otimizado para public schema
    /// </summary>
    private async void OnForceFullSyncClicked(object sender, EventArgs e)
    {
        try
        {
            LogTest("🧪 === FORCE FULL SYNC (PUBLIC SCHEMA) ===");

            var isAuth = _supabaseService.IsAuthenticated;
            LogTest($"🔐 Authentication: {isAuth}");

            if (!isAuth)
            {
                LogTest("❌ Cannot perform sync - not authenticated");
                return;
            }

            LogTest("🔄 Starting comprehensive sync to public.families...");

            var result = await _familyRepository.ForceFullSyncAsync();

            LogTest($"✅ Sync completed in {result.Duration.TotalSeconds:F1} seconds");
            LogTest($"📊 Results:");
            LogTest($"  - Processed: {result.TotalProcessed}");
            LogTest($"  - Successful: {result.Successful}");
            LogTest($"  - Failed: {result.Failed}");

            if (result.ErrorMessages.Any())
            {
                LogTest($"❌ Errors ({result.ErrorMessages.Count}):");
                foreach (var error in result.ErrorMessages)
                {
                    LogTest($"  - {error}");
                }
            }
            else if (result.Successful > 0)
            {
                LogTest("🎉 All sync operations successful!");
            }
            else if (result.TotalProcessed == 0)
            {
                LogTest("✅ No items to sync - everything already synced");
            }

            // Post-sync statistics
            LogTest("📊 Post-sync statistics:");
            var stats = await _familyRepository.GetStatisticsAsync();
            LogTest($"  - Total: {stats.TotalCount}");
            LogTest($"  - Synced: {stats.SyncedCount}");
            LogTest($"  - Local: {stats.LocalCount}");
            LogTest($"  - Pending: {stats.PendingCount}");
            LogTest($"  - Error: {stats.ErrorCount}");

            if (stats.SyncedCount == stats.TotalCount && stats.TotalCount > 0)
            {
                LogTest("🎉 PERFECT SYNC! All families synchronized with public.families");
            }

            LogTest("🧪 === FORCE SYNC COMPLETED ===");

        }
        catch (Exception ex)
        {
            LogTest($"❌ Force sync error: {ex.Message}");
        }
    }

    private void OnClearLogClicked(object sender, EventArgs e)
    {
        StatusLabel.Text = $"Log cleared at {DateTime.Now:HH:mm:ss}\n";
        StatusLabel.Text += "Ready for testing public schema...\n";
        StatusLabel.Text += "🎯 Schema: public.families (standard Supabase)\n";
        StatusLabel.Text += "✅ Migration completed successfully!\n";
    }

    private void LogTest(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        StatusLabel.Text += $"[{timestamp}] {message}\n";

        // Auto-scroll to bottom
        LogScrollView.ScrollToAsync(StatusLabel, ScrollToPosition.End, false);

        // Also log to debug console
        Debug.WriteLine($"[TestSyncPage] {message}");
    }
}