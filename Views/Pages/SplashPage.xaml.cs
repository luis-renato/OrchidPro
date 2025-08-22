using OrchidPro.Services;
using OrchidPro.Services.Data;
using OrchidPro.Services.Navigation;
using OrchidPro.Extensions;
using OrchidPro.Constants;

namespace OrchidPro.Views.Pages;

/// <summary>
/// Enterprise-grade splash screen implementation for OrchidPro application startup.
/// 
/// ARCHITECTURE:
/// - Implements concurrent initialization pattern to minimize startup time
/// - Uses semaphore-based synchronization to prevent race conditions
/// - Employs staggered animation system to reduce CPU load during startup
/// - Integrates comprehensive error recovery with automatic fallback mechanisms
/// 
/// PERFORMANCE OPTIMIZATIONS:
/// - Concurrent execution of animations and service initialization
/// - Background thread pre-warming of critical services
/// - Optimized async patterns with proper Task.FromResult usage
/// - Memory-efficient resource management with proactive disposal
/// 
/// RELIABILITY FEATURES:
/// - Timeout-based service initialization to prevent hanging
/// - Multi-level error handling with graceful degradation
/// - Thread-safe disposal pattern with ObjectDisposedException handling
/// - Automatic navigation fallback on critical failures
/// 
/// FLOW:
/// 1. OnAppearing: Starts concurrent animation + initialization tasks
/// 2. Animation: Staggered entrance animations with loading indicators
/// 3. Initialization: Service validation → Session restoration → Authentication check → Navigation prep
/// 4. Navigation: Route to main app or login based on session status
/// 5. Cleanup: Safe resource disposal on page disappearing
/// </summary>
public partial class SplashPage : ContentPage
{
    // Thread synchronization and state management fields
    private bool _isInitialized = false;
    private readonly SemaphoreSlim _initSemaphore = new(1, 1);
    private bool _isDisposed = false;

    /// <summary>
    /// Initialize splash page with logging setup for diagnostic tracking.
    /// </summary>
    public SplashPage()
    {
        InitializeComponent();
        this.LogInfo("SplashPage created");
    }

    /// <summary>
    /// Entry point for splash screen lifecycle - orchestrates concurrent startup operations.
    /// Runs animation and app initialization in parallel to optimize perceived performance.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Execute animation and initialization concurrently for optimal startup performance
        var animationTask = PerformEntranceAnimationAsync();
        var initTask = InitializeAppOptimizedAsync();

        // Wait for both operations to complete before proceeding
        await Task.WhenAll(animationTask, initTask);
    }

    /// <summary>
    /// Orchestrates splash screen entrance animations with staggered timing to reduce CPU load.
    /// Uses SafeAnimationExecuteAsync extension for error-resistant animation handling.
    /// </summary>
    private async Task PerformEntranceAnimationAsync()
    {
        await this.SafeAnimationExecuteAsync(async () =>
        {
            // Initialize UI elements to starting positions using animation constants
            RootGrid.Opacity = AnimationConstants.INITIAL_OPACITY;
            LogoImage.Scale = AnimationConstants.PAGE_ENTRANCE_INITIAL_SCALE;

            // Primary entrance animation - fade in the root container
            await RootGrid.FadeTo(
                AnimationConstants.FULL_OPACITY,
                AnimationConstants.SPLASH_FADE_IN_DURATION,
                AnimationConstants.ENTRANCE_EASING);

            // Secondary animation - scale up logo with spring easing for professional feel
            var logoTask = LogoImage.ScaleTo(
                AnimationConstants.FEEDBACK_SCALE_NORMAL,
                AnimationConstants.SPLASH_LOGO_SCALE_DURATION,
                AnimationConstants.SPRING_EASING);

            // Tertiary animation - start loading indicator with delay to stagger animations
            var _1 = Task.Delay(200).ContinueWith(_ => AnimateLoadingIndicatorAsync());

            await logoTask;

            this.LogSuccess("Splash entrance animation completed");
        }, "Splash entrance animation");
    }

    /// <summary>
    /// Continuous loading indicator animation that pulses while initialization is running.
    /// Automatically terminates when _isInitialized flag is set or LoadingIndicator stops.
    /// Uses reduced frequency pulses to minimize CPU usage during startup.
    /// </summary>
    private async Task AnimateLoadingIndicatorAsync()
    {
        await this.SafeAnimationExecuteAsync(async () =>
        {
            // Continue pulsing animation while initialization is active
            while (LoadingIndicator.IsRunning && !_isInitialized)
            {
                // Scale up phase with controlled duration
                await LoadingIndicator.ScaleTo(1.1,
                    AnimationConstants.SPLASH_LOADING_PULSE_DURATION,
                    AnimationConstants.LOADING_PULSE_EASING);

                // Scale down phase back to normal
                await LoadingIndicator.ScaleTo(
                    AnimationConstants.FEEDBACK_SCALE_NORMAL,
                    AnimationConstants.SPLASH_LOADING_PULSE_DURATION,
                    AnimationConstants.LOADING_PULSE_EASING);

                // Brief pause between pulse cycles to reduce system load
                await Task.Delay(100);
            }
        }, "Loading indicator pulse animation");
    }

    /// <summary>
    /// Core application initialization pipeline with enterprise-grade error handling.
    /// 
    /// PROCESS FLOW:
    /// 1. Semaphore acquisition for thread safety
    /// 2. Service provider validation 
    /// 3. SupabaseService initialization with timeout protection
    /// 4. Session restoration from local storage (CRITICAL FIX)
    /// 5. Authentication session validation
    /// 6. Navigation service preparation
    /// 7. Concurrent exit animation and service pre-warming
    /// 8. Route determination based on authentication status
    /// 
    /// ERROR HANDLING:
    /// - Timeout protection on all async operations
    /// - ObjectDisposedException handling for disposal scenarios
    /// - Comprehensive logging for diagnostic purposes
    /// - Automatic fallback to login on critical failures
    /// </summary>
    private async Task InitializeAppOptimizedAsync()
    {
        // Early disposal check to prevent unnecessary semaphore operations
        if (_isDisposed)
        {
            this.LogWarning("Page disposed - skipping initialization");
            return;
        }

        // Acquire initialization semaphore to prevent concurrent initialization attempts
        bool semaphoreAcquired;
        try
        {
            // Timeout-protected semaphore acquisition to prevent indefinite blocking
            semaphoreAcquired = await _initSemaphore.WaitAsync(TimeSpan.FromSeconds(10));
            if (!semaphoreAcquired)
            {
                this.LogError("Initialization timeout - semaphore wait failed");
                return;
            }
        }
        catch (ObjectDisposedException)
        {
            // Handle disposal during semaphore acquisition gracefully
            this.LogWarning("Semaphore disposed during initialization attempt");
            return;
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Failed to acquire initialization semaphore");
            return;
        }

        try
        {
            // Guard against multiple initialization or disposal during processing
            if (_isInitialized || _isDisposed) return;

            this.LogInfo("=== ENTERPRISE APP INITIALIZATION PIPELINE ===");

            // PHASE 1: Service Provider Validation
            UpdateStatusAsync("Checking services...");
            var services = (IPlatformApplication.Current?.Services) ?? throw new InvalidOperationException("Service provider not available");
            this.LogSuccess("Service provider obtained");

            // PHASE 2: Database Service Initialization
            UpdateStatusAsync("Connecting to services...");
            var supabaseService = await GetOrWaitForSupabaseServiceAsync(services) ?? throw new InvalidOperationException("Failed to initialize SupabaseService");
            this.LogSuccess("SupabaseService ready");

            // PHASE 3: Session Restoration (CRITICAL FIX)
            UpdateStatusAsync("Restoring session...");
            bool sessionRestored = await RestoreSessionIfExistsAsync(supabaseService);
            this.LogInfo($"Session restoration result: {sessionRestored}");

            // PHASE 4: Authentication Session Validation
            UpdateStatusAsync("Checking authentication...");
            bool hasValidSession = await CheckSessionOptimizedAsync(supabaseService);
            this.LogInfo($"Final session check result: {hasValidSession}");

            // PHASE 5: Navigation Service Preparation
            var navigationService = services.GetRequiredService<INavigationService>();

            // PHASE 6: Mark Initialization Complete
            _isInitialized = true;
            LoadingIndicator.IsRunning = false;

            // PHASE 7: Concurrent Exit Animation and Service Pre-warming
            var exitTask = PerformExitAnimationAsync();
            var prepareNavTask = PrepareNavigationAsync(hasValidSession);
            await Task.WhenAll(exitTask, prepareNavTask);

            // PHASE 8: Route to Appropriate Destination
            await NavigateBasedOnSessionAsync(hasValidSession, navigationService);

            this.LogSuccess("=== ENTERPRISE APP INITIALIZATION COMPLETED ===");

        }
        catch (Exception ex)
        {
            this.LogError(ex, "Enterprise app initialization pipeline failed");
            await HandleInitializationErrorAsync(ex);
        }
        finally
        {
            // Safe semaphore release with comprehensive error handling
            if (semaphoreAcquired && !_isDisposed)
            {
                try
                {
                    _initSemaphore.Release();
                }
                catch (ObjectDisposedException)
                {
                    // Expected scenario when page is disposed during initialization
                    this.LogInfo("Semaphore was disposed during cleanup");
                }
                catch (Exception ex)
                {
                    this.LogWarning($"Error releasing semaphore: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Intelligent SupabaseService acquisition with caching and timeout protection.
    /// 
    /// STRATEGY:
    /// 1. Check for cached initialized service first (performance optimization)
    /// 2. If not cached, initialize new service from DI container
    /// 3. Apply 10-second timeout to prevent startup hanging
    /// 4. Ensure exception propagation for proper error handling
    /// 
    /// BENEFITS:
    /// - Reduces redundant service initialization
    /// - Prevents application freeze on network issues
    /// - Maintains startup reliability under adverse conditions
    /// </summary>
    private async Task<SupabaseService?> GetOrWaitForSupabaseServiceAsync(IServiceProvider services)
    {
        // Attempt to use cached service for optimal performance
        var cachedService = App.GetSupabaseService();
        if (cachedService?.IsInitialized == true)
        {
            this.LogInfo("Using cached SupabaseService");
            return cachedService;
        }

        // Initialize new service from dependency injection container
        this.LogInfo("Initializing SupabaseService...");
        var service = services.GetRequiredService<SupabaseService>();

        // Apply timeout protection to prevent indefinite hanging on network issues
        var initTask = service.InitializeAsync();
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));

        var completedTask = await Task.WhenAny(initTask, timeoutTask);

        // Handle timeout scenario gracefully
        if (completedTask == timeoutTask)
        {
            this.LogError("SupabaseService initialization timeout");
            return null;
        }

        // Ensure any initialization exceptions are properly propagated
        await initTask;
        this.LogSuccess("SupabaseService initialized successfully");

        return service;
    }

    /// <summary>
    /// CRITICAL FIX: Restore session from local storage before checking authentication.
    /// This is the missing piece that prevents automatic login on app restart.
    /// 
    /// PROCESS:
    /// 1. Check if there's a saved session in Preferences
    /// 2. If found, attempt to restore it to SupabaseService
    /// 3. Validate the restored session is working
    /// 4. Return restoration status for flow control
    /// 
    /// IMPORTANCE:
    /// Without this step, the app will always show login screen even if user
    /// has a valid saved session from previous login.
    /// </summary>
    private async Task<bool> RestoreSessionIfExistsAsync(SupabaseService supabaseService)
    {
        try
        {
            this.LogInfo("Checking for saved session in preferences...");

            // Check if there's a saved session
            var savedSession = Preferences.Get("supabase_session", null);
            if (string.IsNullOrEmpty(savedSession))
            {
                this.LogInfo("No saved session found - proceeding to login flow");
                return false;
            }

            this.LogInfo("Saved session found - attempting restoration...");

            // Attempt to restore the session
            bool restored = await supabaseService.RestoreSessionAsync();

            if (restored)
            {
                this.LogSuccess("Session restored successfully");

                // Verify the restored session is working
                var currentUser = supabaseService.GetCurrentUser();
                if (currentUser != null)
                {
                    this.LogSuccess($"Session validation successful for user: {currentUser.Email}");
                    return true;
                }
                else
                {
                    this.LogWarning("Session restored but no current user - clearing invalid session");
                    Preferences.Remove("supabase_session");
                    return false;
                }
            }
            else
            {
                this.LogWarning("Session restoration failed - clearing invalid session");
                Preferences.Remove("supabase_session");
                return false;
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error during session restoration");

            // Clear potentially corrupted session data
            try
            {
                Preferences.Remove("supabase_session");
                this.LogInfo("Cleared potentially corrupted session data");
            }
            catch (Exception clearEx)
            {
                this.LogWarning($"Failed to clear session data: {clearEx.Message}");
            }

            return false;
        }
    }

    /// <summary>
    /// High-performance session validation using synchronous operations.
    /// Now simplified since session restoration happens before this check.
    /// 
    /// OPTIMIZATION APPROACH:
    /// - Uses synchronous GetCurrentUser() to avoid network overhead
    /// - Returns Task.FromResult() for consistent async interface
    /// - Session restoration already handled in RestoreSessionIfExistsAsync()
    /// - Comprehensive error handling with fallback to login flow
    /// 
    /// REASONING:
    /// Deep session validation at startup would add network latency.
    /// This approach provides immediate user feedback while deferring
    /// expensive validation until actual API interactions occur.
    /// </summary>
    private Task<bool> CheckSessionOptimizedAsync(SupabaseService supabaseService)
    {
        try
        {
            // Perform lightweight user presence check (no network call required)
            var currentUser = supabaseService.GetCurrentUser();
            if (currentUser is null)
            {
                this.LogInfo("No current user found after restoration - proceeding to login");
                return Task.FromResult(false);
            }

            this.LogInfo($"Current user confirmed: {currentUser.Email}");

            // At this point, session has been restored and user is present
            // Assume session validity based on successful restoration + user presence
            this.LogSuccess($"Valid session confirmed for user: {currentUser.Email}");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            // Log warning and default to login flow for any session check failures
            this.LogWarning($"Session check failed: {ex.Message} - proceeding to login");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Background service pre-warming to optimize subsequent navigation performance.
    /// 
    /// AUTHENTICATED USERS:
    /// - Pre-initializes all critical services (repositories, caches, etc.)
    /// - Reduces perceived loading time when entering main application
    /// 
    /// UNAUTHENTICATED USERS:
    /// - Minimal preparation to avoid unnecessary resource usage
    /// - Quick transition to login screen
    /// 
    /// Runs on background thread to prevent UI blocking during service initialization.
    /// </summary>
    private Task PrepareNavigationAsync(bool hasValidSession)
    {
        return Task.Run(async () =>
        {
            try
            {
                if (hasValidSession)
                {
                    // Pre-warm all critical services for authenticated users
                    await App.EnsureAllServicesInitializedAsync();
                    this.LogInfo("Main app services pre-warmed");
                }
                else
                {
                    // Minimal delay for login navigation preparation
                    await Task.Delay(50);
                    this.LogInfo("Login navigation prepared");
                }
            }
            catch (Exception ex)
            {
                // Log preparation failures but don't block navigation
                this.LogWarning($"Navigation preparation failed: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Intelligent navigation router based on authentication status.
    /// Uses SafeNavigationExecuteAsync extension for error-resistant navigation.
    /// Includes automatic fallback handling for navigation failures.
    /// </summary>
    private async Task NavigateBasedOnSessionAsync(bool hasValidSession, INavigationService navigationService)
    {
        try
        {
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
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Navigation failed");
            await HandleNavigationErrorAsync(ex, navigationService);
        }
    }

    /// <summary>
    /// Multi-level navigation error recovery system.
    /// 
    /// RECOVERY STRATEGY:
    /// 1. First attempt: Fallback to login navigation
    /// 2. Second failure: Show user-friendly error toast
    /// 3. Critical failure: Log error for diagnostics
    /// 
    /// Ensures user never gets stuck on splash screen regardless of navigation failures.
    /// </summary>
    private async Task HandleNavigationErrorAsync(Exception _, INavigationService navigationService)
    {
        try
        {
            this.LogInfo("Attempting fallback navigation to login");
            await navigationService.NavigateToLoginAsync();
        }
        catch (Exception fallbackEx)
        {
            this.LogError(fallbackEx, "Fallback navigation also failed");
            await this.ShowErrorToast("Navigation failed. Please restart the app.");
        }
    }

    /// <summary>
    /// Comprehensive initialization failure recovery with user feedback.
    /// 
    /// RECOVERY PROCESS:
    /// 1. Reset initialization state and stop loading indicators
    /// 2. Display user-friendly error message via toast
    /// 3. Attempt graceful fallback to login screen
    /// 4. Final fallback: Show restart dialog if all recovery attempts fail
    /// 5. Nuclear option: Force application quit if recovery is impossible
    /// 
    /// Ensures application never remains in unrecoverable state.
    /// </summary>
    private async Task HandleInitializationErrorAsync(Exception _)
    {
        // Reset initialization state for potential retry
        _isInitialized = false;
        LoadingIndicator.IsRunning = false;

        // Provide user feedback about the failure
        UpdateStatusAsync("Initialization failed...");
        await Task.Delay(1000);

        // Attempt comprehensive error recovery
        var success = await this.SafeExecuteAsync(async () =>
        {
            await this.ShowErrorToast("Failed to initialize the app. Please check your connection and try again.");

            // Try navigation fallback to login screen
            var services = IPlatformApplication.Current?.Services;
            if (services is not null)
            {
                var navigationService = services.GetService<INavigationService>();
                if (navigationService is not null)
                {
                    await navigationService.NavigateToLoginAsync();
                    return;
                }
            }

            // Final fallback: Show restart instruction dialog
            await DisplayAlert("Initialization Error",
                "The app failed to start properly. Please restart the application.", "OK");

        }, "Fallback error handling");

        // Nuclear option: Force quit if all recovery attempts fail
        if (!success)
        {
            this.LogError("Critical failure - unable to recover");
            Application.Current?.Quit();
        }
    }

    /// <summary>
    /// Asynchronous status label updates with animation to provide user feedback.
    /// 
    /// FEATURES:
    /// - Thread-safe disposal checks before UI operations
    /// - Main thread marshalling for UI updates
    /// - Smooth fade animations for professional feel
    /// - Comprehensive null checking for stability
    /// 
    /// Uses BeginInvokeOnMainThread for optimal performance during startup.
    /// </summary>
    private void UpdateStatusAsync(string message)
    {
        // Prevent UI updates if page has been disposed
        if (_isDisposed) return;

        // Marshal UI update to main thread for thread safety
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // Double-check disposal state on main thread
            if (_isDisposed) return;

            await this.SafeAnimationExecuteAsync(async () =>
            {
                // Verify UI controls still exist before animation
                if (StatusLabel is null) return;

                // Update text and perform smooth fade animation
                StatusLabel.Text = message;
                await StatusLabel.FadeTo(AnimationConstants.STATUS_FADE_OPACITY, 150);
                await StatusLabel.FadeTo(AnimationConstants.FULL_OPACITY, 150);

                this.LogInfo($"Status: {message}");
            }, "Status update animation");
        });
    }

    /// <summary>
    /// Professional exit animation sequence with concurrent operations for smooth transition.
    /// 
    /// ANIMATION PHASES:
    /// 1. Stop loading indicator
    /// 2. Concurrent logo scale-down and status fade-out
    /// 3. Final root grid fade-out with accelerated timing
    /// 
    /// Uses Task.WhenAll for optimal performance during page transition.
    /// </summary>
    private async Task PerformExitAnimationAsync()
    {
        await this.SafeAnimationExecuteAsync(async () =>
        {
            // Immediately stop loading indicator
            LoadingIndicator.IsRunning = false;

            // Execute logo and status animations concurrently for efficiency
            await Task.WhenAll(
                LogoImage.ScaleTo(
                    AnimationConstants.PAGE_ENTRANCE_INITIAL_SCALE,
                    AnimationConstants.PAGE_EXIT_DURATION,
                    AnimationConstants.EXIT_EASING),
                StatusLabel.FadeTo(
                    AnimationConstants.INITIAL_OPACITY,
                    AnimationConstants.PAGE_EXIT_DURATION,
                    AnimationConstants.EXIT_EASING)
            );

            // Final fade out with accelerated timing for smooth transition
            await RootGrid.FadeTo(
                AnimationConstants.INITIAL_OPACITY,
                AnimationConstants.PAGE_EXIT_DURATION / 2,
                AnimationConstants.EXIT_EASING);

            this.LogSuccess("Splash exit animation completed");
        }, "Splash exit animation");
    }

    /// <summary>
    /// Comprehensive resource cleanup and disposal handling.
    /// 
    /// CLEANUP SEQUENCE:
    /// 1. Mark page as disposed to prevent further operations
    /// 2. Stop any running animations and UI updates
    /// 3. Safely dispose semaphore with exception handling
    /// 4. Log disposal completion for diagnostic tracking
    /// 
    /// Handles ObjectDisposedException gracefully for thread-safe disposal.
    /// </summary>
    protected override void OnDisappearing()
    {
        try
        {
            base.OnDisappearing();

            // Prevent any further operations by marking as disposed first
            _isDisposed = true;

            // Stop loading indicator if still running
            if (LoadingIndicator is not null)
            {
                LoadingIndicator.IsRunning = false;
            }

            this.LogInfo("SplashPage disposing...");

            // Safe semaphore disposal with exception handling
            try
            {
                _initSemaphore?.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Expected scenario - semaphore already disposed
            }
            catch (Exception ex)
            {
                this.LogWarning($"Error disposing semaphore: {ex.Message}");
            }

            this.LogInfo("SplashPage disposed successfully");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error during SplashPage disposal");
        }
    }
}