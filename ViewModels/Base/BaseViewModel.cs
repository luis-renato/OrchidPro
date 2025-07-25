using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;

namespace OrchidPro.ViewModels;

/// <summary>
/// Base ViewModel with common functionality for all ViewModels
/// ✅ CORRIGIDO: Device e Application.MainPage obsoletos
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
    /// ✅ CORRIGIDO: Shows an error message to the user
    /// </summary>
    protected virtual async Task ShowErrorAsync(string title, string message = "")
    {
        try
        {
            // ✅ CORRIGIDO: Usar Windows[0].Page ao invés de MainPage obsoleto
            var mainPage = GetCurrentPage();
            if (mainPage != null)
            {
                await mainPage.DisplayAlert(title, message, "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error showing alert: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ CORRIGIDO: Shows a success message to the user
    /// </summary>
    protected virtual async Task ShowSuccessAsync(string message)
    {
        try
        {
            // ✅ CORRIGIDO: Usar Windows[0].Page ao invés de MainPage obsoleto
            var mainPage = GetCurrentPage();
            if (mainPage != null)
            {
                await mainPage.DisplayAlert("Success", message, "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error showing success alert: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ CORRIGIDO: Shows a confirmation dialog
    /// </summary>
    public virtual async Task<bool> ShowConfirmAsync(string title, string message)
    {
        try
        {
            // ✅ CORRIGIDO: Usar Windows[0].Page ao invés de MainPage obsoleto
            var mainPage = GetCurrentPage();
            if (mainPage != null)
            {
                return await mainPage.DisplayAlert(title, message, "Yes", "No");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error showing confirmation: {ex.Message}");
        }
        return false;
    }

    /// <summary>
    /// ✅ NOVO: Helper method para obter a página atual sem usar MainPage obsoleto
    /// </summary>
    private static Page? GetCurrentPage()
    {
        try
        {
            // ✅ CORRIGIDO: Usar Windows[0].Page ao invés de MainPage obsoleto
            return Application.Current?.Windows?.FirstOrDefault()?.Page;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// ✅ NOVO: Helper method para executar ações na UI thread
    /// </summary>
    protected void DispatchOnMainThread(Action action)
    {
        try
        {
            // ✅ CORRIGIDO: Usar Dispatcher.Dispatch ao invés de Device.BeginInvokeOnMainThread obsoleto
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Dispatch(action);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error dispatching to main thread: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ NOVO: Helper method async para executar ações na UI thread
    /// </summary>
    protected async Task DispatchOnMainThreadAsync(Func<Task> action)
    {
        try
        {
            // ✅ CORRIGIDO: Usar Dispatcher.DispatchAsync ao invés de Device.BeginInvokeOnMainThread obsoleto
            if (Application.Current != null)
            {
                await Application.Current.Dispatcher.DispatchAsync(action);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error dispatching async to main thread: {ex.Message}");
        }
    }
}