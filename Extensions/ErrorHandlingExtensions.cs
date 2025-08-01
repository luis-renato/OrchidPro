using System.Net;
using OrchidPro.Constants;
using System.Runtime.CompilerServices;

namespace OrchidPro.Extensions;

/// <summary>
/// 🛡️ PADRONIZAÇÃO: Extensions para tratamento centralizado de erros do OrchidPro
/// Substitui todos os try/catch espalhados pelo código
/// </summary>
public static class ErrorHandlingExtensions
{
    #region 🛡️ Safe Execution Methods

    /// <summary>
    /// ✅ Execução segura de ação síncrona com logging automático
    /// </summary>
    public static bool SafeExecute(this object source, Action action, string? operationName = null, [CallerMemberName] string memberName = "")
    {
        var operation = operationName ?? memberName;

        try
        {
            source.LogOperationStart(operation, memberName);
            action();
            source.LogOperationSuccess(operation, memberName: memberName);
            return true;
        }
        catch (Exception ex)
        {
            source.LogOperationFailure(operation, ex, memberName);
            return false;
        }
    }

    /// <summary>
    /// ✅ Execução segura de ação assíncrona com logging automático
    /// </summary>
    public static async Task<bool> SafeExecuteAsync(this object source, Func<Task> action, string? operationName = null, [CallerMemberName] string memberName = "")
    {
        var operation = operationName ?? memberName;

        try
        {
            source.LogOperationStart(operation, memberName);
            await action();
            source.LogOperationSuccess(operation, memberName: memberName);
            return true;
        }
        catch (Exception ex)
        {
            source.LogOperationFailure(operation, ex, memberName);
            return false;
        }
    }

    /// <summary>
    /// ✅ Execução segura com retorno de valor
    /// </summary>
    public static T? SafeExecute<T>(this object source, Func<T> func, T? fallbackValue = default, string? operationName = null, [CallerMemberName] string memberName = "")
    {
        var operation = operationName ?? memberName;

        try
        {
            source.LogOperationStart(operation, memberName);
            var result = func();
            source.LogOperationSuccess(operation, memberName: memberName);
            return result;
        }
        catch (Exception ex)
        {
            source.LogOperationFailure(operation, ex, memberName);
            return fallbackValue;
        }
    }

    /// <summary>
    /// ✅ Execução segura assíncrona com retorno de valor
    /// </summary>
    public static async Task<T?> SafeExecuteAsync<T>(this object source, Func<Task<T>> func, T? fallbackValue = default, string? operationName = null, [CallerMemberName] string memberName = "")
    {
        var operation = operationName ?? memberName;

        try
        {
            source.LogOperationStart(operation, memberName);
            var result = await func();
            source.LogOperationSuccess(operation, memberName: memberName);
            return result;
        }
        catch (Exception ex)
        {
            source.LogOperationFailure(operation, ex, memberName);
            return fallbackValue;
        }
    }

    #endregion

    #region 🎯 Specialized Error Handling

    /// <summary>
    /// 💾 Execução segura para operações de dados com tratamento específico
    /// </summary>
    public static async Task<RepositoryResult<T>> SafeDataExecuteAsync<T>(this object source, Func<Task<T>> dataOperation, string entityType, [CallerMemberName] string memberName = "")
    {
        try
        {
            source.LogDataOperation("Starting", entityType, memberName: memberName);
            var result = await dataOperation();
            source.LogDataOperation("Completed", entityType, memberName: memberName);

            return new RepositoryResult<T>
            {
                Success = true,
                Data = result,
                Message = $"{entityType} operation completed successfully"
            };
        }
        catch (HttpRequestException httpEx)
        {
            source.LogError(httpEx, $"Network error in {entityType} operation", memberName);
            return new RepositoryResult<T>
            {
                Success = false,
                Message = "Network connection error. Please check your internet connection.",
                ErrorType = ErrorType.Network
            };
        }
        catch (TimeoutException timeoutEx)
        {
            source.LogError(timeoutEx, $"Timeout in {entityType} operation", memberName);
            return new RepositoryResult<T>
            {
                Success = false,
                Message = "Operation timed out. Please try again.",
                ErrorType = ErrorType.Timeout
            };
        }
        catch (UnauthorizedAccessException authEx)
        {
            source.LogError(authEx, $"Authorization error in {entityType} operation", memberName);
            return new RepositoryResult<T>
            {
                Success = false,
                Message = "You are not authorized to perform this operation.",
                ErrorType = ErrorType.Authorization
            };
        }
        catch (ArgumentException argEx)
        {
            source.LogError(argEx, $"Validation error in {entityType} operation", memberName);
            return new RepositoryResult<T>
            {
                Success = false,
                Message = argEx.Message,
                ErrorType = ErrorType.Validation
            };
        }
        catch (Exception ex)
        {
            source.LogError(ex, $"Unexpected error in {entityType} operation", memberName);
            return new RepositoryResult<T>
            {
                Success = false,
                Message = "An unexpected error occurred. Please try again later.",
                ErrorType = ErrorType.Unknown
            };
        }
    }

    /// <summary>
    /// 🎨 Execução segura para animações com fallback silencioso
    /// </summary>
    public static async Task SafeAnimationExecuteAsync(this object source, Func<Task> animationAction, string animationType, [CallerMemberName] string memberName = "")
    {
        try
        {
            source.LogAnimation(animationType, "starting", TimeSpan.Zero, memberName);
            await animationAction();
            source.LogAnimation(animationType, "completed", TimeSpan.Zero, memberName);
        }
        catch (Exception ex)
        {
            // Para animações, log de warning apenas - não quebrar UX
            source.LogWarning($"Animation {animationType} failed: {ex.Message}", memberName);
        }
    }

    /// <summary>
    /// 🧭 Execução segura para navegação com fallback
    /// </summary>
    public static async Task<bool> SafeNavigationExecuteAsync(this object source, Func<Task> navigationAction, string destination, [CallerMemberName] string memberName = "")
    {
        try
        {
            source.LogNavigation(destination, "starting", memberName);
            await navigationAction();
            source.LogNavigation(destination, "completed", memberName);
            return true;
        }
        catch (Exception ex)
        {
            source.LogError(ex, $"Navigation to {destination} failed", memberName);
            return false;
        }
    }

    #endregion

    #region 🔄 Retry Mechanisms

    /// <summary>
    /// 🔄 Execução com retry automático para operações críticas
    /// </summary>
    public static async Task<T?> SafeExecuteWithRetryAsync<T>(
        this object source,
        Func<Task<T>> action,
        int maxRetries = 3,
        TimeSpan? delay = null,
        string? operationName = null,
        [CallerMemberName] string memberName = "")
    {
        var operation = operationName ?? memberName;
        var retryDelay = delay ?? TimeSpan.FromSeconds(1);
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (attempt == 1)
                    source.LogOperationStart(operation, memberName);
                else
                    source.LogInfo($"Retry attempt {attempt}/{maxRetries} for {operation}", memberName);

                var result = await action();

                if (attempt > 1)
                    source.LogSuccess($"{operation} succeeded on attempt {attempt}", memberName);
                else
                    source.LogOperationSuccess(operation, memberName: memberName);

                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt < maxRetries)
                {
                    source.LogWarning($"{operation} failed on attempt {attempt}: {ex.Message}, retrying in {retryDelay.TotalSeconds}s", memberName);
                    await Task.Delay(retryDelay);
                }
                else
                {
                    source.LogOperationFailure(operation, ex, memberName);
                }
            }
        }

        return default(T);
    }

    /// <summary>
    /// 🔄 Retry específico para operações de rede
    /// </summary>
    public static async Task<T?> SafeNetworkExecuteAsync<T>(
        this object source,
        Func<Task<T>> networkAction,
        string? operationName = null,
        [CallerMemberName] string memberName = "")
    {
        return await source.SafeExecuteWithRetryAsync(
            networkAction,
            maxRetries: 3,
            delay: TimeSpan.FromSeconds(2),
            operationName: operationName ?? "Network operation",
            memberName: memberName
        );
    }

    #endregion

    #region 🔧 Validation Helpers

    /// <summary>
    /// ✅ Validação segura com logging
    /// </summary>
    public static bool SafeValidate(this object source, Func<bool> validation, string validationType, [CallerMemberName] string memberName = "")
    {
        try
        {
            source.LogDebug($"Starting {validationType} validation", memberName);
            var result = validation();

            if (result)
                source.LogDebug($"{validationType} validation passed", memberName);
            else
                source.LogWarning($"{validationType} validation failed", memberName);

            return result;
        }
        catch (Exception ex)
        {
            source.LogError(ex, $"{validationType} validation error", memberName);
            return false;
        }
    }

    /// <summary>
    /// ✅ Validação assíncrona segura
    /// </summary>
    public static async Task<bool> SafeValidateAsync(this object source, Func<Task<bool>> validation, string validationType, [CallerMemberName] string memberName = "")
    {
        try
        {
            source.LogDebug($"Starting async {validationType} validation", memberName);
            var result = await validation();

            if (result)
                source.LogDebug($"Async {validationType} validation passed", memberName);
            else
                source.LogWarning($"Async {validationType} validation failed", memberName);

            return result;
        }
        catch (Exception ex)
        {
            source.LogError(ex, $"Async {validationType} validation error", memberName);
            return false;
        }
    }

    #endregion

    #region 🔄 IDisposable Safe Disposal

    /// <summary>
    /// 🗑️ Dispose seguro com logging
    /// </summary>
    public static void SafeDispose(this object source, IDisposable? disposable, string? resourceName = null, [CallerMemberName] string memberName = "")
    {
        if (disposable == null) return;

        try
        {
            var name = resourceName ?? disposable.GetType().Name;
            source.LogDebug($"Disposing {name}", memberName);
            disposable.Dispose();
            source.LogDebug($"{name} disposed successfully", memberName);
        }
        catch (Exception ex)
        {
            source.LogWarning($"Error disposing resource: {ex.Message}", memberName);
        }
    }

    #endregion
}

#region 📊 Result Classes

/// <summary>
/// ✅ Classe padrão para resultados de operações de repositório
/// </summary>
public class RepositoryResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public ErrorType ErrorType { get; set; } = ErrorType.None;
    public Exception? Exception { get; set; }
}

/// <summary>
/// ✅ Tipos de erro padronizados
/// </summary>
public enum ErrorType
{
    None,
    Network,
    Timeout,
    Authorization,
    Validation,
    NotFound,
    Conflict,
    Unknown
}

#endregion