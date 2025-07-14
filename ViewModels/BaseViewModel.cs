using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;

namespace OrchidPro.ViewModels;

/// <summary>
/// Base ViewModel with common functionality for all ViewModels
/// </summary>
public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string subtitle = string.Empty;

    [ObservableProperty]
    private bool isInitialized;

    /// <summary>
    /// Called when the view appears
    /// </summary>
    public virtual async Task OnAppearingAsync()
    {
        if (!IsInitialized)
        {
            await InitializeAsync();
            IsInitialized = true;
        }
    }

    /// <summary>
    /// Called when the view disappears
    /// </summary>
    public virtual Task OnDisappearingAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Initialize the ViewModel
    /// </summary>
    protected virtual Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Shows an error message to the user
    /// </summary>
    protected virtual async Task ShowErrorAsync(string title, string message = "")
    {
        try
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(title, message, "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error showing alert: {ex.Message}");
        }
    }

    /// <summary>
    /// Shows a success message to the user
    /// </summary>
    protected virtual async Task ShowSuccessAsync(string message)
    {
        try
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Success", message, "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error showing success alert: {ex.Message}");
        }
    }

    /// <summary>
    /// Shows a confirmation dialog
    /// </summary>
    public virtual async Task<bool> ShowConfirmAsync(string title, string message)
    {
        try
        {
            if (Application.Current?.MainPage != null)
            {
                return await Application.Current.MainPage.DisplayAlert(title, message, "Yes", "No");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error showing confirm alert: {ex.Message}");
        }
        return false;
    }

    /// <summary>
    /// Executes an action with busy state management
    /// </summary>
    protected async Task ExecuteWithBusyAsync(Func<Task> action)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            await action();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in ExecuteWithBusyAsync: {ex.Message}");
            await ShowErrorAsync("Error", "An unexpected error occurred");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Executes a function with busy state management and returns result
    /// </summary>
    protected async Task<T?> ExecuteWithBusyAsync<T>(Func<Task<T>> func)
    {
        if (IsBusy) return default;

        try
        {
            IsBusy = true;
            return await func();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in ExecuteWithBusyAsync<T>: {ex.Message}");
            await ShowErrorAsync("Error", "An unexpected error occurred");
            return default;
        }
        finally
        {
            IsBusy = false;
        }
    }
}