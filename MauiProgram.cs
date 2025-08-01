using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using OrchidPro.Config;
using OrchidPro.Extensions;
using OrchidPro.Services;
using OrchidPro.Services.Data;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Families;
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

            // Data services with caching and performance optimization
            services.AddSingleton<SupabaseFamilyService>();
            services.AddSingleton<IFamilyRepository, FamilyRepository>();

            // ViewModels with transient lifetime for fresh state per navigation
            services.AddTransient<FamiliesListViewModel>();
            services.AddTransient<FamilyEditViewModel>();

            // Pages with transient lifetime for proper lifecycle management
            services.AddTransient<SplashPage>();
            services.AddTransient<LoginPage>();
            services.AddTransient<MainPage>();
            services.AddTransient<FamiliesListPage>();
            services.AddTransient<FamilyEditPage>();
            services.AddTransient<TestSyncPage>();

            // Application shell with singleton lifetime
            services.AddSingleton<AppShell>();

            RegisterRoutes();

            typeof(MauiProgram).LogSuccess("Successfully configured services");
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

            Routing.RegisterRoute("familyedit", typeof(FamilyEditPage));
            Routing.RegisterRoute("families-syncfusion", typeof(FamiliesListPage));

            if (AppSettings.IsDebugMode)
            {
                Routing.RegisterRoute("testsync", typeof(TestSyncPage));
                typeof(MauiProgram).LogInfo("Debug routes registered");
            }

            var futureRoutes = new[]
            {
                "genera", "species", "orchids", "schedule",
                "health", "reports", "statistics", "settings", "sync"
            };

            foreach (var route in futureRoutes)
            {
                Routing.RegisterRoute(route, typeof(MainPage));
            }

            typeof(MauiProgram).LogSuccess("Successfully registered navigation routes");
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
            }

            typeof(MauiProgram).LogSuccess($"OrchidPro {AppSettings.ApplicationVersion} started successfully in {AppSettings.Environment} mode");
        }
        catch (Exception ex)
        {
            typeof(MauiProgram).LogWarning($"Failed to log startup configuration: {ex.Message}");
        }
    }
}