using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using OrchidPro.Config;
using OrchidPro.Extensions;
using OrchidPro.Services;
using OrchidPro.Services.Data;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Families;
using OrchidPro.ViewModels.Genera; // ✅ NEW: Genus ViewModels
using OrchidPro.Views.Pages;
using Syncfusion.Maui.Core.Hosting;

namespace OrchidPro;

/// <summary>
/// Configures the MAUI application with enterprise dependency injection and centralized configuration management
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Creates and configures the MAUI application with validated settings
    /// </summary>
    public static MauiApp CreateMauiApp()
    {
        if (!ValidateApplicationConfiguration())
        {
            throw new InvalidOperationException("Application configuration validation failed. Check AppSettings.");
        }

        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureSyncfusionCore()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        ConfigureServices(builder.Services);
        ConfigureLogging(builder.Logging);

        var app = builder.Build();
        LogStartupConfiguration();

        return app;
    }

    /// <summary>
    /// Validates application configuration using centralized AppSettings
    /// </summary>
    private static bool ValidateApplicationConfiguration()
    {
        try
        {
            var isValid = AppSettings.ValidateConfiguration();

            if (isValid)
            {
                typeof(MauiProgram).LogSuccess("Application configuration validated successfully");
            }
            else
            {
                typeof(MauiProgram).LogError("Application configuration validation failed");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            typeof(MauiProgram).LogError(ex, "Critical error during configuration validation");
            return false;
        }
    }

    /// <summary>
    /// Configures dependency injection services with application architecture patterns
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        try
        {
            typeof(MauiProgram).LogInfo("Configuring application services");

            // Core services with singleton lifetime
            services.AddSingleton<SupabaseService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // ✅ EXISTING: Family data services
            services.AddSingleton<SupabaseFamilyService>();
            services.AddSingleton<IFamilyRepository, FamilyRepository>();

            // ✅ NEW: Genus data services
            services.AddSingleton<SupabaseGenusService>();
            services.AddSingleton<IGenusRepository, GenusRepository>();

            // ✅ EXISTING: Family ViewModels
            services.AddTransient<FamiliesListViewModel>();
            services.AddTransient<FamilyEditViewModel>();

            // ✅ NEW: Genus ViewModels
            services.AddTransient<GeneraListViewModel>();
            services.AddTransient<GenusEditViewModel>();

            // ✅ EXISTING: Core pages
            services.AddTransient<SplashPage>();
            services.AddTransient<LoginPage>();
            services.AddTransient<MainPage>();

            // ✅ EXISTING: Family pages
            services.AddTransient<FamiliesListPage>();
            services.AddTransient<FamilyEditPage>();

            // ✅ NEW: Genus pages
            services.AddTransient<GeneraListPage>();
            services.AddTransient<GenusEditPage>();

            // ✅ EXISTING: Debug page
            services.AddTransient<TestSyncPage>();

            // Application shell with singleton lifetime
            services.AddSingleton<AppShell>();

            RegisterRoutes();

            typeof(MauiProgram).LogSuccess("Successfully configured services (including Genus module)");
        }
        catch (Exception ex)
        {
            typeof(MauiProgram).LogError(ex, "Failed to configure application services");
            throw;
        }
    }

    /// <summary>
    /// Configures logging based on environment and debug settings
    /// </summary>
    private static void ConfigureLogging(ILoggingBuilder logging)
    {
        try
        {
            if (AppSettings.IsDebugMode)
            {
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Debug);
                typeof(MauiProgram).LogInfo("Debug logging enabled");
            }
            else
            {
                logging.SetMinimumLevel(LogLevel.Warning);
                typeof(MauiProgram).LogInfo("Production logging enabled");
            }

            if (AppSettings.EnableCrashReporting)
            {
                typeof(MauiProgram).LogInfo("Crash reporting will be enabled");
            }

            if (AppSettings.EnableAnalytics)
            {
                typeof(MauiProgram).LogInfo("Analytics logging will be enabled");
            }
        }
        catch (Exception ex)
        {
            typeof(MauiProgram).LogError(ex, "Failed to configure logging");
        }
    }

    /// <summary>
    /// Registers navigation routes for application routing system
    /// </summary>
    private static void RegisterRoutes()
    {
        try
        {
            typeof(MauiProgram).LogInfo("Registering navigation routes");

            // ✅ EXISTING: Family routes
            Routing.RegisterRoute("familyedit", typeof(FamilyEditPage));
            Routing.RegisterRoute("families-syncfusion", typeof(FamiliesListPage));

            // ✅ NEW: Genus routes
            Routing.RegisterRoute("generalist", typeof(GeneraListPage));
            Routing.RegisterRoute("genusedit", typeof(GenusEditPage));
            Routing.RegisterRoute("genera/list", typeof(GeneraListPage));
            Routing.RegisterRoute("genera/edit", typeof(GenusEditPage));
            Routing.RegisterRoute("genera/add", typeof(GenusEditPage));
            Routing.RegisterRoute("genera/family", typeof(GeneraListPage));

            // ✅ NEW: Parameterized genus routes
            Routing.RegisterRoute("genus/{genusId}/edit", typeof(GenusEditPage));
            Routing.RegisterRoute("genera/family/{familyId}", typeof(GeneraListPage));

            // ✅ EXISTING: Debug routes
            if (AppSettings.IsDebugMode)
            {
                Routing.RegisterRoute("testsync", typeof(TestSyncPage));
                typeof(MauiProgram).LogInfo("Debug routes registered");
            }

            // ✅ UPDATED: Future routes (removed genera since it's now active)
            var futureRoutes = new[]
            {
                "species", "orchids", "schedule",
                "health", "reports", "statistics", "settings", "sync"
            };

            foreach (var route in futureRoutes)
            {
                Routing.RegisterRoute(route, typeof(MainPage));
            }

            typeof(MauiProgram).LogSuccess("Successfully registered navigation routes (including Genus)");
        }
        catch (Exception ex)
        {
            typeof(MauiProgram).LogError(ex, "Failed to register navigation routes");
            throw;
        }
    }

    /// <summary>
    /// Logs startup configuration summary for debugging and monitoring
    /// </summary>
    private static void LogStartupConfiguration()
    {
        try
        {
            if (AppSettings.IsDebugMode)
            {
                var summary = AppSettings.GetConfigurationSummary();
                typeof(MauiProgram).LogInfo($"Startup Configuration:\n{summary}");

                // ✅ NEW: Log Genus module registration
                typeof(MauiProgram).LogInfo("Genus module services registered and ready");
            }

            typeof(MauiProgram).LogSuccess($"OrchidPro {AppSettings.ApplicationVersion} started successfully in {AppSettings.Environment} mode");
        }
        catch (Exception ex)
        {
            typeof(MauiProgram).LogWarning($"Failed to log startup configuration: {ex.Message}");
        }
    }

    // ✅ NEW: Helper method for service validation (optional)
    /// <summary>
    /// Validates that all required services are properly registered
    /// </summary>
    public static bool ValidateServiceRegistration(IServiceProvider services)
    {
        try
        {
            // Test core services
            var supabaseService = services.GetService<SupabaseService>();
            var navigationService = services.GetService<INavigationService>();

            // Test family services
            var familyRepository = services.GetService<IFamilyRepository>();
            var familiesListViewModel = services.GetService<FamiliesListViewModel>();

            // Test genus services
            var genusRepository = services.GetService<IGenusRepository>();
            var generaListViewModel = services.GetService<GeneraListViewModel>();

            var allServicesValid = supabaseService != null &&
                                 navigationService != null &&
                                 familyRepository != null &&
                                 familiesListViewModel != null &&
                                 genusRepository != null &&
                                 generaListViewModel != null;

            if (allServicesValid)
            {
                typeof(MauiProgram).LogSuccess("All services validated successfully");
            }
            else
            {
                typeof(MauiProgram).LogError("Service validation failed - some services are missing");
            }

            return allServicesValid;
        }
        catch (Exception ex)
        {
            typeof(MauiProgram).LogError(ex, "Error during service validation");
            return false;
        }
    }
}