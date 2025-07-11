using OrchidPro.Services.Data;
using OrchidPro.Services.Navigation;

namespace OrchidPro.Views.Pages;

/// <summary>
/// Login page that handles user authentication
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

        // Ensure navigation bar is hidden
        Shell.SetNavBarIsVisible(this, false);
        NavigationPage.SetHasNavigationBar(this, false);

        // Perform entrance animation with fade in
        await PerformEntranceAnimation();
    }

    /// <summary>
    /// Animates the login card entrance with fade in
    /// </summary>
    private async Task PerformEntranceAnimation()
    {
        // Fade in root grid
        await RootGrid.FadeTo(1, 400, Easing.CubicOut);

        // Scale in login card with bounce effect
        await LoginCard.ScaleTo(1, 500, Easing.SpringOut);
    }

    /// <summary>
    /// Handles the login button click event
    /// </summary>
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        // Get and validate input
        string? email = EmailEntry.Text?.Trim();
        string? password = PasswordEntry.Text;

        if (!ValidateInput(email, password))
        {
            return;
        }

        try
        {
            SetLoadingState(true);
            HideError();

            // CORREÇÃO: Garantir que o serviço está inicializado
            if (_supabaseService.Client == null)
            {
                await _supabaseService.InitializeAsync();
            }

            // Attempt authentication
            if (_supabaseService.Client != null)
            {
                var response = await _supabaseService.Client.Auth.SignIn(email!, password!);

                if (response?.User != null)
                {
                    // Save session
                    _supabaseService.SaveSession();

                    // Animate success with fade out
                    await AnimateLoginSuccess();

                    // Navigate to main app
                    await _navigationService.NavigateToMainAsync();
                }
                else
                {
                    ShowError("Invalid credentials");
                    // Shake já é chamado no ShowError
                }
            }
            else
            {
                ShowError("Authentication service not initialized");
                // Shake já é chamado no ShowError
            }
        }
        catch (Exception ex)
        {
            ShowError($"Login failed: {ex.Message}");
            // Shake já é chamado no ShowError
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    /// <summary>
    /// Validates user input
    /// </summary>
    private bool ValidateInput(string? email, string? password)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ShowError("Please enter your email");
            EmailEntry.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowError("Please enter your password");
            PasswordEntry.Focus();
            return false;
        }

        // Basic email validation
        if (!email.Contains("@") || !email.Contains("."))
        {
            ShowError("Please enter a valid email address");
            EmailEntry.Focus();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Shows error message with animation AND SHAKE
    /// </summary>
    private async void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
        await ErrorLabel.FadeTo(1, 200);

        // SEMPRE fazer shake quando mostrar erro
        _ = ShakeLoginCard(); // Fire and forget
    }

    /// <summary>
    /// Hides error message
    /// </summary>
    private void HideError()
    {
        ErrorLabel.IsVisible = false;
        ErrorLabel.Text = string.Empty;
    }

    /// <summary>
    /// Sets the loading state of the form
    /// </summary>
    private void SetLoadingState(bool isLoading)
    {
        LoadingIndicator.IsVisible = isLoading;
        LoadingIndicator.IsRunning = isLoading;
        LoginButton.IsEnabled = !isLoading;
        EmailEntry.IsEnabled = !isLoading;
        PasswordEntry.IsEnabled = !isLoading;

        // Update button text
        LoginButton.Text = isLoading ? "Signing in..." : "Sign In";
    }

    /// <summary>
    /// Animates the login card with a MORE EVIDENT shake effect - SEMPRE FUNCIONA
    /// </summary>
    private async Task ShakeLoginCard()
    {
        try
        {
            // GARANTIR que a animação sempre funciona
            LoginCard.IsEnabled = false; // Previne múltiplos cliques

            uint duration = 80;
            double amplitude = 25; // Ainda maior para ser bem visível

            // SEQUÊNCIA MAIS DRAMÁTICA
            await LoginCard.TranslateTo(-amplitude, 0, duration, Easing.CubicOut);
            await LoginCard.TranslateTo(amplitude, 0, duration, Easing.CubicOut);
            await LoginCard.TranslateTo(-amplitude * 0.8, 0, duration, Easing.CubicOut);
            await LoginCard.TranslateTo(amplitude * 0.8, 0, duration, Easing.CubicOut);
            await LoginCard.TranslateTo(-amplitude * 0.6, 0, duration, Easing.CubicOut);
            await LoginCard.TranslateTo(amplitude * 0.6, 0, duration, Easing.CubicOut);
            await LoginCard.TranslateTo(-amplitude * 0.3, 0, duration, Easing.CubicOut);
            await LoginCard.TranslateTo(0, 0, duration, Easing.CubicOut);
        }
        catch (Exception ex)
        {
            // Se falhar, pelo menos garantir posição normal
            LoginCard.TranslationX = 0;
            System.Diagnostics.Debug.WriteLine($"Shake animation failed: {ex.Message}");
        }
        finally
        {
            // SEMPRE reabilitar o card
            LoginCard.IsEnabled = true;
        }
    }

    /// <summary>
    /// Animates successful login with fade out
    /// </summary>
    private async Task AnimateLoginSuccess()
    {
        await Task.WhenAll(
            LoginCard.ScaleTo(0.95, 200, Easing.CubicIn),
            RootGrid.FadeTo(0, 300, Easing.CubicIn)
        );
    }
}