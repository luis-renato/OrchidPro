using OrchidPro.Services.Data;
using OrchidPro.Views.Pages;

namespace OrchidPro.Services.Navigation;

/// <summary>
/// Service responsible for handling navigation throughout the application
/// Provides smooth transitions between pages
/// </summary>
public class NavigationService(SupabaseService supabaseService) : INavigationService
{
    private readonly SupabaseService _supabaseService = supabaseService;

    /// <summary>
    /// Navigates to a specific route with optional animation
    /// </summary>
    public async Task NavigateToAsync(string route, bool animate = true)
    {
        await NavigateToAsync(route, null, animate);
    }

    /// <summary>
    /// Navigates to a specific route with parameters and optional animation
    /// </summary>
    public async Task NavigateToAsync(string route, Dictionary<string, object>? parameters, bool animate = true)
    {
        if (animate)
        {
            await AnimateTransition();
        }

        if (parameters != null)
        {
            await Shell.Current.GoToAsync(route, parameters);
        }
        else
        {
            await Shell.Current.GoToAsync(route);
        }
    }

    /// <summary>
    /// Navigates back with optional animation
    /// </summary>
    public async Task GoBackAsync(bool animate = true)
    {
        if (animate)
        {
            await AnimateTransition();
        }
        await Shell.Current.GoToAsync("..");
    }

    /// <summary>
    /// ✅ CORRIGIDO: Navigates to login page with ZERO DELAY transition
    /// </summary>
    public async Task NavigateToLoginAsync()
    {
        var app = Application.Current;
        if (app?.Windows.Count > 0)
        {
            // ✅ FIX: Get services from DI to inject into LoginPage
            var services = IPlatformApplication.Current?.Services;
            if (services != null)
            {
                var navigationService = services.GetRequiredService<INavigationService>();
                var loginPage = new LoginPage(_supabaseService, navigationService);
                await InstantTransition(new NavigationPage(loginPage));
            }
            else
            {
                // Fallback: Create LoginPage using this instance as navigation service
                var loginPage = new LoginPage(_supabaseService, this);
                await InstantTransition(new NavigationPage(loginPage));
            }
        }
    }

    /// <summary>
    /// Navigates to main app shell with ZERO DELAY transition
    /// </summary>
    public async Task NavigateToMainAsync()
    {
        var app = Application.Current;
        if (app?.Windows.Count > 0)
        {
            var appShell = new AppShell(_supabaseService);
            await InstantTransition(appShell);
        }
    }

    /// <summary>
    /// TRANSIÇÃO INSTANTÂNEA: Zero delay, máxima velocidade
    /// </summary>
    private static async Task InstantTransition(Page newPage)
    {
        var app = Application.Current;
        if (app?.Windows.Count > 0)
        {
            var window = app.Windows[0];
            var currentPage = window.Page;

            // 1. PREPARAR nova página COM COR ANTES de tudo
            var primaryColor = Color.FromArgb("#A47764");
            newPage.BackgroundColor = primaryColor;

            if (newPage is NavigationPage navPage)
            {
                navPage.BarBackgroundColor = primaryColor;
                navPage.BackgroundColor = primaryColor;
            }

            if (newPage is Shell shell)
            {
                shell.BackgroundColor = primaryColor;
            }

            // 2. OVERLAY INSTANTÂNEO
            var overlay = new ContentPage
            {
                BackgroundColor = primaryColor,
                Content = new Grid { BackgroundColor = primaryColor }
            };

            // 3. SEQUÊNCIA ULTRARRÁPIDA
            if (currentPage != null)
            {
                await currentPage.FadeTo(0, 50, Easing.Linear);
            }

            // ZERO DELAY - Transição instantânea
            window.Page = overlay;
            window.Page = newPage;
            newPage.Opacity = 0;

            // Fade in super rápido
            await newPage.FadeTo(1, 100, Easing.Linear);
        }
    }

    /// <summary>
    /// Animates page transition within Shell navigation
    /// </summary>
    private static async Task AnimateTransition()
    {
        if (Shell.Current?.CurrentPage != null)
        {
            await Shell.Current.CurrentPage.FadeTo(0.9, 50);
            await Shell.Current.CurrentPage.FadeTo(1, 50);
        }
    }
}