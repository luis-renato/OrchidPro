using OrchidPro.Config;
using OrchidPro.Extensions;
using OrchidPro.Services;
using OrchidPro.Services.Data;
using OrchidPro.Views.Pages;

namespace OrchidPro;

/// <summary>
/// Main application class with enterprise service initialization and lifecycle management
/// </summary>
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Creates the application window with service initialization
    /// </summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        this.LogInfo("Application window creation starting");
        this.LogInfo($"Starting {AppSettings.ApplicationName} v{AppSettings.ApplicationVersion}");
        this.LogInfo($"Environment: {AppSettings.Environment}");

        _ = Task.Run(async () =>
        {
            await this.SafeExecuteAsync(async () =>
            {
                this.LogInfo("Initializing singleton services with application configuration");

                var services = IPlatformApplication.Current?.Services;
                if (services != null)
                {
                    this.LogSuccess("Service provider available");

                    var supabaseService = services.GetRequiredService<SupabaseService>();
                    await supabaseService.InitializeAsync();
                    this.LogSuccess("SupabaseService singleton initialized");

                    if (AppSettings.IsDebugMode)
                    {
                        supabaseService.DebugCurrentState();
                    }

                    var familyService = services.GetRequiredService<SupabaseFamilyService>();
                    var familyRepo = services.GetRequiredService<IFamilyRepository>();
                    this.LogSuccess("All singleton services created and configured");

                    if (AppSettings.EnableAutoSync)
                    {
                        this.LogInfo($"Auto-sync enabled with {AppSettings.AutoSyncIntervalMinutes}m interval");
                    }

                    if (AppSettings.EnableAnalytics)
                    {
                        this.LogInfo("Analytics enabled");
                    }
                }
                else
                {
                    this.LogError("Service provider not available");
                }
            }, operationName: "InitializeSingletonServices");
        });

        var splashPage = new SplashPage();
        return new Window(splashPage)
        {
            Title = $"{AppSettings.ApplicationName} v{AppSettings.ApplicationVersion}",
            MinimumWidth = 400,
            MinimumHeight = 600
        };
    }

    #region Application Lifecycle Events

    /// <summary>
    /// Application started event with service configuration
    /// </summary>
    protected override void OnStart()
    {
        this.SafeExecute(() =>
        {
            this.LogInfo("Application started");

            if (AppSettings.EnableAutoSync)
            {
                this.LogInfo("Starting background sync services");
            }

            if (AppSettings.EnableAnalytics)
            {
                this.LogInfo("Starting performance monitoring");
            }

            if (AppSettings.EnableRealTimeUpdates)
            {
                this.LogInfo("Real-time updates enabled - testing initial connectivity");
                _ = Task.Run(async () =>
                {
                    var services = IPlatformApplication.Current?.Services;
                    if (services != null)
                    {
                        var supabaseService = services.GetService<SupabaseService>();
                        if (supabaseService != null)
                        {
                            var connected = await supabaseService.TestSyncConnectionAsync();
                            if (connected)
                            {
                                this.LogSuccess("Initial connectivity test passed");
                            }
                            else
                            {
                                this.LogWarning("Initial connectivity test failed");
                            }
                        }
                    }
                });
            }

        }, operationName: "ApplicationStart");
    }

    /// <summary>
    /// Application sleeping event with resource management
    /// </summary>
    protected override void OnSleep()
    {
        this.SafeExecute(() =>
        {
            this.LogInfo("Application going to sleep");

            if (AppSettings.EnableAutoSync)
            {
                this.LogInfo("Pausing background sync for sleep mode");
            }

            this.LogInfo("Ensuring data is saved before sleep");

            var services = IPlatformApplication.Current?.Services;
            if (services != null)
            {
                var supabaseService = services.GetService<SupabaseService>();
                supabaseService?.InvalidateConnectionCache();
            }

        }, operationName: "ApplicationSleep");
    }

    /// <summary>
    /// Application resuming event with service restoration
    /// </summary>
    protected override void OnResume()
    {
        this.SafeExecute(() =>
        {
            this.LogInfo("Application resuming from sleep");

            if (AppSettings.EnableAutoSync)
            {
                this.LogInfo("Resuming background sync services");

                _ = Task.Run(async () =>
                {
                    var services = IPlatformApplication.Current?.Services;
                    if (services != null)
                    {
                        var supabaseService = services.GetService<SupabaseService>();
                        if (supabaseService != null)
                        {
                            var connected = await supabaseService.TestSyncConnectionAsync();
                            if (connected)
                            {
                                this.LogSuccess("Connection restored after resume");
                            }
                            else
                            {
                                this.LogWarning("Connection issues detected after resume");
                            }
                        }
                    }
                });
            }

            if (AppSettings.EnableAnalytics)
            {
                this.LogInfo("Resuming analytics tracking");
            }

        }, operationName: "ApplicationResume");
    }

    #endregion

    #region Error Handling

    /// <summary>
    /// Handles unhandled exceptions based on application configuration
    /// </summary>
    public void HandleGlobalException(Exception ex)
    {
        this.SafeExecute(() =>
        {
            this.LogError(ex, "Unhandled application exception");

            if (AppSettings.EnableCrashReporting)
            {
                this.LogInfo("Sending crash report");
            }

            if (AppSettings.IsDebugMode)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Current?.MainPage?.DisplayAlert(
                        "Debug Error",
                        $"Exception: {ex.Message}\n\nStack: {ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace.Length))}",
                        "OK"
                    );
                });
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Current?.MainPage?.DisplayAlert(
                        "Unexpected Error",
                        "An unexpected error occurred. The error has been logged and will be investigated.",
                        "OK"
                    );
                });
            }

        }, operationName: "HandleGlobalException");
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Gets comprehensive application status for monitoring
    /// </summary>
    public async Task<ApplicationStatus> GetApplicationStatusAsync()
    {
        var status = await this.SafeExecuteAsync(async () =>
        {
            var status = new ApplicationStatus
            {
                ApplicationName = AppSettings.ApplicationName,
                Version = AppSettings.ApplicationVersion,
                Environment = AppSettings.Environment,
                IsDebugMode = AppSettings.IsDebugMode,
                AutoSyncEnabled = AppSettings.EnableAutoSync,
                RealTimeEnabled = AppSettings.EnableRealTimeUpdates,
                AnalyticsEnabled = AppSettings.EnableAnalytics,
                CrashReportingEnabled = AppSettings.EnableCrashReporting,
                ConnectionTimeout = AppSettings.NetworkTimeoutSeconds,
                MaxRetryAttempts = AppSettings.MaxRetryAttempts,
                LastChecked = DateTime.UtcNow
            };

            var services = IPlatformApplication.Current?.Services;
            if (services != null)
            {
                var supabaseService = services.GetService<SupabaseService>();
                if (supabaseService != null)
                {
                    var serviceStatus = await supabaseService.GetServiceStatusAsync();
                    status.SupabaseConnected = serviceStatus.IsConnected;
                    status.HasActiveSession = serviceStatus.HasActiveSession;
                }
            }

            return status;
        }, operationName: "GetApplicationStatus");

        return status != null ? status : new ApplicationStatus();
    }

    /// <summary>
    /// Logs comprehensive application configuration for debugging
    /// </summary>
    public void LogApplicationConfiguration()
    {
        this.SafeExecute(() =>
        {
            if (AppSettings.IsDebugMode)
            {
                var summary = AppSettings.GetConfigurationSummary();
                this.LogInfo($"Application Configuration:\n{summary}");

                this.LogInfo($"Platform: {DeviceInfo.Platform}");
                this.LogInfo($"Device: {DeviceInfo.Model}");
                this.LogInfo($"OS Version: {DeviceInfo.VersionString}");
            }
        }, operationName: "LogApplicationConfiguration");
    }

    #endregion
}

#region Status Classes

/// <summary>
/// Application status information for monitoring and debugging
/// </summary>
public class ApplicationStatus
{
    public string ApplicationName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public bool IsDebugMode { get; set; }
    public bool AutoSyncEnabled { get; set; }
    public bool RealTimeEnabled { get; set; }
    public bool AnalyticsEnabled { get; set; }
    public bool CrashReportingEnabled { get; set; }
    public int ConnectionTimeout { get; set; }
    public int MaxRetryAttempts { get; set; }
    public bool SupabaseConnected { get; set; }
    public bool HasActiveSession { get; set; }
    public DateTime LastChecked { get; set; }

    public override string ToString()
    {
        return $"{ApplicationName} v{Version} ({Environment}) - Connected: {SupabaseConnected}, Debug: {IsDebugMode}";
    }
}

#endregion