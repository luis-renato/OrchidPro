using OrchidPro.Services.Data;
using OrchidPro.Services.Navigation;

namespace OrchidPro.Views.Pages;

/// <summary>
/// Initial splash screen that handles app initialization and authentication check
/// </summary>
public partial class SplashPage : ContentPage
{
    private readonly SupabaseService _supabaseService;
    private readonly INavigationService _navigationService;

    public SplashPage()
    {
        InitializeComponent();

        // Get services from DI container
        var services = MauiProgram.CreateMauiApp().Services;
        _supabaseService = services.GetRequiredService<SupabaseService>();
        _navigationService = services.GetRequiredService<INavigationService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Start entrance animation with enhanced fade in
        await PerformEntranceAnimation();

        // Initialize app
        await InitializeApp();
    }

    /// <summary>
    /// Performs enhanced entrance animation for splash screen elements
    /// </summary>
    private async Task PerformEntranceAnimation()
    {
        // Start with full transparency
        RootGrid.Opacity = 0;
        LogoImage.Scale = 0.8;

        // Fade in the root grid smoothly
        await RootGrid.FadeTo(1, 500, Easing.CubicOut);

        // Scale and fade in logo with spring effect
        var logoAnimation = LogoImage.ScaleTo(1, 600, Easing.SpringOut);

        // Start pulse animation for loading indicator
        _ = AnimateLoadingIndicator();

        await logoAnimation;
    }

    /// <summary>
    /// Continuously animates the loading indicator with a pulse effect
    /// </summary>
    private async Task AnimateLoadingIndicator()
    {
        try
        {
            while (LoadingIndicator.IsRunning)
            {
                await LoadingIndicator.ScaleTo(1.2, 500, Easing.SinInOut);
                await LoadingIndicator.ScaleTo(1.0, 500, Easing.SinInOut);
            }
        }
        catch
        {
            // Page was destroyed during animation
        }
    }

    /// <summary>
    /// Initializes the app services and determines navigation destination
    /// </summary>
    private async Task InitializeApp()
    {
        try
        {
            // Update status
            UpdateStatus("Initializing services...");
            await _supabaseService.InitializeAsync();

            // Add delay for better UX
            await Task.Delay(700);

            // Check for existing session
            UpdateStatus("Checking authentication...");
            bool hasValidSession = await _supabaseService.RestoreSessionAsync();

            // Add delay to ensure smooth transition
            await Task.Delay(500);

            // Perform enhanced exit animation
            await PerformExitAnimation();

            // Navigate based on session status
            if (hasValidSession)
            {
                await _navigationService.NavigateToMainAsync();
            }
            else
            {
                await _navigationService.NavigateToLoginAsync();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Initialization failed: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Updates the status label with enhanced fade animation
    /// </summary>
    private async void UpdateStatus(string message)
    {
        await StatusLabel.FadeTo(0.3, 150);
        StatusLabel.Text = message;
        await StatusLabel.FadeTo(0.8, 150);
    }

    /// <summary>
    /// Performs enhanced exit animation before navigation
    /// </summary>
    private async Task PerformExitAnimation()
    {
        LoadingIndicator.IsRunning = false;

        // Enhanced exit with multiple elements fading
        await Task.WhenAll(
            LogoImage.ScaleTo(0.9, 300, Easing.CubicIn),
            StatusLabel.FadeTo(0, 200, Easing.CubicIn),
            LoadingIndicator.FadeTo(0, 200, Easing.CubicIn)
        );

        // Final fade out of entire screen
        await RootGrid.FadeTo(0, 300, Easing.CubicIn);
    }
}