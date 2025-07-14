using OrchidPro.Services.Data;
using OrchidPro.Services;
using OrchidPro.Models;
using System.Diagnostics;

namespace OrchidPro.Views.Pages;

/// <summary>
/// Página de teste MELHORADA com diagnósticos completos
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
    /// MELHORADO: Teste completo do Supabase com diagnósticos detalhados
    /// </summary>
    private async void OnTestSupabaseClicked(object sender, EventArgs e)
    {
        try
        {
            LogTest("🧪 === COMPREHENSIVE SUPABASE TEST ===");

            // Step 1: Debug current state BEFORE anything
            LogTest("📊 Current state check:");
            _supabaseService.DebugCurrentState();

            // Step 2: Initialize (should be safe to call multiple times)
            LogTest("🔄 Initializing Supabase...");
            await _supabaseService.InitializeAsync();
            LogTest("✅ Supabase initialized");

            // Step 3: Check authentication IMMEDIATELY after init
            var isAuthAfterInit = _supabaseService.IsAuthenticated;
            var userIdAfterInit = _supabaseService.GetCurrentUserId();
            LogTest($"🔐 Auth after init: {isAuthAfterInit}");
            LogTest($"🆔 User ID after init: {userIdAfterInit ?? "null"}");

            // Step 4: Test basic connectivity
            LogTest("🌐 Testing basic connectivity...");
            var basicConnection = await _supabaseService.TestSyncConnectionAsync();
            LogTest($"🌐 Basic connection: {basicConnection}");

            // Step 5: Test families table specifically
            LogTest("🏷️ Testing families table access...");
            var familiesConnection = await _syncService.TestConnectionAsync();
            LogTest($"🏷️ Families table: {familiesConnection}");

            // Step 6: If authenticated, test operations
            if (isAuthAfterInit)
            {
                LogTest("🧪 Testing authenticated operations...");

                // Test download
                try
                {
                    var downloadTest = await _syncService.DownloadFamiliesAsync();
                    LogTest($"📥 Download test: {downloadTest.Count} families found");
                }
                catch (Exception ex)
                {
                    LogTest($"❌ Download test failed: {ex.Message}");
                }

                // Test insert capability
                try
                {
                    var insertTest = await _syncService.TestInsertAsync();
                    LogTest($"➕ Insert test: {insertTest}");
                }
                catch (Exception ex)
                {
                    LogTest($"❌ Insert test failed: {ex.Message}");
                }
            }
            else
            {
                LogTest("⚠️ Not authenticated - skipping authenticated tests");

                // Detailed session debugging
                LogTest("🔍 Session debugging:");
                var savedSession = Preferences.Get("supabase_session", null);
                LogTest($"📱 Saved session exists: {!string.IsNullOrEmpty(savedSession)}");

                if (!string.IsNullOrEmpty(savedSession))
                {
                    LogTest($"📱 Session length: {savedSession.Length} characters");
                    LogTest($"📱 Session preview: {savedSession[..Math.Min(100, savedSession.Length)]}...");

                    // Try to restore session manually
                    LogTest("🔄 Attempting manual session restore...");
                    var restoreResult = await _supabaseService.RestoreSessionAsync();
                    LogTest($"🔄 Restore result: {restoreResult}");

                    if (restoreResult)
                    {
                        LogTest("✅ Session restored! Checking auth status...");
                        var authAfterRestore = _supabaseService.IsAuthenticated;
                        var userAfterRestore = _supabaseService.GetCurrentUserId();
                        LogTest($"🔐 Auth after restore: {authAfterRestore}");
                        LogTest($"🆔 User after restore: {userAfterRestore ?? "null"}");
                    }
                }
                else
                {
                    LogTest("❌ No saved session found - user needs to login again");
                }
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
    /// MELHORADO: Teste de famílias com mais detalhes
    /// </summary>
    private async void OnTestFamiliesClicked(object sender, EventArgs e)
    {
        try
        {
            LogTest("🧪 === FAMILIES TEST ===");

            // Check auth first
            var isAuth = _supabaseService.IsAuthenticated;
            LogTest($"🔐 Authentication status: {isAuth}");

            if (!isAuth)
            {
                LogTest("❌ Not authenticated - some tests will be limited");
            }

            // Test 1: Local families
            LogTest("📱 Testing local families...");
            var localFamilies = await _familyRepository.GetAllAsync();
            LogTest($"📱 Local families count: {localFamilies.Count}");

            foreach (var family in localFamilies.Take(5))
            {
                LogTest($"  - {family.Name} ({family.SyncStatus}) - Created: {family.CreatedAt:yyyy-MM-dd}");
            }

            // Test 2: Server families (only if authenticated)
            if (isAuth)
            {
                LogTest("☁️ Testing server families download...");
                var serverFamilies = await _syncService.DownloadFamiliesAsync();
                LogTest($"☁️ Server families count: {serverFamilies.Count}");

                foreach (var family in serverFamilies.Take(5))
                {
                    LogTest($"  - {family.Name} (from server) - Created: {family.CreatedAt:yyyy-MM-dd}");
                }

                // Compare local vs server
                var localNames = localFamilies.Select(f => f.Name).ToHashSet();
                var serverNames = serverFamilies.Select(f => f.Name).ToHashSet();
                var onlyLocal = localNames.Except(serverNames).ToList();
                var onlyServer = serverNames.Except(localNames).ToList();

                LogTest($"📊 Only local: {onlyLocal.Count} families");
                LogTest($"📊 Only server: {onlyServer.Count} families");
                LogTest($"📊 Common: {localNames.Intersect(serverNames).Count()} families");
            }
            else
            {
                LogTest("⚠️ Skipping server tests - not authenticated");
            }

            // Test 3: Statistics
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
            LogTest($"❌ Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// MELHORADO: Criação de família de teste com sync tracking
    /// </summary>
    private async void OnCreateTestFamilyClicked(object sender, EventArgs e)
    {
        try
        {
            LogTest("🧪 === CREATE TEST FAMILY ===");

            // Check auth first
            var isAuth = _supabaseService.IsAuthenticated;
            LogTest($"🔐 Authentication: {isAuth}");

            var testFamily = new Family
            {
                Name = $"Test Family {DateTime.Now:HH:mm:ss}",
                Description = $"Test family created at {DateTime.Now:yyyy-MM-dd HH:mm:ss} for sync testing",
                IsActive = true
            };

            LogTest($"➕ Creating family: {testFamily.Name}");
            var created = await _familyRepository.CreateAsync(testFamily);

            LogTest($"✅ Created successfully:");
            LogTest($"  - ID: {created.Id}");
            LogTest($"  - Name: {created.Name}");
            LogTest($"  - Status: {created.SyncStatus}");
            LogTest($"  - User ID: {created.UserId ?? Guid.Empty}");

            if (isAuth)
            {
                LogTest("⏳ Monitoring auto-sync progress...");

                // Monitor sync progress
                for (int i = 0; i < 10; i++)
                {
                    await Task.Delay(1000); // Wait 1 second

                    var updated = await _familyRepository.GetByIdAsync(created.Id);
                    if (updated != null)
                    {
                        LogTest($"  [{i + 1}s] Status: {updated.SyncStatus}" +
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
                LogTest("⚠️ Not authenticated - family will remain local only");
            }

            LogTest("🧪 === CREATE TEST COMPLETED ===");

        }
        catch (Exception ex)
        {
            LogTest($"❌ Create test error: {ex.Message}");
            LogTest($"❌ Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// MELHORADO: Force sync com relatório detalhado
    /// </summary>
    private async void OnForceFullSyncClicked(object sender, EventArgs e)
    {
        try
        {
            LogTest("🧪 === FORCE FULL SYNC ===");

            var isAuth = _supabaseService.IsAuthenticated;
            LogTest($"🔐 Authentication: {isAuth}");

            if (!isAuth)
            {
                LogTest("❌ Cannot perform sync - not authenticated");
                LogTest("💡 Try logging in first, then test again");
                return;
            }

            LogTest("🔄 Starting manual full sync...");
            var startTime = DateTime.Now;

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

            // Refresh and show updated counts
            LogTest("📊 Post-sync statistics:");
            var stats = await _familyRepository.GetStatisticsAsync();
            LogTest($"  - Total: {stats.TotalCount}");
            LogTest($"  - Synced: {stats.SyncedCount}");
            LogTest($"  - Local: {stats.LocalCount}");
            LogTest($"  - Pending: {stats.PendingCount}");
            LogTest($"  - Error: {stats.ErrorCount}");

            LogTest("🧪 === FORCE SYNC COMPLETED ===");

        }
        catch (Exception ex)
        {
            LogTest($"❌ Force sync error: {ex.Message}");
            LogTest($"❌ Stack trace: {ex.StackTrace}");
        }
    }

    private void OnClearLogClicked(object sender, EventArgs e)
    {
        StatusLabel.Text = $"Log cleared at {DateTime.Now:HH:mm:ss}\n";
        StatusLabel.Text += "Ready for testing...\n";
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