using OrchidPro.Services.Data;
using OrchidPro.Services.Navigation;
using OrchidPro.Views.Pages;

namespace OrchidPro;

/// <summary>
/// Main application shell that provides navigation structure
/// </summary>
public partial class AppShell : Shell
{
    private readonly SupabaseService _supabaseService;
    private readonly INavigationService _navigationService;

    public AppShell(SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        // Get navigation service
        var services = MauiProgram.CreateMauiApp().Services;
        _navigationService = services.GetRequiredService<INavigationService>();
    }

    /// <summary>
    /// Handles logout button click
    /// </summary>
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Logout",
            "Are you sure you want to sign out?",
            "Yes",
            "No"
        );

        if (confirm)
        {
            try
            {
                // Show loading
                await DisplayAlert("", "Signing out...", "OK");

                // Perform logout
                _supabaseService.Logout();

                // Navigate to login with transition
                await _navigationService.NavigateToLoginAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Logout failed: {ex.Message}", "OK");
            }
        }
    }
}