using OrchidPro.Services.Data;
using OrchidPro.Views.Pages;

namespace OrchidPro.Services.Navigation;

/// <summary>
/// ✅ CORRIGIDO: NavigationService sem dependência de AppShell com parâmetro
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
    /// ✅ CORRIGIDO: Navigates to login page
    /// </summary>
    public async Task NavigateToLoginAsync()
    {
        var app = Application.Current;
        if (app?.Windows.Count > 0)
        {
            var services = IPlatformApplication.Current?.Services;
            if (services != null)
            {
                var navigationService = services.GetRequiredService<INavigationService>();
                var loginPage = new LoginPage(_supabaseService, navigationService);
                await InstantTransition(new NavigationPage(loginPage));
            }
            else
            {
                var loginPage = new LoginPage(_supabaseService, this);
                await InstantTransition(new NavigationPage(loginPage));
            }
        }
    }

    /// <summary>
    /// ✅ CORRIGIDO: Navigates to main app shell sem parâmetro
    /// </summary>
    public async Task NavigateToMainAsync()
    {
        var app = Application.Current;
        if (app?.Windows.Count > 0)
        {
            // ✅ CORRIGIDO: AppShell sem parâmetro + injetar SupabaseService via DI
            var appShell = new AppShell();
            await InstantTransition(appShell);
        }
    }

    /// <summary>
    /// Smooth transition animation
    /// </summary>
    private static async Task AnimateTransition()
    {
        try
        {
            await Task.Delay(50); // Minimal delay for smooth transition
        }
        catch
        {
            // Ignore animation errors
        }
    }

    /// <summary>
    /// ✅ TRANSIÇÃO INSTANTÂNEA: Zero delay, máxima velocidade
    /// </summary>
    private static async Task InstantTransition(Page newPage)
    {
        var app = Application.Current;
        if (app?.Windows.Count > 0)
        {
            var window = app.Windows[0];

            // Preparar nova página com cor
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

            // Transição instantânea
            window.Page = newPage;

            await Task.Delay(100); // Mínimo para garantir transição suave
        }
    }
}