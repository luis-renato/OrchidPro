using System.Diagnostics;
using System.Runtime.CompilerServices;
using OrchidPro.Constants;

namespace OrchidPro.Extensions;

/// <summary>
/// Extension methods for standardized logging throughout OrchidPro
/// Replaces scattered Debug.WriteLine calls with consistent logging
/// </summary>
public static class LoggingExtensions
{
    #region Logging Methods

    /// <summary>
    /// Logs success message with standardized format
    /// </summary>
    public static void LogSuccess(this object source, string message, [CallerMemberName] string memberName = "")
    {
        var category = GetCategoryFromSource(source);
        var className = source.GetType().Name;
        Debug.WriteLine(LoggingConstants.LOG_FORMAT_SUCCESS, $"{category}:{className}:{memberName}", message);
    }

    /// <summary>
    /// Logs error message with standardized format
    /// </summary>
    public static void LogError(this object source, string message, [CallerMemberName] string memberName = "")
    {
        var category = GetCategoryFromSource(source);
        var className = source.GetType().Name;
        Debug.WriteLine(LoggingConstants.LOG_FORMAT_ERROR, $"{category}:{className}:{memberName}", message);
    }

    /// <summary>
    /// Logs error with exception details
    /// </summary>
    public static void LogError(this object source, Exception ex, string? additionalMessage = null, [CallerMemberName] string memberName = "")
    {
        var category = GetCategoryFromSource(source);
        var className = source.GetType().Name;
        var message = additionalMessage != null
            ? $"{additionalMessage}: {ex.Message}"
            : ex.Message;
        Debug.WriteLine(LoggingConstants.LOG_FORMAT_ERROR, $"{category}:{className}:{memberName}", message);

        // Log stack trace only in Debug builds
#if DEBUG
        if (ex.StackTrace != null)
        {
            Debug.WriteLine($"🔍 [{category}:{className}:{memberName}] Stack: {ex.StackTrace}");
        }
#endif
    }

    /// <summary>
    /// Logs informational message with standardized format
    /// </summary>
    public static void LogInfo(this object source, string message, [CallerMemberName] string memberName = "")
    {
        var category = GetCategoryFromSource(source);
        var className = source.GetType().Name;
        Debug.WriteLine(LoggingConstants.LOG_FORMAT_INFO, $"{category}:{className}:{memberName}", message);
    }

    /// <summary>
    /// Logs warning message with standardized format
    /// </summary>
    public static void LogWarning(this object source, string message, [CallerMemberName] string memberName = "")
    {
        var category = GetCategoryFromSource(source);
        var className = source.GetType().Name;
        Debug.WriteLine(LoggingConstants.LOG_FORMAT_WARNING, $"{category}:{className}:{memberName}", message);
    }

    /// <summary>
    /// Logs debug message with standardized format (Debug builds only)
    /// </summary>
    public static void LogDebug(this object source, string message, [CallerMemberName] string memberName = "")
    {
#if DEBUG
        var category = GetCategoryFromSource(source);
        var className = source.GetType().Name;
        Debug.WriteLine(LoggingConstants.LOG_FORMAT_DEBUG, $"{category}:{className}:{memberName}", message);
#endif
    }

    #endregion

    #region Specialized Logging

    /// <summary>
    /// Logs the start of an operation
    /// </summary>
    public static void LogOperationStart(this object source, string operationName, [CallerMemberName] string memberName = "")
    {
        source.LogInfo($"Starting {operationName}", memberName);
    }

    /// <summary>
    /// Logs successful completion of an operation with optional timing
    /// </summary>
    public static void LogOperationSuccess(this object source, string operationName, TimeSpan? duration = null, [CallerMemberName] string memberName = "")
    {
        var message = duration.HasValue
            ? $"{operationName} completed successfully in {duration.Value.TotalMilliseconds:F1}ms"
            : $"{operationName} completed successfully";
        source.LogSuccess(message, memberName);
    }

    /// <summary>
    /// Logs operation failure with exception details
    /// </summary>
    public static void LogOperationFailure(this object source, string operationName, Exception ex, [CallerMemberName] string memberName = "")
    {
        source.LogError(ex, $"{operationName} failed", memberName);
    }

    /// <summary>
    /// Logs method entry with optional parameters
    /// </summary>
    public static void LogMethodEntry(this object source, object? parameters = null, [CallerMemberName] string memberName = "")
    {
        var message = parameters != null
            ? $"Entering with parameters: {parameters}"
            : "Entering";
        source.LogDebug(message, memberName);
    }

    /// <summary>
    /// Logs method exit with optional result
    /// </summary>
    public static void LogMethodExit(this object source, object? result = null, [CallerMemberName] string memberName = "")
    {
        var message = result != null
            ? $"Exiting with result: {result}"
            : "Exiting";
        source.LogDebug(message, memberName);
    }

    /// <summary>
    /// Logs navigation operations
    /// </summary>
    public static void LogNavigation(this object source, string destination, string? additionalInfo = null, [CallerMemberName] string memberName = "")
    {
        var message = additionalInfo != null
            ? $"Navigating to {destination} - {additionalInfo}"
            : $"Navigating to {destination}";
        source.LogInfo(message, memberName);
    }

    /// <summary>
    /// Logs data operations (CRUD operations)
    /// </summary>
    public static void LogDataOperation(this object source, string operation, string entityType, object? identifier = null, [CallerMemberName] string memberName = "")
    {
        var message = identifier != null
            ? $"{operation} {entityType} (ID: {identifier})"
            : $"{operation} {entityType}";
        source.LogInfo(message, memberName);
    }

    /// <summary>
    /// Logs animation operations
    /// </summary>
    public static void LogAnimation(this object source, string animationType, string target, TimeSpan duration, [CallerMemberName] string memberName = "")
    {
        var message = $"{animationType} animation on {target} ({duration.TotalMilliseconds:F0}ms)";
        source.LogDebug(message, memberName);
    }

    /// <summary>
    /// Logs connectivity status changes
    /// </summary>
    public static void LogConnectivity(this object source, string status, string? additionalInfo = null, [CallerMemberName] string memberName = "")
    {
        var message = additionalInfo != null
            ? $"Connection {status} - {additionalInfo}"
            : $"Connection {status}";
        source.LogInfo(message, memberName);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Determines logging category based on source type
    /// </summary>
    private static string GetCategoryFromSource(object source)
    {
        var typeName = source.GetType().Name;

        // ViewModels
        if (typeName.EndsWith("ViewModel"))
            return LoggingConstants.CATEGORY_UI;

        // Pages
        if (typeName.EndsWith("Page"))
            return LoggingConstants.CATEGORY_UI;

        // Services
        if (typeName.EndsWith("Service"))
            return LoggingConstants.CATEGORY_DATA;

        // Repositories
        if (typeName.EndsWith("Repository"))
            return LoggingConstants.CATEGORY_DATA;

        // Navigation
        if (typeName.Contains("Navigation"))
            return LoggingConstants.CATEGORY_NAVIGATION;

        // Animation related
        if (typeName.Contains("Animation") || typeName.Contains("Transition"))
            return LoggingConstants.CATEGORY_ANIMATION;

        // Validation related
        if (typeName.Contains("Validation") || typeName.Contains("Validator"))
            return LoggingConstants.CATEGORY_VALIDATION;

        // Default
        return "GENERAL";
    }

    #endregion

    #region Performance Logging

    /// <summary>
    /// Logs performance of an operation with automatic timing - returns struct-based disposable
    /// Usage: using (source.LogPerformance("OperationName")) { ... }
    /// </summary>
    public static PerformanceScope LogPerformance(this object source, string operationName, [CallerMemberName] string memberName = "")
    {
        source.LogOperationStart(operationName, memberName);
        return new PerformanceScope(source, operationName, memberName);
    }

    #endregion
}

/// <summary>
/// Struct-based performance scope to avoid WinRT class issues - structs don't trigger WinRT warnings
/// </summary>
public readonly struct PerformanceScope : IDisposable
{
    private readonly object _source;
    private readonly string _operationName;
    private readonly string _memberName;
    private readonly Stopwatch _stopwatch;

    internal PerformanceScope(object source, string operationName, string memberName)
    {
        _source = source;
        _operationName = operationName;
        _memberName = memberName;
        _stopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Stops timing and logs performance result
    /// </summary>
    public void Dispose()
    {
        _stopwatch?.Stop();
        _source?.LogOperationSuccess(_operationName, _stopwatch?.Elapsed ?? TimeSpan.Zero, _memberName);
    }
}