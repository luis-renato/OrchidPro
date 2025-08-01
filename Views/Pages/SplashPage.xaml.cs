using OrchidPro.Services;
using OrchidPro.Services.Data;
using OrchidPro.Services.Navigation;
using OrchidPro.Extensions;
using OrchidPro.Constants;

namespace OrchidPro.Views.Pages;

/// <summary>
/// Splash screen page that handles application initialization and singleton service setup.
/// Provides smooth animations while initializing core services and checking authentication state.
/// </summary>
public partial class SplashPage : ContentPage
{
    /// <summary>
    /// Initialize splash page with default setup
    /// </summary>
    public SplashPage()
    {
        InitializeComponent();
        this.LogInfo("SplashPage created");
    }

    /// <summary>
    /// Handle page appearing with animation and app initialization
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Start entrance animation and app initialization
        await PerformEntranceAnimation();
        await InitializeAppWithSingletonsAsync();
    }

    /// <summary>
    /// Perform enhanced entrance animation for splash screen elements
    /// </summary>
    private async Task PerformEntranceAnimation()
    {
        await this.SafeAnimationExecuteAsync(async () =>
        {
            // Set initial states using constants
            RootGrid.Opacity = AnimationConstants.INITIAL_OPACITY;
            LogoImage.Scale = AnimationConstants.PAGE_ENTRANCE_INITIAL_SCALE;

            // Fade in the root grid smoothly
            await RootGrid.FadeTo(
                AnimationConstants.FULL_OPACITY,
                AnimationConstants.SPLASH_FADE_IN_DURATION,
                AnimationConstants.ENTRANCE_EASING);

            // Scale logo with spring effect
            var logoAnimation = LogoImage.ScaleTo(
                AnimationConstants.FEEDBACK_SCALE_NORMAL,
                AnimationConstants.SPLASH_LOGO_SCALE_DURATION,
                AnimationConstants.SPRING_EASING);

            // Start pulse animation for loading indicator
            _ = AnimateLoadingIndicatorAsync();

            await logoAnimation;

            this.LogSuccess("Splash entrance animation completed");
        }, "Splash entrance animation");
    }

    /// <summary>
    /// Continuously animate loading indicator with pulse effect using constants
    /// </summary>
    private async Task AnimateLoadingIndicatorAsync()
    {
        await this.SafeAnimationExecuteAsync(async () =>
        {
            while (LoadingIndicator.IsRunning)
            {
                await LoadingIndicator.ScaleTo(1.2,
                    AnimationConstants.SPLASH_LOADING_PULSE_DURATION,
                    AnimationConstants.LOADING_PULSE_EASING);

                await LoadingIndicator.ScaleTo(
                    AnimationConstants.FEEDBACK_SCALE_NORMAL,
                    AnimationConstants.SPLASH_LOADING_PULSE_DURATION,
                    AnimationConstants.LOADING_PULSE_EASING);
            }
        }, "Loading indicator pulse animation");
    }

    /// <summary>
    /// Initialize application ensuring singleton services are created and configured
    /// </summary>
    private async Task InitializeAppWithSingletonsAsync()
    {
        try
        {
            this.LogInfo("=== APP INITIALIZATION WITH SINGLETONS ===");

            // Step 1: Get services from dependency injection
            UpdateStatus("Getting services...");
            this.LogInfo("Getting services from DI container...");

            var services = IPlatformApplication.Current?.Services;
            if (services == null)
            {
                throw new InvalidOperationException("Service provider not available");
            }

            this.LogSuccess("Service provider obtained");

            // Step 2: Force creation and initialization of singleton services
            UpdateStatus("Initializing core services...");
            this.LogInfo("Creating and initializing singleton services...");

            // Force creation of SupabaseService singleton and initialize
            var supabaseService = services.GetRequiredService<SupabaseService>();
            this.LogSuccess("SupabaseService singleton obtained");

            // Ensure service is initialized
            if (!supabaseService.IsInitialized)
            {
                this.LogInfo("SupabaseService not initialized - initializing now...");
                await supabaseService.InitializeAsync();
                this.LogSuccess("SupabaseService initialized successfully");
            }
            else
            {
                this.LogSuccess("SupabaseService already initialized");
            }

            // Force creation of other singleton services
            var familyService = services.GetRequiredService<SupabaseFamilyService>();
            var familyRepo = services.GetRequiredService<IFamilyRepository>();
            var navigationService = services.GetRequiredService<INavigationService>();

            this.LogSuccess("All singleton services created and available");

            // Step 3: Verify state after initialization
            UpdateStatus("Verifying initialization...");
            this.LogInfo("Verifying singleton initialization...");

            this.LogInfo($"SupabaseService.IsInitialized: {supabaseService.IsInitialized}");
            this.LogInfo($"SupabaseService.IsAuthenticated: {supabaseService.IsAuthenticated}");

            if (!supabaseService.IsInitialized)
            {
                throw new InvalidOperationException("SupabaseService failed to initialize");
            }

            // Step 4: Check for existing authentication session
            UpdateStatus("Checking authentication...");
            this.LogInfo("Checking for existing session...");

            bool hasValidSession = await supabaseService.RestoreSessionAsync();
            this.LogInfo($"Session restore result: {hasValidSession}");

            if (hasValidSession)
            {
                var user = supabaseService.GetCurrentUser();
                this.LogSuccess($"Valid session found for user: {user?.Email}");
                this.LogInfo($"User ID: {user?.Id}");
            }
            else
            {
                this.LogWarning("No valid session found - user needs to login");
            }

            // Step 5: Final verification before navigation
            this.LogInfo("=== FINAL VERIFICATION BEFORE NAVIGATION ===");
            this.LogInfo($"SupabaseService initialized: {supabaseService.IsInitialized}");
            this.LogInfo($"SupabaseService authenticated: {supabaseService.IsAuthenticated}");
            this.LogInfo("Services available for dependency injection");

            // Add delay for smooth transition using constants
            await Task.Delay(700);

            // Step 6: Perform exit animation
            await PerformExitAnimation();

            // Step 7: Navigate based on authentication status
            this.LogInfo("Navigating based on session status...");

            if (hasValidSession)
            {
                this.LogInfo("Navigating to main app...");
                await this.SafeNavigationExecuteAsync(async () =>
                {
                    await navigationService.NavigateToMainAsync();
                }, "Main app navigation");
            }
            else
            {
                this.LogInfo("Navigating to login...");
                await this.SafeNavigationExecuteAsync(async () =>
                {
                    await navigationService.NavigateToLoginAsync();
                }, "Login navigation");
            }

            this.LogSuccess("=== APP INITIALIZATION WITH SINGLETONS COMPLETED ===");

        }
        catch (Exception ex)
        {
            this.LogError(ex, "App initialization failed");

            // Enhanced error handling with user feedback
            UpdateStatus("Initialization failed...");
            await Task.Delay(1000);

            var errorHandled = await this.SafeExecuteAsync(async () =>
            {
                await this.ShowErrorToast("Failed to initialize the app");

                // Try to navigate to login as fallback
                var services = IPlatformApplication.Current?.Services;
                if (services != null)
                {
                    var navigationService = services.GetRequiredService<INavigationService>();
                    await navigationService.NavigateToLoginAsync();
                }
            }, "Fallback navigation");

            if (!errorHandled)
            {
                this.LogError("Even fallback navigation failed - closing app");
                Application.Current?.Quit();
            }
        }
    }

    /// <summary>
    /// Update status label with smooth fade animation using constants
    /// </summary>
    private async void UpdateStatus(string message)
    {
        await this.SafeAnimationExecuteAsync(async () =>
        {
            await StatusLabel.FadeTo(
                AnimationConstants.STATUS_FADE_OPACITY,
                AnimationConstants.STATUS_LABEL_FADE_DURATION);

            StatusLabel.Text = message;

            await StatusLabel.FadeTo(
                AnimationConstants.FULL_OPACITY,
                AnimationConstants.STATUS_LABEL_FADE_DURATION);

            this.LogInfo($"Status: {message}");
        }, "Status update animation");
    }

    /// <summary>
    /// Perform enhanced exit animation before navigation using constants
    /// </summary>
    private async Task PerformExitAnimation()
    {
        await this.SafeAnimationExecuteAsync(async () =>
        {
            LoadingIndicator.IsRunning = false;

            // Enhanced exit with multiple elements fading using constants
            await Task.WhenAll(
                LogoImage.ScaleTo(
                    AnimationConstants.PAGE_ENTRANCE_INITIAL_SCALE,
                    AnimationConstants.PAGE_EXIT_DURATION,
                    AnimationConstants.EXIT_EASING),
                StatusLabel.FadeTo(
                    AnimationConstants.INITIAL_OPACITY,
                    AnimationConstants.PAGE_EXIT_DURATION,
                    AnimationConstants.EXIT_EASING),
                LoadingIndicator.FadeTo(
                    AnimationConstants.INITIAL_OPACITY,
                    AnimationConstants.PAGE_EXIT_DURATION,
                    AnimationConstants.EXIT_EASING)
            );

            // Final fade out of entire screen
            await RootGrid.FadeTo(
                AnimationConstants.INITIAL_OPACITY,
                AnimationConstants.PAGE_EXIT_DURATION,
                AnimationConstants.EXIT_EASING);

            this.LogSuccess("Splash exit animation completed");
        }, "Splash exit animation");
    }
}