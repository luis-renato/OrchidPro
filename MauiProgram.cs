using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using OrchidPro.Services.Data;
using OrchidPro.Services.Navigation;
using OrchidPro.Views.Pages;

namespace OrchidPro;

/// <summary>
/// Configures the MAUI application with services and dependencies
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Creates and configures the MAUI application
    /// </summary>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureEffects(effects =>
            {
                // Add platform-specific effects if needed
            });

        // Register services
        ConfigureServices(builder.Services);

        // Configure logging
#if DEBUG
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
#else
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
#endif

        return builder.Build();
    }

    /// <summary>
    /// Configures dependency injection services
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        // Core services
        services.AddSingleton<SupabaseService>();
        services.AddSingleton<INavigationService, NavigationService>();

        // ViewModels
        // services.AddTransient<LoginViewModel>();
        // services.AddTransient<MainViewModel>();

        // Pages
        services.AddTransient<SplashPage>();
        services.AddTransient<LoginPage>();
        services.AddTransient<MainPage>();

        // Shell
        services.AddSingleton<AppShell>();
    }
}