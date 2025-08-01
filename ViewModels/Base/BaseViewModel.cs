using CommunityToolkit.Mvvm.ComponentModel;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels;

/// <summary>
/// Base ViewModel providing common functionality for all ViewModels in the application.
/// Implements MVVM pattern with observable properties and lifecycle management.
/// </summary>
public abstract partial class BaseViewModel : ObservableObject
{
    #region Observable Properties

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string subtitle = string.Empty;

    [ObservableProperty]
    private bool isInitialized;

    #endregion

    #region Lifecycle Methods

    /// <summary>
    /// Called when the view appears. Handles initialization on first appearance.
    /// </summary>
    public virtual async Task OnAppearingAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (!IsInitialized)
            {
                this.LogInfo("Initializing ViewModel for first appearance");
                await InitializeAsync();
                IsInitialized = true;
                this.LogSuccess("ViewModel initialization completed successfully");
            }
            else
            {
                this.LogDebug("ViewModel already initialized, skipping initialization");
            }
        }, "ViewModel Appearing");
    }

    /// <summary>
    /// Called when the view disappears. Override in derived classes for cleanup.
    /// </summary>
    public virtual Task OnDisappearingAsync()
    {
        this.LogDebug("ViewModel disappearing");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Initialize the ViewModel. Override in derived classes for specific initialization logic.
    /// </summary>
    protected virtual Task InitializeAsync()
    {
        this.LogDebug("Base ViewModel initialization (override in derived classes)");
        return Task.CompletedTask;
    }

    #endregion

    #region User Interface Methods

    /// <summary>
    /// Shows an error message to the user using platform-specific alert dialogs.
    /// </summary>
    /// <param name="title">The title of the error dialog</param>
    /// <param name="message">The error message to display</param>
    protected virtual async Task ShowErrorAsync(string title, string message = "")
    {
        await this.SafeExecuteAsync(async () =>
        {
            var mainPage = GetCurrentPage();
            if (mainPage != null)
            {
                await mainPage.DisplayAlert(title, message, "OK");
            }
        }, "Show Error Dialog");
    }

    /// <summary>
    /// Shows a success message to the user using platform-specific alert dialogs.
    /// </summary>
    /// <param name="message">The success message to display</param>
    protected virtual async Task ShowSuccessAsync(string message)
    {
        await this.SafeExecuteAsync(async () =>
        {
            var mainPage = GetCurrentPage();
            if (mainPage != null)
            {
                await mainPage.DisplayAlert("Success", message, "OK");
            }
        }, "Show Success Dialog");
    }

    /// <summary>
    /// Shows a confirmation dialog to the user with Yes/No options.
    /// </summary>
    /// <param name="title">The title of the confirmation dialog</param>
    /// <param name="message">The confirmation message</param>
    /// <returns>True if user confirmed, false otherwise</returns>
    public virtual async Task<bool> ShowConfirmAsync(string title, string message)
    {
        return await this.SafeExecuteAsync(async () =>
        {
            var mainPage = GetCurrentPage();
            if (mainPage != null)
            {
                return await mainPage.DisplayAlert(title, message, "Yes", "No");
            }
            return false;
        }, fallbackValue: false, "Show Confirmation Dialog");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the current active page from the application window hierarchy.
    /// </summary>
    /// <returns>The current page or null if not available</returns>
    private static Page? GetCurrentPage()
    {
        return new object().SafeExecute(() =>
        {
            return Application.Current?.Windows?.FirstOrDefault()?.Page;
        }, fallbackValue: null, "Get Current Page");
    }

    /// <summary>
    /// Executes an action on the main UI thread safely.
    /// </summary>
    /// <param name="action">The action to execute on the main thread</param>
    protected void DispatchOnMainThread(Action action)
    {
        this.SafeExecute(() =>
        {
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Dispatch(action);
            }
        }, "Dispatch On Main Thread");
    }

    /// <summary>
    /// Executes an async action on the main UI thread safely.
    /// </summary>
    /// <param name="action">The async action to execute on the main thread</param>
    protected async Task DispatchOnMainThreadAsync(Func<Task> action)
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (Application.Current != null)
            {
                await Application.Current.Dispatcher.DispatchAsync(action);
            }
        }, "Dispatch Async On Main Thread");
    }

    #endregion
}