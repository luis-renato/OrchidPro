using OrchidPro.Services.Data;
using OrchidPro.Services.Navigation;
using System.Diagnostics;

namespace OrchidPro.Views.Pages;

/// <summary>
/// CORRIGIDO: Login page que salva sessão nos singleton services
/// </summary>
public partial class LoginPage : ContentPage
{
    private readonly SupabaseService _supabaseService;
    private readonly INavigationService _navigationService;

    /// <summary>
    /// ✅ CORRIGIDO: Usar DI para obter singleton services
    /// </summary>
    public LoginPage(SupabaseService supabaseService, INavigationService navigationService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;
        _navigationService = navigationService;

        Debug.WriteLine("📱 LoginPage created with singleton services");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // ✅ VERIFICAR estado dos singletons
        Debug.WriteLine("👁️ === LOGIN PAGE APPEARING ===");
        Debug.WriteLine($"🔧 SupabaseService.IsInitialized: {_supabaseService.IsInitialized}");
        Debug.WriteLine($"🔐 SupabaseService.IsAuthenticated: {_supabaseService.IsAuthenticated}");

        // ✅ GARANTIR que SupabaseService está inicializado
        if (!_supabaseService.IsInitialized)
        {
            Debug.WriteLine("🔄 SupabaseService not initialized - initializing...");
            try
            {
                await _supabaseService.InitializeAsync();
                Debug.WriteLine("✅ SupabaseService initialized in LoginPage");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Failed to initialize SupabaseService: {ex.Message}");
            }
        }

        // Perform entrance animation
        await PerformEntranceAnimation();
    }

    /// <summary>
    /// Performs enhanced entrance animation
    /// </summary>
    private async Task PerformEntranceAnimation()
    {
        // Set initial states
        RootGrid.Opacity = 0;
        LoginCard.Scale = 0.9;
        LogoImage.Scale = 0.8;

        // Animate main container
        await RootGrid.FadeTo(1, 600, Easing.CubicOut);

        // Animate card and logo
        await Task.WhenAll(
            LoginCard.ScaleTo(1, 600, Easing.SpringOut),
            LogoImage.ScaleTo(1, 800, Easing.SpringOut)
        );
    }

    /// <summary>
    /// ✅ CORRIGIDO: Handles login com salvamento correto na sessão singleton
    /// </summary>
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EmailEntry.Text) || string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            await DisplayAlert("Error", "Please enter both email and password", "OK");
            return;
        }

        try
        {
            // Show loading state
            LoginButton.IsVisible = false;
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            ErrorLabel.IsVisible = false;

            Debug.WriteLine("🔐 Attempting login with singleton service...");

            // ✅ GARANTIR que SupabaseService está inicializado
            if (!_supabaseService.IsInitialized)
            {
                Debug.WriteLine("🔄 Initializing SupabaseService during login...");
                await _supabaseService.InitializeAsync();
            }

            Debug.WriteLine("✅ SupabaseService ready for login");

            // Attempt login
            var session = await _supabaseService.Client!.Auth.SignIn(EmailEntry.Text, PasswordEntry.Text);

            if (session?.User != null)
            {
                Debug.WriteLine($"✅ Login successful for user: {session.User.Email}");
                Debug.WriteLine($"✅ User ID: {session.User.Id}");
                Debug.WriteLine($"✅ Access Token: {session.AccessToken?[..20]}...");

                // ✅ CRÍTICO: Salvar sessão no singleton
                _supabaseService.SaveSession();
                Debug.WriteLine("💾 Session saved to singleton service");

                // ✅ VERIFICAÇÃO: Confirmar que sessão foi salva
                var savedSession = Preferences.Get("supabase_session", null);
                Debug.WriteLine($"✅ Session verification: {(string.IsNullOrEmpty(savedSession) ? "FAILED" : "SUCCESS")}");

                // ✅ VERIFICAÇÃO: Estado do singleton após login
                var isAuth = _supabaseService.IsAuthenticated;
                var userId = _supabaseService.GetCurrentUserId();
                Debug.WriteLine($"✅ Singleton state - Authenticated: {isAuth}, UserID: {userId}");

                if (!isAuth)
                {
                    Debug.WriteLine("❌ CRITICAL: Singleton not reflecting authenticated state!");
                    ShowError("Login succeeded but singleton state invalid");
                    return;
                }

                // Add delay for better UX
                await Task.Delay(500);

                // Navigate to main app
                await _navigationService.NavigateToMainAsync();
            }
            else
            {
                Debug.WriteLine("❌ Login failed - no session returned");
                ShowError("Login failed. Please check your credentials.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Login exception: {ex.Message}");
            Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            ShowError($"Login failed: {ex.Message}");
        }
        finally
        {
            // Hide loading state
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            LoginButton.IsVisible = true;
        }
    }

    /// <summary>
    /// Shows error message with animation
    /// </summary>
    private async void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;

        // Animate error appearance
        ErrorLabel.Opacity = 0;
        await ErrorLabel.FadeTo(1, 300);
    }
}