using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using OrchidPro.Services;
using OrchidPro.Services.Data;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Families;
using OrchidPro.Views.Pages;
using Syncfusion.Maui.Core.Hosting;
using System;

namespace OrchidPro;

/// <summary>
/// CORRIGIDO: Configures the MAUI application with proper DI for TestSyncPage
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
            .ConfigureSyncfusionCore()
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
    /// CORRIGIDO: Configures dependency injection services with proper TestSyncPage registration
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        // ✅ CORE SERVICES - SINGLETON (shared state across app)
        // These need to maintain state and be shared across the entire application
        services.AddSingleton<SupabaseService>();
        services.AddSingleton<INavigationService, NavigationService>();

        // ✅ SIMPLIFIED DATA SERVICES - SINGLETON (direct Supabase + cache)
        services.AddSingleton<SupabaseFamilyService>();
        services.AddSingleton<IFamilyRepository, FamilyRepository>();

        // ✅ VIEWMODELS - TRANSIENT (new instance per page navigation)
        // ViewModels should be transient to get fresh state per navigation
        services.AddTransient<FamiliesListViewModel>();
        services.AddTransient<FamilyEditViewModel>();
        // ✅ NOVA: Versão Syncfusion(SfListView)
        services.AddTransient<FamiliesListSyncfusionViewModel>();


        // ✅ PAGES - TRANSIENT (new instance per navigation)
        // ✅ CORRIGIDO: Todas as páginas registradas com DI
        services.AddTransient<SplashPage>();
        services.AddTransient<LoginPage>();
        services.AddTransient<MainPage>();
        services.AddTransient<FamiliesListPage>();
        services.AddTransient<FamilyEditPage>();
        services.AddTransient<TestSyncPage>();
        services.AddTransient<FamiliesListSyncfusionPage>();

        // ✅ SHELL - SINGLETON (app navigation structure)
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

        // ✅ CORRIGIDO: Testing and debug routes with proper DI
        Routing.RegisterRoute("testsync", typeof(TestSyncPage));

        // Routes
        Routing.RegisterRoute("families-syncfusion", typeof(FamiliesListSyncfusionPage));

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
    }
}