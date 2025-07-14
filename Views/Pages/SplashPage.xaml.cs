using OrchidPro.Services.Data;
using OrchidPro.Services.Navigation;
using System.Diagnostics;

namespace OrchidPro.Views.Pages;

/// <summary>
/// CORRIGIDO: SplashPage que inicializa Supabase adequadamente e verifica sessão
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

        // Start entrance animation
        await PerformEntranceAnimation();

        // Initialize app with proper error handling
        await InitializeAppAsync();
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
        _ = AnimateLoadingIndicatorAsync();

        await logoAnimation;
    }

    /// <summary>
    /// Continuously animates the loading indicator with a pulse effect
    /// </summary>
    private async Task AnimateLoadingIndicatorAsync()
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
    /// CORRIGIDO: Initializes the app services and determines navigation destination
    /// </summary>
    private async Task InitializeAppAsync()
    {
        try
        {
            Debug.WriteLine("🚀 === APP INITIALIZATION START ===");

            // Step 1: Initialize Supabase
            UpdateStatus("Initializing services...");
            Debug.WriteLine("🔄 Initializing Supabase...");

            await _supabaseService.InitializeAsync();
            Debug.WriteLine("✅ Supabase initialized successfully");

            // Add delay for better UX
            await Task.Delay(500);

            // Step 2: Check for existing session
            UpdateStatus("Checking authentication...");
            Debug.WriteLine("🔐 Checking for existing session...");

            // CORRIGIDO: Use the corrected RestoreSessionAsync method
            bool hasValidSession = await _supabaseService.RestoreSessionAsync();

            Debug.WriteLine($"🔐 Session restore result: {hasValidSession}");

            if (hasValidSession)
            {
                var user = _supabaseService.GetCurrentUser();
                Debug.WriteLine($"✅ Valid session found for user: {user?.Email}");
                Debug.WriteLine($"✅ User ID: {user?.Id}");
            }
            else
            {
                Debug.WriteLine("❌ No valid session found - user needs to login");
            }

            // Add delay to ensure smooth transition
            await Task.Delay(700);

            // Step 3: Perform enhanced exit animation
            await PerformExitAnimation();

            // Step 4: Navigate based on session status
            Debug.WriteLine("🧭 Navigating based on session status...");

            if (hasValidSession)
            {
                Debug.WriteLine("🧭 Navigating to main app...");
                await _navigationService.NavigateToMainAsync();
            }
            else
            {
                Debug.WriteLine("🧭 Navigating to login...");
                await _navigationService.NavigateToLoginAsync();
            }

            Debug.WriteLine("🚀 === APP INITIALIZATION COMPLETED ===");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Initialization failed: {ex.Message}");
            Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");

            UpdateStatus("Initialization failed...");
            await Task.Delay(1000);

            try
            {
                await DisplayAlert("Initialization Error",
                    $"Failed to initialize the app: {ex.Message}", "OK");

                // Try to navigate to login as fallback
                await _navigationService.NavigateToLoginAsync();
            }
            catch (Exception navEx)
            {
                Debug.WriteLine($"❌ Even fallback navigation failed: {navEx.Message}");
                // Last resort - close app
                Application.Current?.Quit();
            }
        }
    }

    /// <summary>
    /// Updates the status label with enhanced fade animation
    /// </summary>
    private async void UpdateStatus(string message)
    {
        try
        {
            await StatusLabel.FadeTo(0.3, 150);
            StatusLabel.Text = message;
            await StatusLabel.FadeTo(0.8, 150);

            Debug.WriteLine($"📱 Status: {message}");
        }
        catch
        {
            // Ignore animation errors during page destruction
        }
    }

    /// <summary>
    /// Performs enhanced exit animation before navigation
    /// </summary>
    private async Task PerformExitAnimation()
    {
        try
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
        catch
        {
            // Ignore animation errors during page destruction
        }
    }
}