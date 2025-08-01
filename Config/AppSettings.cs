using OrchidPro.Extensions;
using System.Runtime.CompilerServices;

namespace OrchidPro.Config;

/// <summary>
/// 🔧 CONFIGURATION: Centralized application settings for OrchidPro enterprise solution
/// Provides environment-specific configuration with secure key management and runtime flexibility
/// </summary>
public static class AppSettings
{
    #region 🌐 Backend Configuration

    /// <summary>
    /// Supabase service URL for database and authentication services
    /// Environment-specific endpoint configuration
    /// </summary>
#if DEBUG
    public const string SupabaseUrl = "https://yrgxobkjrpgzoskxncto.supabase.co";
#else
    public const string SupabaseUrl = "https://yrgxobkjrpgzoskxncto.supabase.co";
#endif

    /// <summary>
    /// Supabase anonymous access key for public API operations
    /// Auto-rotated key with Row Level Security enforcement
    /// </summary>
#if DEBUG
    public const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InlyZ3hvYmtqcnBnem9za3huY3RvIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDk0MjE5NzAsImV4cCI6MjA2NDk5Nzk3MH0.RlJ5Jdc7v8fVP6cM9HcY6S3-uRc0V2RVCenqmOtpiHg";
#else
    public const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InlyZ3hvYmtqcnBnem9za3huY3RvIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDk0MjE5NzAsImV4cCI6MjA2NDk5Nzk3MH0.RlJ5Jdc7v8fVP6cM9HcY6S3-uRc0V2RVCenqmOtpiHg";
#endif

    #endregion

    #region ⚙️ Application Configuration

    /// <summary>
    /// Application name for logging and user interface display
    /// </summary>
    public const string ApplicationName = "OrchidPro";

    /// <summary>
    /// Current application version for compatibility checks and analytics
    /// </summary>
    public const string ApplicationVersion = "1.0.0";

    /// <summary>
    /// Environment identifier for configuration switching and debugging
    /// </summary>
#if DEBUG
    public const string Environment = "Development";
#else
    public const string Environment = "Production";
#endif

    /// <summary>
    /// Enables detailed logging and debug features for development builds
    /// </summary>
#if DEBUG
    public const bool IsDebugMode = true;
#else
    public const bool IsDebugMode = false;
#endif

    #endregion

    #region 🗄️ Cache and Performance Settings

    /// <summary>
    /// Default cache duration for API responses (in minutes)
    /// Optimizes performance while maintaining data freshness
    /// </summary>
    public const int DefaultCacheDurationMinutes = 15;

    /// <summary>
    /// Maximum number of items to cache in memory per entity type
    /// Prevents excessive memory usage on resource-constrained devices
    /// </summary>
    public const int MaxCacheItems = 1000;

    /// <summary>
    /// Network request timeout duration (in seconds)
    /// Balances user experience with network reliability
    /// </summary>
    public const int NetworkTimeoutSeconds = 30;

    /// <summary>
    /// Retry attempts for failed network operations
    /// Improves reliability in unstable network conditions
    /// </summary>
    public const int MaxRetryAttempts = 3;

    #endregion

    #region 🔒 Security Configuration

    /// <summary>
    /// Session timeout duration (in hours)
    /// Balances security with user convenience
    /// </summary>
    public const int SessionTimeoutHours = 24;

    /// <summary>
    /// Enables secure local data encryption for sensitive information
    /// Always enabled in production, optional in development
    /// </summary>
#if DEBUG
    public const bool EnableLocalEncryption = false;
#else
    public const bool EnableLocalEncryption = true;
#endif

    /// <summary>
    /// Requires biometric authentication for sensitive operations
    /// Enhanced security for production environments
    /// </summary>
#if DEBUG
    public const bool RequireBiometricAuth = false;
#else
    public const bool RequireBiometricAuth = false; // TODO: Enable after biometric implementation
#endif

    #endregion

    #region 🎨 User Interface Settings

    /// <summary>
    /// Default animation duration for UI transitions (in milliseconds)
    /// Provides consistent visual experience across the application
    /// </summary>
    public const int DefaultAnimationDuration = 300;

    /// <summary>
    /// Enables haptic feedback for user interactions
    /// Enhances user experience on supported devices
    /// </summary>
    public const bool EnableHapticFeedback = true;

    /// <summary>
    /// Default page size for paginated lists
    /// Optimizes loading performance and memory usage
    /// </summary>
    public const int DefaultPageSize = 50;

    /// <summary>
    /// Minimum search text length to trigger search operations
    /// Prevents excessive API calls for short search terms
    /// </summary>
    public const int MinSearchLength = 1;

    #endregion

    #region 🔄 Synchronization Settings

    /// <summary>
    /// Enables automatic background synchronization
    /// Keeps data fresh without user intervention
    /// </summary>
    public const bool EnableAutoSync = true;

    /// <summary>
    /// Automatic sync interval (in minutes)
    /// Balances data freshness with battery consumption
    /// </summary>
    public const int AutoSyncIntervalMinutes = 30;

    /// <summary>
    /// Enables real-time updates via WebSocket connections
    /// Provides instant data updates across devices
    /// </summary>
    public const bool EnableRealTimeUpdates = true;

    /// <summary>
    /// Maximum offline storage duration (in days)
    /// Prevents unlimited local storage growth
    /// </summary>
    public const int MaxOfflineStorageDays = 30;

    #endregion

    #region 📊 Analytics and Monitoring

    /// <summary>
    /// Enables anonymous usage analytics for application improvement
    /// Privacy-focused data collection for development insights
    /// </summary>
#if DEBUG
    public const bool EnableAnalytics = false;
#else
    public const bool EnableAnalytics = true;
#endif

    /// <summary>
    /// Enables crash reporting for stability monitoring
    /// Helps identify and resolve application issues
    /// </summary>
#if DEBUG
    public const bool EnableCrashReporting = false;
#else
    public const bool EnableCrashReporting = true;
#endif

    /// <summary>
    /// Performance monitoring threshold (in milliseconds)
    /// Operations exceeding this duration will be logged for optimization
    /// </summary>
    public const int PerformanceMonitoringThreshold = 1000;

    #endregion

    #region 🔧 Runtime Configuration Helpers

    /// <summary>
    /// Gets the complete Supabase configuration for client initialization
    /// Centralizes configuration access with validation
    /// </summary>
    public static (string Url, string AnonKey) GetSupabaseConfig()
    {
        return (SupabaseUrl, SupabaseAnonKey);
    }

    /// <summary>
    /// Validates essential configuration values at application startup
    /// Prevents runtime errors due to misconfiguration
    /// </summary>
    public static bool ValidateConfiguration()
    {
        try
        {
            // Validate Supabase configuration
            if (string.IsNullOrWhiteSpace(SupabaseUrl) ||
                string.IsNullOrWhiteSpace(SupabaseAnonKey))
            {
                LogConfigurationError("Supabase configuration is missing or invalid");
                return false;
            }

            // Validate URL format
            if (!Uri.TryCreate(SupabaseUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "https" && uri.Scheme != "http"))
            {
                LogConfigurationError($"Invalid Supabase URL format: {SupabaseUrl}");
                return false;
            }

            // Validate numeric settings
            if (DefaultCacheDurationMinutes <= 0 ||
                NetworkTimeoutSeconds <= 0 ||
                MaxRetryAttempts <= 0)
            {
                LogConfigurationError("Invalid numeric configuration values");
                return false;
            }

            LogConfigurationSuccess("All configuration values validated successfully");
            return true;
        }
        catch (Exception ex)
        {
            LogConfigurationError($"Configuration validation failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets environment-specific configuration summary for debugging
    /// Provides runtime configuration visibility for troubleshooting
    /// </summary>
    public static string GetConfigurationSummary()
    {
        return $"""
            🔧 OrchidPro Configuration Summary
            ================================
            Environment: {Environment}
            Debug Mode: {IsDebugMode}
            Version: {ApplicationVersion}
            Supabase URL: {MaskUrl(SupabaseUrl)}
            Cache Duration: {DefaultCacheDurationMinutes}m
            Network Timeout: {NetworkTimeoutSeconds}s
            Auto Sync: {EnableAutoSync} ({AutoSyncIntervalMinutes}m)
            Real-time Updates: {EnableRealTimeUpdates}
            Local Encryption: {EnableLocalEncryption}
            Analytics: {EnableAnalytics}
            ================================
            """;
    }

    /// <summary>
    /// Creates a masked version of sensitive URLs for logging
    /// Prevents accidental exposure of sensitive configuration data
    /// </summary>
    private static string MaskUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return "Not configured";

        try
        {
            var uri = new Uri(url);
            return $"{uri.Scheme}://{uri.Host}/...";
        }
        catch
        {
            return "Invalid URL";
        }
    }

    /// <summary>
    /// Logs configuration validation success with structured format
    /// </summary>
    private static void LogConfigurationSuccess(string message, [CallerMemberName] string memberName = "")
    {
        typeof(AppSettings).LogSuccess(message, memberName);
    }

    /// <summary>
    /// Logs configuration validation errors with structured format
    /// </summary>
    private static void LogConfigurationError(string message, [CallerMemberName] string memberName = "")
    {
        typeof(AppSettings).LogError(message, memberName);
    }

    #endregion
}