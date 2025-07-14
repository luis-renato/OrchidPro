using OrchidPro.Services.Data;
using OrchidPro.Services;
using OrchidPro.Models;
using System.Diagnostics;

namespace OrchidPro.Views.Pages;

/// <summary>
/// Página temporária para testar sincronização sem alterar o login
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
        var services = MauiProgram.CreateMauiApp().Services;
        _supabaseService = services.GetRequiredService<SupabaseService>();
        _familyRepository = services.GetRequiredService<IFamilyRepository>();
        _syncService = services.GetRequiredService<SupabaseFamilySync>();
    }

    private async void OnTestSupabaseClicked(object sender, EventArgs e)
    {
        try
        {
            UpdateStatus("Testing Supabase connection...");

            // Test 1: Initialize
            await _supabaseService.InitializeAsync();
            UpdateStatus("✅ Supabase initialized");

            // Test 2: Connection
            var connected = await _syncService.TestConnectionAsync();
            UpdateStatus($"✅ Connection test: {connected}");

            // Test 3: User status
            var isAuth = _supabaseService.IsAuthenticated;
            var userId = _supabaseService.GetCurrentUserId();
            UpdateStatus($"✅ Authenticated: {isAuth}, User: {userId ?? "None"}");

        }
        catch (Exception ex)
        {
            UpdateStatus($"❌ Error: {ex.Message}");
        }
    }

    private async void OnTestFamiliesClicked(object sender, EventArgs e)
    {
        try
        {
            UpdateStatus("Testing families...");

            // Test 1: Get local families
            var families = await _familyRepository.GetAllAsync();
            UpdateStatus($"✅ Local families: {families.Count}");

            foreach (var family in families.Take(3))
            {
                UpdateStatus($"  - {family.Name} ({family.SyncStatus})");
            }

            // Test 2: Download from server
            var serverFamilies = await _syncService.DownloadFamiliesAsync();
            UpdateStatus($"✅ Server families: {serverFamilies.Count}");

        }
        catch (Exception ex)
        {
            UpdateStatus($"❌ Error: {ex.Message}");
        }
    }

    private async void OnCreateTestFamilyClicked(object sender, EventArgs e)
    {
        try
        {
            UpdateStatus("Creating test family...");

            var testFamily = new Family
            {
                Name = $"Test Family {DateTime.Now:HH:mm:ss}",
                Description = "Family created for testing sync",
                IsActive = true
            };

            var created = await _familyRepository.CreateAsync(testFamily);
            UpdateStatus($"✅ Created: {created.Name}");
            UpdateStatus($"   Status: {created.SyncStatus}");
            UpdateStatus($"   ID: {created.Id}");

            // Wait a bit and check sync status
            await Task.Delay(2000);
            var updated = await _familyRepository.GetByIdAsync(created.Id);
            if (updated != null)
            {
                UpdateStatus($"   After 2s: {updated.SyncStatus}");
            }

        }
        catch (Exception ex)
        {
            UpdateStatus($"❌ Error: {ex.Message}");
        }
    }

    private async void OnForceFullSyncClicked(object sender, EventArgs e)
    {
        try
        {
            UpdateStatus("Starting full sync...");

            var result = await _familyRepository.ForceFullSyncAsync();

            UpdateStatus($"✅ Sync completed:");
            UpdateStatus($"   Processed: {result.TotalProcessed}");
            UpdateStatus($"   Successful: {result.Successful}");
            UpdateStatus($"   Failed: {result.Failed}");
            UpdateStatus($"   Duration: {result.Duration.TotalSeconds:F1}s");

            if (result.ErrorMessages.Any())
            {
                UpdateStatus($"   Errors: {string.Join(", ", result.ErrorMessages)}");
            }

        }
        catch (Exception ex)
        {
            UpdateStatus($"❌ Error: {ex.Message}");
        }
    }

    private async void OnClearLogClicked(object sender, EventArgs e)
    {
        StatusLabel.Text = "Log cleared\n";
    }

    private void UpdateStatus(string message)
    {
        StatusLabel.Text += $"{message}\n";
        Debug.WriteLine(message);

        // Auto-scroll to bottom
        LogScrollView.ScrollToAsync(StatusLabel, ScrollToPosition.End, false);
    }
}