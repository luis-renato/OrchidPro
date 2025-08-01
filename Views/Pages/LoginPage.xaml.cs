using OrchidPro.Services.Data;
using OrchidPro.Services.Navigation;
using OrchidPro.Extensions;
using OrchidPro.Constants;

namespace OrchidPro.Views.Pages;

/// <summary>
/// Login page that manages user authentication and session persistence.
/// Handles Supabase authentication with singleton service management and smooth animations.
/// </summary>
public partial class LoginPage : ContentPage
{
    private readonly SupabaseService _supabaseService;
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Initialize login page with dependency injection for singleton services
    /// </summary>
    public LoginPage(SupabaseService supabaseService, INavigationService navigationService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;
        _navigationService = navigationService;

        this.LogInfo("LoginPage created with singleton services");
    }

    /// <summary>
    /// Handle page appearing with service initialization and animations
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("=== LOGIN PAGE APPEARING ===");
            this.LogInfo($"SupabaseService.IsInitialized: {_supabaseService.IsInitialized}");
            this.LogInfo($"SupabaseService.IsAuthenticated: {_supabaseService.IsAuthenticated}");

            // Ensure SupabaseService is initialized
            if (!_supabaseService.IsInitialized)
            {
                this.LogInfo("SupabaseService not initialized - initializing...");
                await _supabaseService.InitializeAsync();
                this.LogSuccess("SupabaseService initialized in LoginPage");
            }

            // Perform entrance animation
            await PerformEntranceAnimation();
        }, "LoginPage OnAppearing");
    }

    /// <summary>
    /// Perform enhanced entrance animation with logo and card effects
    /// </summary>
    private async Task PerformEntranceAnimation()
    {
        await this.SafeAnimationExecuteAsync(async () =>
        {
            // Set initial states using constants
            RootGrid.Opacity = AnimationConstants.INITIAL_OPACITY;
            LoginCard.Scale = AnimationConstants.PAGE_ENTRANCE_INITIAL_SCALE;
            LogoImage.Scale = AnimationConstants.PAGE_ENTRANCE_INITIAL_SCALE;

            // Animate main container using constants
            await RootGrid.FadeTo(
                AnimationConstants.FULL_OPACITY,
                AnimationConstants.PAGE_ENTRANCE_DURATION,
                AnimationConstants.ENTRANCE_EASING);

            // Animate card and logo in parallel using constants
            await Task.WhenAll(
                LoginCard.ScaleTo(
                    AnimationConstants.FEEDBACK_SCALE_NORMAL,
                    AnimationConstants.PAGE_ENTRANCE_SCALE_DURATION,
                    AnimationConstants.SPRING_EASING),
                LogoImage.ScaleTo(
                    AnimationConstants.FEEDBACK_SCALE_NORMAL,
                    AnimationConstants.SPLASH_LOGO_SCALE_DURATION,
                    AnimationConstants.SPRING_EASING)
            );

            this.LogSuccess("Login entrance animation completed");
        }, "Login entrance animation");
    }

    /// <summary>
    /// Handle login button click with authentication and session management
    /// </summary>
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        try
        {
            // Validate input fields
            if (string.IsNullOrWhiteSpace(EmailEntry.Text) || string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                await this.ShowErrorToast("Please enter both email and password");
                return;
            }

            // Show loading state
            SetLoadingState(true);

            this.LogInfo("Attempting login with singleton service...");

            // Ensure SupabaseService is initialized
            if (!_supabaseService.IsInitialized)
            {
                this.LogInfo("Initializing SupabaseService during login...");
                await _supabaseService.InitializeAsync();
            }

            this.LogSuccess("SupabaseService ready for login");

            // Attempt authentication
            var session = await _supabaseService.Client!.Auth.SignIn(EmailEntry.Text, PasswordEntry.Text);

            if (session?.User != null)
            {
                this.LogSuccess($"Login successful for user: {session.User.Email}");
                this.LogInfo($"User ID: {session.User.Id}");
                this.LogInfo($"Access Token: {session.AccessToken?[..20]}...");

                // Save session to singleton service
                _supabaseService.SaveSession();
                this.LogSuccess("Session saved to singleton service");

                // Verify session was saved correctly
                var savedSession = Preferences.Get("supabase_session", null);
                var sessionVerified = !string.IsNullOrEmpty(savedSession);
                this.LogInfo($"Session verification: {(sessionVerified ? "SUCCESS" : "FAILED")}");

                // Verify singleton state after login
                var isAuth = _supabaseService.IsAuthenticated;
                var userId = _supabaseService.GetCurrentUserId();
                this.LogInfo($"Singleton state - Authenticated: {isAuth}, UserID: {userId}");

                if (!isAuth)
                {
                    this.LogError("CRITICAL: Singleton not reflecting authenticated state!");
                    await ShowErrorMessage("Login succeeded but singleton state invalid");
                    return;
                }

                // Brief delay for better user experience
                await Task.Delay(500);

                // Navigate to main application
                await this.SafeNavigationExecuteAsync(async () =>
                {
                    await _navigationService.NavigateToMainAsync();
                }, "Main app navigation");
            }
            else
            {
                this.LogError("Login failed - no session returned");
                await ShowErrorMessage("Login failed. Please check your credentials.");
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Login process failed");
            await ShowErrorMessage($"Login failed: {ex.Message}");
        }
        finally
        {
            // Always hide loading state
            SetLoadingState(false);
        }
    }

    /// <summary>
    /// Set loading state for login button and indicator
    /// </summary>
    private void SetLoadingState(bool isLoading)
    {
        this.SafeExecute(() =>
        {
            LoginButton.IsVisible = !isLoading;
            LoadingIndicator.IsVisible = isLoading;
            LoadingIndicator.IsRunning = isLoading;

            if (!isLoading)
            {
                ErrorLabel.IsVisible = false;
            }

            this.LogInfo($"Loading state: {(isLoading ? "ACTIVE" : "INACTIVE")}");
        }, "SetLoadingState");
    }

    /// <summary>
    /// Display error message with smooth animation using constants
    /// </summary>
    private async Task ShowErrorMessage(string message)
    {
        await this.SafeAnimationExecuteAsync(async () =>
        {
            ErrorLabel.Text = message;
            ErrorLabel.IsVisible = true;

            // Animate error appearance using constants
            ErrorLabel.Opacity = AnimationConstants.INITIAL_OPACITY;
            await ErrorLabel.FadeTo(
                AnimationConstants.FULL_OPACITY,
                AnimationConstants.STATUS_LABEL_FADE_DURATION);

            this.LogInfo($"Error message displayed: {message}");
        }, "Error message animation");
    }
}