using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using OrchidPro.Services.Data;
using OrchidPro.Services.Navigation;
using OrchidPro.Services;
using OrchidPro.ViewModels;
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

        // Data services
        services.AddSingleton<ILocalDataService, LocalDataService>();
        services.AddSingleton<SupabaseFamilySync>(); // Novo serviço de sync
        services.AddSingleton<IFamilyRepository, FamilyRepository>();

        // ViewModels
        services.AddTransient<FamiliesListViewModel>();
        services.AddTransient<FamilyEditViewModel>();

        // Pages
        services.AddTransient<SplashPage>();
        services.AddTransient<LoginPage>();
        services.AddTransient<MainPage>();
        services.AddTransient<FamiliesListPage>();
        services.AddTransient<FamilyEditPage>();

        // Shell
        services.AddSingleton<AppShell>();

        // Register routes for navigation
        RegisterRoutes();
    }

    /// <summary>
    /// Registers navigation routes
    /// </summary>
    private static void RegisterRoutes()
    {
        // Family management routes
        Routing.RegisterRoute("families", typeof(FamiliesListPage));
        Routing.RegisterRoute("familyedit", typeof(FamilyEditPage));
        Routing.RegisterRoute("familydetails", typeof(FamilyEditPage));

        // Future routes for other modules
        Routing.RegisterRoute("genera", typeof(MainPage)); // Placeholder
        Routing.RegisterRoute("species", typeof(MainPage)); // Placeholder
        Routing.RegisterRoute("orchids", typeof(MainPage)); // Placeholder
        Routing.RegisterRoute("schedule", typeof(MainPage)); // Placeholder
        Routing.RegisterRoute("health", typeof(MainPage)); // Placeholder
        Routing.RegisterRoute("reports", typeof(MainPage)); // Placeholder
        Routing.RegisterRoute("statistics", typeof(MainPage)); // Placeholder
        Routing.RegisterRoute("settings", typeof(MainPage)); // Placeholder
        Routing.RegisterRoute("sync", typeof(MainPage)); // Placeholder

        Routing.RegisterRoute("testsync", typeof(TestSyncPage));
    }
}