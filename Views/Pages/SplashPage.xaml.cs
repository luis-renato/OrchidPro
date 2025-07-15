using OrchidPro.Services;
using OrchidPro.Services.Data;
using OrchidPro.Services.Navigation;
using System.Diagnostics;

namespace OrchidPro.Views.Pages;

/// <summary>
/// CORRIGIDO: SplashPage que GARANTE inicialização dos singleton services
/// </summary>
public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
        Debug.WriteLine("📱 SplashPage created");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Start entrance animation
        await PerformEntranceAnimation();

        // ✅ CRÍTICO: Garantir inicialização ANTES de navegar
        await InitializeAppWithSingletonsAsync();
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
    /// ✅ CORRIGIDO: Inicializa app GARANTINDO que singletons sejam criados e inicializados
    /// </summary>
    private async Task InitializeAppWithSingletonsAsync()
    {
        try
        {
            Debug.WriteLine("🚀 === APP INITIALIZATION WITH SINGLETONS ===");

            // Step 1: Get services from DI
            UpdateStatus("Getting services...");
            Debug.WriteLine("🔄 Getting services from DI container...");

            var services = IPlatformApplication.Current?.Services;
            if (services == null)
            {
                throw new InvalidOperationException("Service provider not available");
            }

            Debug.WriteLine("✅ Service provider obtained");

            // Step 2: ✅ CRÍTICO - Forçar criação e inicialização dos singletons
            UpdateStatus("Initializing core services...");
            Debug.WriteLine("🔧 Creating and initializing singleton services...");

            // ✅ FORÇAR criação do SupabaseService singleton e inicializar
            var supabaseService = services.GetRequiredService<SupabaseService>();
            Debug.WriteLine("✅ SupabaseService singleton obtained");

            // ✅ GARANTIR que está inicializado
            if (!supabaseService.IsInitialized)
            {
                Debug.WriteLine("🔄 SupabaseService not initialized - initializing now...");
                await supabaseService.InitializeAsync();
                Debug.WriteLine("✅ SupabaseService initialized successfully");
            }
            else
            {
                Debug.WriteLine("✅ SupabaseService already initialized");
            }

            // ✅ FORÇAR criação dos outros singletons
            var familyService = services.GetRequiredService<SupabaseFamilyService>();
            var familyRepo = services.GetRequiredService<IFamilyRepository>();
            var navigationService = services.GetRequiredService<INavigationService>();

            Debug.WriteLine("✅ All singleton services created and available");

            // Step 3: ✅ VERIFICAR estado após inicialização
            UpdateStatus("Verifying initialization...");
            Debug.WriteLine("🔍 Verifying singleton initialization...");

            Debug.WriteLine($"🔧 SupabaseService.IsInitialized: {supabaseService.IsInitialized}");
            Debug.WriteLine($"🔐 SupabaseService.IsAuthenticated: {supabaseService.IsAuthenticated}");

            if (!supabaseService.IsInitialized)
            {
                throw new InvalidOperationException("SupabaseService failed to initialize");
            }

            // Step 4: Check for existing session
            UpdateStatus("Checking authentication...");
            Debug.WriteLine("🔐 Checking for existing session...");

            bool hasValidSession = await supabaseService.RestoreSessionAsync();
            Debug.WriteLine($"🔐 Session restore result: {hasValidSession}");

            if (hasValidSession)
            {
                var user = supabaseService.GetCurrentUser();
                Debug.WriteLine($"✅ Valid session found for user: {user?.Email}");
                Debug.WriteLine($"✅ User ID: {user?.Id}");
            }
            else
            {
                Debug.WriteLine("❌ No valid session found - user needs to login");
            }

            // Step 5: ✅ VERIFICAÇÃO FINAL antes de navegar
            Debug.WriteLine("🧪 === FINAL VERIFICATION BEFORE NAVIGATION ===");
            Debug.WriteLine($"✅ SupabaseService initialized: {supabaseService.IsInitialized}");
            Debug.WriteLine($"✅ SupabaseService authenticated: {supabaseService.IsAuthenticated}");
            Debug.WriteLine($"✅ Services available for dependency injection");

            // Add delay to ensure smooth transition
            await Task.Delay(700);

            // Step 6: Perform enhanced exit animation
            await PerformExitAnimation();

            // Step 7: Navigate based on session status
            Debug.WriteLine("🧭 Navigating based on session status...");

            if (hasValidSession)
            {
                Debug.WriteLine("🧭 Navigating to main app...");
                await navigationService.NavigateToMainAsync();
            }
            else
            {
                Debug.WriteLine("🧭 Navigating to login...");
                await navigationService.NavigateToLoginAsync();
            }

            Debug.WriteLine("🚀 === APP INITIALIZATION WITH SINGLETONS COMPLETED ===");
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
                var services = IPlatformApplication.Current?.Services;
                if (services != null)
                {
                    var navigationService = services.GetRequiredService<INavigationService>();
                    await navigationService.NavigateToLoginAsync();
                }
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