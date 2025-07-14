using OrchidPro.Services.Data;
using OrchidPro.Services.Navigation;
using System.Diagnostics;

namespace OrchidPro.Views.Pages;

/// <summary>
/// Login page with enhanced animations and professional UI
/// CORRIGIDO: Agora salva sessão após login bem-sucedido
/// </summary>
public partial class LoginPage : ContentPage
{
    private readonly SupabaseService _supabaseService;
    private readonly INavigationService _navigationService;

    public LoginPage(SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        // Get navigation service
        var services = MauiProgram.CreateMauiApp().Services;
        _navigationService = services.GetRequiredService<INavigationService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

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
    /// Handles login button click with CORREÇÃO para salvar sessão
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

            Debug.WriteLine("🔐 Attempting login...");

            // Ensure Supabase is initialized
            await _supabaseService.InitializeAsync();
            Debug.WriteLine("✅ Supabase initialized for login");

            // Attempt login
            var session = await _supabaseService.Client!.Auth.SignIn(EmailEntry.Text, PasswordEntry.Text);

            if (session?.User != null)
            {
                Debug.WriteLine($"✅ Login successful for user: {session.User.Email}");
                Debug.WriteLine($"✅ User ID: {session.User.Id}");
                Debug.WriteLine($"✅ Access Token: {session.AccessToken?[..20]}...");

                // 🔥 CORREÇÃO CRÍTICA: Salvar sessão IMEDIATAMENTE após login
                _supabaseService.SaveSession();
                Debug.WriteLine("💾 Session saved to preferences");

                // Verify session was saved
                var savedSession = Preferences.Get("supabase_session", null);
                Debug.WriteLine($"✅ Session verification: {(string.IsNullOrEmpty(savedSession) ? "FAILED" : "SUCCESS")}");

                // Verify authentication state
                var isAuth = _supabaseService.IsAuthenticated;
                var userId = _supabaseService.GetCurrentUserId();
                Debug.WriteLine($"✅ Auth state - Authenticated: {isAuth}, UserID: {userId}");

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