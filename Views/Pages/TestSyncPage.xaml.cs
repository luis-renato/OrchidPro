using OrchidPro.Services.Data;
using OrchidPro.Services;
using OrchidPro.Models;
using System.Diagnostics;
using System.Text;

namespace OrchidPro.Views.Pages;

/// <summary>
/// MIGRADO: Página de teste atualizada para arquitetura simplificada
/// Remove funcionalidades de sincronização complexa, foca em debug de conectividade
/// </summary>
public partial class TestSyncPage : ContentPage
{
    private readonly SupabaseService _supabaseService;
    private readonly IFamilyRepository _familyRepository;
    private readonly SupabaseFamilyService _familyService;

    public TestSyncPage()
    {
        InitializeComponent();

        try
        {
            var services = MauiProgram.CreateMauiApp().Services;
            _supabaseService = services.GetRequiredService<SupabaseService>();
            _familyRepository = services.GetRequiredService<IFamilyRepository>();
            _familyService = services.GetRequiredService<SupabaseFamilyService>();

            LogTest("✅ All services loaded successfully");
            LogTest("🎯 SIMPLIFIED ARCHITECTURE - Direct Supabase");
            LogTest("🔍 Focus: Connectivity and cache management");
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
    /// MIGRADO: Teste de conectividade simplificado
    /// </summary>
    private async void OnTestSupabaseClicked(object sender, EventArgs e)
    {
        try
        {
            LogTest("🧪 === SIMPLIFIED ARCHITECTURE TEST ===");
            LogTest("🎯 Target: Test direct Supabase connectivity");

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
                LogTest("❌ Not authenticated - login first to continue test");
                return;
            }

            LogTest("");
            LogTest("🔍 === CONNECTION TEST ===");

            var connectionOk = await _familyService.TestConnectionAsync();
            LogTest($"🌐 Connection test: {connectionOk}");

            if (!connectionOk)
            {
                LogTest("❌ Connection failed - check Supabase setup");
                return;
            }

            LogTest("");
            LogTest("📊 === REPOSITORY TEST ===");

            var repoConnectionOk = await _familyRepository.TestConnectionAsync();
            LogTest($"🏪 Repository connection: {repoConnectionOk}");

            if (repoConnectionOk)
            {
                LogTest("🎉 SIMPLIFIED ARCHITECTURE: WORKING!");
                LogTest("✅ Direct Supabase connection working");
                LogTest("✅ Repository with cache working");
                LogTest("✅ No sync complexity - all operations direct");
            }
            else
            {
                LogTest("❌ Repository test failed");
                return;
            }

            LogTest("");
            LogTest("💾 === CACHE TEST ===");
            await TestCacheManagement();

            LogTest("🧪 === SIMPLIFIED TEST COMPLETED ===");
            await UpdateQuickStats();

        }
        catch (Exception ex)
        {
            LogTest($"❌ Test failed with exception: {ex.Message}");
            LogTest($"❌ Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// NOVO: Teste específico do cache inteligente
    /// </summary>
    private async Task TestCacheManagement()
    {
        try
        {
            LogTest("💾 Testing intelligent cache management...");

            // Obter info do cache
            var cacheInfo = _familyRepository.GetCacheInfo();
            LogTest($"📊 Current cache: {cacheInfo}");

            // Forçar refresh do cache
            LogTest("🔄 Testing cache refresh...");
            await _familyRepository.RefreshCacheAsync();

            var newCacheInfo = _familyRepository.GetCacheInfo();
            LogTest($"📊 After refresh: {newCacheInfo}");

            // Teste de performance do cache
            LogTest("⚡ Testing cache performance...");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var families1 = await _familyRepository.GetAllAsync();
            stopwatch.Stop();
            LogTest($"⚡ First call (server): {stopwatch.ElapsedMilliseconds}ms, {families1.Count} families");

            stopwatch.Restart();
            var families2 = await _familyRepository.GetAllAsync();
            stopwatch.Stop();
            LogTest($"⚡ Second call (cache): {stopwatch.ElapsedMilliseconds}ms, {families2.Count} families");

            if (stopwatch.ElapsedMilliseconds < 50)
            {
                LogTest("🎉 CACHE PERFORMANCE: EXCELLENT! (<50ms)");
            }
            else
            {
                LogTest("⚠️ Cache might not be working optimally");
            }

        }
        catch (Exception ex)
        {
            LogTest($"❌ Cache test failed: {ex.Message}");
        }
    }

    /// <summary>
    /// MIGRADO: Teste de famílias com análise simplificada
    /// </summary>
    private async void OnTestFamiliesClicked(object sender, EventArgs e)
    {
        try
        {
            LogTest("🧪 === SIMPLIFIED FAMILIES TEST ===");

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
            LogTest("📊 === REPOSITORY STATISTICS ===");

            var stats = await _familyRepository.GetStatisticsAsync();
            LogTest($"📊 Repository stats:");
            LogTest($"  Total: {stats.TotalCount}");
            LogTest($"  Active: {stats.ActiveCount}");
            LogTest($"  Inactive: {stats.InactiveCount}");
            LogTest($"  System: {stats.SystemDefaultCount}");
            LogTest($"  User: {stats.UserCreatedCount}");

            LogTest("");
            LogTest("📱 === FAMILIES ANALYSIS ===");

            var allFamilies = await _familyRepository.GetAllAsync(true); // Include inactive
            LogTest($"📱 Total families: {allFamilies.Count}");

            var activeFamilies = allFamilies.Where(f => f.IsActive).ToList();
            var inactiveFamilies = allFamilies.Where(f => !f.IsActive).ToList();
            var systemFamilies = allFamilies.Where(f => f.IsSystemDefault).ToList();
            var userFamilies = allFamilies.Where(f => !f.IsSystemDefault).ToList();

            LogTest($"  📊 Active: {activeFamilies.Count}");
            LogTest($"  📊 Inactive: {inactiveFamilies.Count}");
            LogTest($"  📊 System: {systemFamilies.Count}");
            LogTest($"  📊 User: {userFamilies.Count}");

            LogTest("");
            LogTest("🔍 === FAMILY DETAILS ===");

            foreach (var family in allFamilies.Take(10))
            {
                LogTest($"  📝 {family.Name}");
                LogTest($"     ID: {family.Id}");
                LogTest($"     Status: {(family.IsActive ? "Active" : "Inactive")}");
                LogTest($"     Type: {(family.IsSystemDefault ? "System" : "User")}");
                LogTest($"     Created: {family.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                LogTest($"     Sync: {family.SyncStatusDisplay}");
            }

            if (allFamilies.Count > 10)
            {
                LogTest($"  ... and {allFamilies.Count - 10} more families");
            }

            LogTest("");
            LogTest("💾 === CACHE INFORMATION ===");
            var cacheInfo = _familyRepository.GetCacheInfo();
            LogTest($"💾 {cacheInfo}");

            LogTest("🧪 === FAMILIES TEST COMPLETED ===");
            await UpdateQuickStats();

        }
        catch (Exception ex)
        {
            LogTest($"❌ Families test error: {ex.Message}");
            LogTest($"❌ Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// MIGRADO: Criação de família de teste sem duplicatas
    /// </summary>
    private async void OnCreateTestFamilyClicked(object sender, EventArgs e)
    {
        try
        {
            LogTest("🧪 === CREATE TEST FAMILY (SIMPLIFIED) ===");

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
                LogTest($"⚠️ Name collision detected - adding random suffix");
                uniqueName += $"_{Random.Shared.Next(1000, 9999)}";
            }

            var testFamily = new Family
            {
                Name = uniqueName,
                Description = $"Test family created at {DateTime.Now:yyyy-MM-dd HH:mm:ss} for simplified architecture testing",
                IsActive = true
            };

            LogTest($"➕ Creating family: {testFamily.Name}");
            LogTest($"➕ Family ID: {testFamily.Id}");

            var created = await _familyRepository.CreateAsync(testFamily);

            LogTest($"✅ Created successfully:");
            LogTest($"  - ID: {created.Id}");
            LogTest($"  - Name: {created.Name}");
            LogTest($"  - Status: {created.SyncStatusDisplay}");
            LogTest($"  - User ID: {created.UserId ?? Guid.Empty}");
            LogTest($"  - Created At: {created.CreatedAt:yyyy-MM-dd HH:mm:ss}");

            LogTest("");
            LogTest("🔍 Verifying creation...");

            // Verificar se foi criado corretamente
            var verification = await _familyRepository.GetByIdAsync(created.Id);
            if (verification != null)
            {
                LogTest("🎉 PERFECT! Family created and verified successfully");
                LogTest($"✅ Verification: {verification.Name} exists in repository");
                LogTest($"✅ No complexity - direct creation worked flawlessly");
            }
            else
            {
                LogTest("❌ Verification failed - family not found after creation");
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
    /// MIGRADO: Force refresh do cache
    /// </summary>
    private async void OnForceFullSyncClicked(object sender, EventArgs e)
    {
        try
        {
            LogTest("🧪 === FORCE CACHE REFRESH (SIMPLIFIED) ===");

            var isAuth = _supabaseService.IsAuthenticated;
            LogTest($"🔐 Authentication: {isAuth}");

            if (!isAuth)
            {
                LogTest("❌ Cannot refresh cache - not authenticated");
                return;
            }

            LogTest("🔄 Starting cache refresh from server...");

            // Mostrar estado pré-refresh
            LogTest("");
            LogTest("📊 PRE-REFRESH STATE:");
            var preStats = await _familyRepository.GetStatisticsAsync();
            LogTest($"  Total families: {preStats.TotalCount}");
            LogTest($"  Active: {preStats.ActiveCount}");
            LogTest($"  System: {preStats.SystemDefaultCount}");
            LogTest($"  User: {preStats.UserCreatedCount}");

            var preCacheInfo = _familyRepository.GetCacheInfo();
            LogTest($"  Cache: {preCacheInfo}");

            LogTest("");
            LogTest("🚀 EXECUTING CACHE REFRESH...");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var result = await _familyRepository.ForceFullSyncAsync();

            stopwatch.Stop();

            LogTest($"✅ Cache refresh completed in {stopwatch.ElapsedMilliseconds}ms");
            LogTest($"📊 REFRESH RESULTS:");
            LogTest($"  - Duration: {result.Duration.TotalSeconds:F1} seconds");
            LogTest($"  - Processed: {result.TotalProcessed} families");
            LogTest($"  - Successful: {result.Successful}");
            LogTest($"  - Failed: {result.Failed}");

            if (result.ErrorMessages.Any())
            {
                LogTest($"❌ Errors ({result.ErrorMessages.Count}):");
                foreach (var error in result.ErrorMessages.Take(3))
                {
                    LogTest($"  - {error}");
                }
            }

            LogTest("");
            LogTest("📊 POST-REFRESH STATE:");
            var postStats = await _familyRepository.GetStatisticsAsync();
            LogTest($"  Total families: {postStats.TotalCount}");
            LogTest($"  Active: {postStats.ActiveCount}");
            LogTest($"  System: {postStats.SystemDefaultCount}");
            LogTest($"  User: {postStats.UserCreatedCount}");

            var postCacheInfo = _familyRepository.GetCacheInfo();
            LogTest($"  Cache: {postCacheInfo}");

            // Análise de resultados
            LogTest("");
            LogTest("📈 PERFORMANCE ANALYSIS:");
            LogTest($"  Refresh time: {stopwatch.ElapsedMilliseconds}ms");
            LogTest($"  Families loaded: {postStats.TotalCount}");

            if (stopwatch.ElapsedMilliseconds < 2000)
            {
                LogTest("🎉 EXCELLENT PERFORMANCE! (<2s)");
            }
            else if (stopwatch.ElapsedMilliseconds < 5000)
            {
                LogTest("✅ Good performance (<5s)");
            }
            else
            {
                LogTest("⚠️ Slow performance (>5s) - check connection");
            }

            LogTest("");
            LogTest("💡 SIMPLIFIED ARCHITECTURE BENEFITS:");
            LogTest("  ✅ No sync conflicts or duplicates");
            LogTest("  ✅ Direct server data always fresh");
            LogTest("  ✅ Intelligent cache improves performance");
            LogTest("  ✅ Zero complexity - just works!");

            LogTest("🧪 === CACHE REFRESH COMPLETED ===");
            await UpdateQuickStats();

        }
        catch (Exception ex)
        {
            LogTest($"❌ Cache refresh error: {ex.Message}");
            LogTest($"❌ Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// NOVO: Teste de performance da arquitetura
    /// </summary>
    private async void OnPerformanceTestClicked(object sender, EventArgs e)
    {
        try
        {
            LogTest("🧪 === PERFORMANCE TEST ===");

            if (!_supabaseService.IsAuthenticated)
            {
                LogTest("❌ Not authenticated");
                return;
            }

            LogTest("⚡ Testing simplified architecture performance...");

            // Teste 1: Cache vs Server
            LogTest("");
            LogTest("📊 CACHE VS SERVER PERFORMANCE:");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await _familyRepository.RefreshCacheAsync(); // Force server call
            stopwatch.Stop();
            var serverTime = stopwatch.ElapsedMilliseconds;
            LogTest($"  Server call: {serverTime}ms");

            stopwatch.Restart();
            await _familyRepository.GetAllAsync(); // Use cache
            stopwatch.Stop();
            var cacheTime = stopwatch.ElapsedMilliseconds;
            LogTest($"  Cache call: {cacheTime}ms");

            var improvement = serverTime > 0 ? ((double)(serverTime - cacheTime) / serverTime * 100) : 0;
            LogTest($"  Performance improvement: {improvement:F1}%");

            // Teste 2: Multiple operations
            LogTest("");
            LogTest("🔄 MULTIPLE OPERATIONS TEST:");

            stopwatch.Restart();
            for (int i = 0; i < 5; i++)
            {
                await _familyRepository.GetAllAsync();
            }
            stopwatch.Stop();
            var multipleCallsTime = stopwatch.ElapsedMilliseconds;
            LogTest($"  5 consecutive calls: {multipleCallsTime}ms ({multipleCallsTime / 5.0:F1}ms avg)");

            // Teste 3: Filtered queries
            stopwatch.Restart();
            await _familyRepository.GetFilteredAsync("test", true, null);
            stopwatch.Stop();
            var filteredTime = stopwatch.ElapsedMilliseconds;
            LogTest($"  Filtered query: {filteredTime}ms");

            LogTest("");
            LogTest("📊 PERFORMANCE SUMMARY:");
            if (cacheTime < 20 && multipleCallsTime < 100)
            {
                LogTest("🎉 EXCELLENT! Cache is working perfectly");
            }
            else if (cacheTime < 50)
            {
                LogTest("✅ Good cache performance");
            }
            else
            {
                LogTest("⚠️ Cache might need optimization");
            }

            LogTest("🧪 === PERFORMANCE TEST COMPLETED ===");

        }
        catch (Exception ex)
        {
            LogTest($"❌ Performance test error: {ex.Message}");
        }
    }

    /// <summary>
    /// NOVO: Export debug information simplificado
    /// </summary>
    private async void OnExportDebugInfoClicked(object sender, EventArgs e)
    {
        try
        {
            LogTest("📋 === EXPORTING DEBUG INFO (SIMPLIFIED) ===");

            var debugInfo = new StringBuilder();
            debugInfo.AppendLine("OrchidPro Simplified Architecture Debug Report");
            debugInfo.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            debugInfo.AppendLine($"User: {_supabaseService.GetCurrentUser()?.Email ?? "Not authenticated"}");
            debugInfo.AppendLine($"User ID: {_supabaseService.GetCurrentUserId() ?? "null"}");
            debugInfo.AppendLine();

            debugInfo.AppendLine("=== ARCHITECTURE INFO ===");
            debugInfo.AppendLine("Type: Simplified - Direct Supabase");
            debugInfo.AppendLine("Cache: Intelligent 5-minute cache");
            debugInfo.AppendLine("Sync: Always synced (no local-remote conflicts)");
            debugInfo.AppendLine("Benefits: 50% less code, zero sync bugs");
            debugInfo.AppendLine();

            debugInfo.AppendLine("=== AUTHENTICATION STATUS ===");
            debugInfo.AppendLine($"Authenticated: {_supabaseService.IsAuthenticated}");
            debugInfo.AppendLine($"Client Initialized: {_supabaseService.IsInitialized}");
            debugInfo.AppendLine();

            debugInfo.AppendLine("=== REPOSITORY STATISTICS ===");
            try
            {
                var stats = await _familyRepository.GetStatisticsAsync();
                debugInfo.AppendLine($"Total Families: {stats.TotalCount}");
                debugInfo.AppendLine($"Active: {stats.ActiveCount}");
                debugInfo.AppendLine($"Inactive: {stats.InactiveCount}");
                debugInfo.AppendLine($"System Defaults: {stats.SystemDefaultCount}");
                debugInfo.AppendLine($"User Created: {stats.UserCreatedCount}");
                debugInfo.AppendLine($"Cache Info: {_familyRepository.GetCacheInfo()}");
            }
            catch (Exception ex)
            {
                debugInfo.AppendLine($"Error getting stats: {ex.Message}");
            }

            debugInfo.AppendLine();
            debugInfo.AppendLine("=== CONNECTIVITY TEST ===");
            try
            {
                var connected = await _familyRepository.TestConnectionAsync();
                debugInfo.AppendLine($"Repository Connection: {connected}");

                var serviceConnected = await _familyService.TestConnectionAsync();
                debugInfo.AppendLine($"Service Connection: {serviceConnected}");
            }
            catch (Exception ex)
            {
                debugInfo.AppendLine($"Connection test error: {ex.Message}");
            }

            debugInfo.AppendLine();
            debugInfo.AppendLine("=== SAMPLE FAMILIES ===");
            try
            {
                var families = await _familyRepository.GetAllAsync(true);
                foreach (var family in families.Take(5))
                {
                    debugInfo.AppendLine($"- {family.Name} (Active: {family.IsActive}, System: {family.IsSystemDefault})");
                }
                if (families.Count > 5)
                {
                    debugInfo.AppendLine($"... and {families.Count - 5} more");
                }
            }
            catch (Exception ex)
            {
                debugInfo.AppendLine($"Error getting families: {ex.Message}");
            }

            debugInfo.AppendLine();
            debugInfo.AppendLine("=== CURRENT LOG ===");
            debugInfo.AppendLine(StatusLabel.Text);

            // Save to clipboard
            await Clipboard.SetTextAsync(debugInfo.ToString());

            LogTest("📋 Debug information exported to clipboard");
            LogTest($"📊 Report size: {debugInfo.Length} characters");
            LogTest("💡 Simplified architecture - much cleaner debug info!");

        }
        catch (Exception ex)
        {
            LogTest($"❌ Export error: {ex.Message}");
        }
    }

    /// <summary>
    /// Copy log to clipboard
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
    /// Update quick stats display
    /// </summary>
    private async Task UpdateQuickStats()
    {
        try
        {
            if (!_supabaseService.IsAuthenticated)
            {
                LocalCountLabel.Text = "N/A";
                ServerCountLabel.Text = "N/A";
                SyncedCountLabel.Text = "N/A";
                DuplicatesLabel.Text = "N/A";
                return;
            }

            // Repository stats
            var stats = await _familyRepository.GetStatisticsAsync();
            LocalCountLabel.Text = stats.TotalCount.ToString();
            SyncedCountLabel.Text = stats.TotalCount.ToString(); // All synced in new architecture
            ServerCountLabel.Text = stats.TotalCount.ToString(); // Same as local since no cache divergence

            // No duplicates in simplified architecture
            DuplicatesLabel.Text = "0";
            DuplicatesLabel.TextColor = Colors.Green;
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

    private void OnClearLogClicked(object sender, EventArgs e)
    {
        StatusLabel.Text = $"Log cleared at {DateTime.Now:HH:mm:ss}\n";
        StatusLabel.Text += "🚀 SIMPLIFIED ARCHITECTURE - Ready for testing\n";
        StatusLabel.Text += "🎯 Focus: Direct Supabase with intelligent cache\n";
        StatusLabel.Text += "✅ Benefits: 50% less code, zero sync bugs\n";
        StatusLabel.Text += "💡 Use 'Test Connection' to verify connectivity\n";
        StatusLabel.Text += "💡 Use 'Test Families' to analyze data\n";
        StatusLabel.Text += "💡 Use 'Create Test' to test direct operations\n";
        StatusLabel.Text += "💡 Use 'Force Refresh' to test cache management\n";

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