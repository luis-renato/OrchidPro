using System.Diagnostics;
using System.Runtime.CompilerServices;
using OrchidPro.Constants;

namespace OrchidPro.Extensions;

/// <summary>
/// 📝 PADRONIZAÇÃO: Extensions para logging centralizado do OrchidPro
/// Substitui todos os Debug.WriteLine espalhados pelo código
/// </summary>
public static class LoggingExtensions
{
    #region 📝 Logging Methods

    /// <summary>
    /// ✅ Log de sucesso padronizado
    /// </summary>
    public static void LogSuccess(this object source, string message, [CallerMemberName] string memberName = "")
    {
        var category = GetCategoryFromSource(source);
        var className = source.GetType().Name;
        Debug.WriteLine(LoggingConstants.LOG_FORMAT_SUCCESS, $"{category}:{className}:{memberName}", message);
    }

    /// <summary>
    /// ❌ Log de erro padronizado
    /// </summary>
    public static void LogError(this object source, string message, [CallerMemberName] string memberName = "")
    {
        var category = GetCategoryFromSource(source);
        var className = source.GetType().Name;
        Debug.WriteLine(LoggingConstants.LOG_FORMAT_ERROR, $"{category}:{className}:{memberName}", message);
    }

    /// <summary>
    /// ❌ Log de erro com exception
    /// </summary>
    public static void LogError(this object source, Exception ex, string? additionalMessage = null, [CallerMemberName] string memberName = "")
    {
        var category = GetCategoryFromSource(source);
        var className = source.GetType().Name;
        var message = additionalMessage != null
            ? $"{additionalMessage}: {ex.Message}"
            : ex.Message;
        Debug.WriteLine(LoggingConstants.LOG_FORMAT_ERROR, $"{category}:{className}:{memberName}", message);

        // Log stack trace apenas em Debug
#if DEBUG
        if (ex.StackTrace != null)
        {
            Debug.WriteLine($"🔍 [{category}:{className}:{memberName}] Stack: {ex.StackTrace}");
        }
#endif
    }

    /// <summary>
    /// ℹ️ Log de informação padronizado
    /// </summary>
    public static void LogInfo(this object source, string message, [CallerMemberName] string memberName = "")
    {
        var category = GetCategoryFromSource(source);
        var className = source.GetType().Name;
        Debug.WriteLine(LoggingConstants.LOG_FORMAT_INFO, $"{category}:{className}:{memberName}", message);
    }

    /// <summary>
    /// ⚠️ Log de warning padronizado
    /// </summary>
    public static void LogWarning(this object source, string message, [CallerMemberName] string memberName = "")
    {
        var category = GetCategoryFromSource(source);
        var className = source.GetType().Name;
        Debug.WriteLine(LoggingConstants.LOG_FORMAT_WARNING, $"{category}:{className}:{memberName}", message);
    }

    /// <summary>
    /// 🔧 Log de debug padronizado
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

    #region 🎯 Specialized Logging

    /// <summary>
    /// 🚀 Log de início de operação
    /// </summary>
    public static void LogOperationStart(this object source, string operationName, [CallerMemberName] string memberName = "")
    {
        source.LogInfo($"Starting {operationName}", memberName);
    }

    /// <summary>
    /// ✅ Log de fim de operação com sucesso
    /// </summary>
    public static void LogOperationSuccess(this object source, string operationName, TimeSpan? duration = null, [CallerMemberName] string memberName = "")
    {
        var message = duration.HasValue
            ? $"{operationName} completed successfully in {duration.Value.TotalMilliseconds:F1}ms"
            : $"{operationName} completed successfully";
        source.LogSuccess(message, memberName);
    }

    /// <summary>
    /// ❌ Log de falha de operação
    /// </summary>
    public static void LogOperationFailure(this object source, string operationName, Exception ex, [CallerMemberName] string memberName = "")
    {
        source.LogError(ex, $"{operationName} failed", memberName);
    }

    /// <summary>
    /// 🔄 Log de entrada em método
    /// </summary>
    public static void LogMethodEntry(this object source, object? parameters = null, [CallerMemberName] string memberName = "")
    {
        var message = parameters != null
            ? $"Entering with parameters: {parameters}"
            : "Entering";
        source.LogDebug(message, memberName);
    }

    /// <summary>
    /// 🏁 Log de saída de método
    /// </summary>
    public static void LogMethodExit(this object source, object? result = null, [CallerMemberName] string memberName = "")
    {
        var message = result != null
            ? $"Exiting with result: {result}"
            : "Exiting";
        source.LogDebug(message, memberName);
    }

    /// <summary>
    /// 🔗 Log de navegação
    /// </summary>
    public static void LogNavigation(this object source, string destination, string? additionalInfo = null, [CallerMemberName] string memberName = "")
    {
        var message = additionalInfo != null
            ? $"Navigating to {destination} - {additionalInfo}"
            : $"Navigating to {destination}";
        source.LogInfo(message, memberName);
    }

    /// <summary>
    /// 💾 Log de operações de dados
    /// </summary>
    public static void LogDataOperation(this object source, string operation, string entityType, object? identifier = null, [CallerMemberName] string memberName = "")
    {
        var message = identifier != null
            ? $"{operation} {entityType} (ID: {identifier})"
            : $"{operation} {entityType}";
        source.LogInfo(message, memberName);
    }

    /// <summary>
    /// 🎨 Log de animações
    /// </summary>
    public static void LogAnimation(this object source, string animationType, string target, TimeSpan duration, [CallerMemberName] string memberName = "")
    {
        var message = $"{animationType} animation on {target} ({duration.TotalMilliseconds:F0}ms)";
        source.LogDebug(message, memberName);
    }

    /// <summary>
    /// 🔌 Log de conectividade
    /// </summary>
    public static void LogConnectivity(this object source, string status, string? additionalInfo = null, [CallerMemberName] string memberName = "")
    {
        var message = additionalInfo != null
            ? $"Connection {status} - {additionalInfo}"
            : $"Connection {status}";
        source.LogInfo(message, memberName);
    }

    #endregion

    #region 🔧 Helper Methods

    /// <summary>
    /// ✅ Determina categoria baseada no tipo do source
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

    #region 🎯 Performance Logging

    /// <summary>
    /// ⚡ Log de performance com timing
    /// </summary>
    public static IDisposable LogPerformance(this object source, string operationName, [CallerMemberName] string memberName = "")
    {
        return new PerformanceLogger(source, operationName, memberName);
    }

    /// <summary>
    /// 📊 Helper class para medir performance automaticamente
    /// </summary>
    private class PerformanceLogger : IDisposable
    {
        private readonly object _source;
        private readonly string _operationName;
        private readonly string _memberName;
        private readonly Stopwatch _stopwatch;

        public PerformanceLogger(object source, string operationName, string memberName)
        {
            _source = source;
            _operationName = operationName;
            _memberName = memberName;
            _stopwatch = Stopwatch.StartNew();

            _source.LogOperationStart(_operationName, _memberName);
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _source.LogOperationSuccess(_operationName, _stopwatch.Elapsed, _memberName);
        }
    }

    #endregion
}