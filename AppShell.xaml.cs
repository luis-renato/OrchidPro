using OrchidPro.Config;
using OrchidPro.Extensions;
using OrchidPro.Services.Data;
using OrchidPro.Services.Navigation;
using OrchidPro.Views.Pages;

namespace OrchidPro;

/// <summary>
/// Application shell with enterprise navigation structure and user session management
/// </summary>
public partial class AppShell : Shell
{
    public AppShell()
    {
        // Register Syncfusion license
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Mzk1ODIxMEAzMzMwMmUzMDJlMzAzYjMzMzAzYkRoeUFQTmxzYk00RkNhN0M0QTZtYzZPcWF5YnViT3Z0Y2tMZlcvWGh4R289");

        InitializeComponent();
        RegisterRoutes();
        ConfigureUI();

        this.LogSuccess("AppShell initialized with navigation structure");
    }

    /// <summary>
    /// Registers all navigation routes for the application
    /// </summary>
    private void RegisterRoutes()
    {
        this.SafeExecute(() =>
        {
            this.LogInfo("Registering navigation routes");

            // Family management routes
            Routing.RegisterRoute("familyedit", typeof(FamilyEditPage));
            Routing.RegisterRoute("familieslist", typeof(FamiliesListPage));

            // Authentication routes
            Routing.RegisterRoute("login", typeof(LoginPage));

            // Debug routes (only in debug mode)
            if (AppSettings.IsDebugMode)
            {
                Routing.RegisterRoute("testsync", typeof(TestSyncPage));
                this.LogInfo("Debug routes registered");
            }

            // Future module routes (placeholders)
            // Routing.RegisterRoute("genusedit", typeof(GenusEditPage));
            // Routing.RegisterRoute("speciesedit", typeof(SpeciesEditPage));

            this.LogSuccess("All navigation routes registered successfully");
        }, operationName: "RegisterRoutes");
    }

    /// <summary>
    /// Configures UI elements with dynamic content
    /// </summary>
    private void ConfigureUI()
    {
        this.SafeExecute(() =>
        {
            // Set dynamic version information
            if (this.FindByName("VersionLabel") is Label versionLabel)
            {
                versionLabel.Text = GetAppVersion();
            }

            if (this.FindByName("BuildLabel") is Label buildLabel)
            {
                buildLabel.Text = GetBuildInfo();
            }

            // Hide debug section in production
            if (!AppSettings.IsDebugMode)
            {
                if (this.FindByName("DebugSection") is FlyoutItem debugSection)
                {
                    debugSection.IsVisible = false;
                }
            }

            this.LogInfo("UI configured with dynamic content");
        }, operationName: "ConfigureUI");
    }

    /// <summary>
    /// Handles user logout with confirmation and proper cleanup
    /// </summary>
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            // Enhanced visual feedback animation using AnimationExtensions
            if (sender is Grid menuItem)
            {
                await menuItem.PerformTapFeedbackAsync();
            }

            // Confirmation dialog
            bool confirm = await DisplayAlert(
                "Sign Out",
                "Are you sure you want to sign out of OrchidPro?",
                "Sign Out",
                "Cancel"
            );

            if (confirm)
            {
                await PerformLogoutAsync();
            }
            else
            {
                this.LogInfo("Logout cancelled by user");
            }
        }, operationName: "LogoutClicked");
    }

    /// <summary>
    /// Performs the actual logout process with service cleanup
    /// </summary>
    private async Task PerformLogoutAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Starting logout process");

            var services = IPlatformApplication.Current?.Services;
            if (services == null)
            {
                this.LogError("Could not access services for logout");
                await DisplayAlert("Error", "Unable to access system services.", "OK");
                return;
            }

            // Get required services
            var supabaseService = services.GetRequiredService<SupabaseService>();
            var navigationService = services.GetRequiredService<INavigationService>();

            // Perform logout with proper cleanup
            this.LogInfo("Performing user logout");
            supabaseService.Logout();

            // Navigate to login page
            this.LogInfo("Navigating to login page");
            await navigationService.NavigateToLoginAsync();

            this.LogSuccess("Logout completed successfully");
        }, operationName: "PerformLogout");
    }

    /// <summary>
    /// Gets current application version information
    /// </summary>
    public static string GetAppVersion()
    {
        try
        {
            // Try to get actual version from AppInfo first
            var actualVersion = AppInfo.Current.VersionString;
            return $"{AppSettings.ApplicationName} v{actualVersion}";
        }
        catch
        {
            // Fallback to AppSettings if AppInfo fails
            return $"{AppSettings.ApplicationName} v{AppSettings.ApplicationVersion}";
        }
    }

    /// <summary>
    /// Gets application build information with intelligent build number
    /// </summary>
    public static string GetBuildInfo()
    {
        try
        {
            var environment = AppSettings.Environment;

            // Try to get actual build number from AppInfo
            var buildNumber = AppInfo.Current.BuildString;

            // If we have a real build number, use it
            if (!string.IsNullOrEmpty(buildNumber) && buildNumber != "1")
            {
                return $"{environment} Build {buildNumber}";
            }

            // Otherwise, use compilation timestamp
            var buildDate = GetCompilationTimestamp();
            return $"{environment} Build {buildDate}";
        }
        catch
        {
            return $"{AppSettings.Environment} Build {DateTime.Now:yyyy.MM.dd}";
        }
    }

    /// <summary>
    /// Gets compilation timestamp for more accurate build information
    /// </summary>
    private static string GetCompilationTimestamp()
    {
        try
        {
            // Get assembly creation time as proxy for build time
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var creationTime = System.IO.File.GetCreationTime(assembly.Location);
            return creationTime.ToString("yyyy.MM.dd.HHmm");
        }
        catch
        {
            // Ultimate fallback
            return DateTime.Now.ToString("yyyy.MM.dd");
        }
    }

    /// <summary>
    /// Gets comprehensive application information for display
    /// </summary>
    public static string GetApplicationInfo()
    {
        try
        {
            return $"{GetAppVersion()} • {GetBuildInfo()}";
        }
        catch
        {
            return "OrchidPro v1.0.0 • Production Build";
        }
    }
}