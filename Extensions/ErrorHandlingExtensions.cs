using System.Net;
using OrchidPro.Constants;
using System.Runtime.CompilerServices;

namespace OrchidPro.Extensions;

/// <summary>
/// Extension methods for centralized error handling throughout OrchidPro
/// Replaces scattered try-catch blocks with consistent error handling
/// </summary>
public static class ErrorHandlingExtensions
{
    #region Safe Execution Methods

    /// <summary>
    /// Safe execution of synchronous action with automatic logging
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
    /// Safe execution of asynchronous action with automatic logging
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
    /// Safe execution with return value
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
    /// Safe asynchronous execution with return value
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

    #region Specialized Error Handling

    /// <summary>
    /// Safe execution for data operations with specific error handling
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
    /// Safe execution for animations with silent fallback
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
            // For animations, only log warning - don't break UX
            source.LogWarning($"Animation {animationType} failed: {ex.Message}", memberName);
        }
    }

    /// <summary>
    /// Safe execution for navigation with fallback
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

    #region Retry Mechanisms

    /// <summary>
    /// Execution with automatic retry for critical operations
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

        return default;
    }

    /// <summary>
    /// Retry specific for network operations
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

    #region Validation Helpers

    /// <summary>
    /// Safe validation with logging
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
    /// Safe asynchronous validation
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

    #region IDisposable Safe Disposal

    /// <summary>
    /// Safe disposal with logging
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

#region Result Classes

/// <summary>
/// Standard class for repository operation results
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
/// Standard error types
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