using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using OrchidPro.Constants;
using OrchidPro.ViewModels;
using System.Diagnostics;

namespace OrchidPro.Extensions;

/// <summary>
/// ✅ PADRONIZAÇÃO: Extensions para toasts e dialogs consistentes
/// Centraliza todas as mensagens de feedback do OrchidPro
/// </summary>
public static class ToastExtensions
{
    #region 🍞 Toast Messages Padronizados

    /// <summary>
    /// ✅ Toast de sucesso padronizado com ícone e duração consistente
    /// </summary>
    public static async Task ShowSuccessToast(this Page page, string message)
    {
        try
        {
            var toast = Toast.Make(
                $"{NotificationConstants.ICON_SUCCESS} {message}",
                ToastDuration.Short,
                NotificationConstants.TOAST_FONT_SIZE
            );
            await toast.Show();
            Debug.WriteLine($"{NotificationConstants.ICON_SUCCESS} [TOAST] Success: {message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [TOAST] Success toast failed: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ Toast de erro padronizado com ícone e duração estendida
    /// </summary>
    public static async Task ShowErrorToast(this Page page, string message)
    {
        try
        {
            var toast = Toast.Make(
                $"{NotificationConstants.ICON_ERROR} {message}",
                ToastDuration.Long,
                NotificationConstants.TOAST_FONT_SIZE
            );
            await toast.Show();
            Debug.WriteLine($"{NotificationConstants.ICON_ERROR} [TOAST] Error: {message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [TOAST] Error toast failed: {ex.Message}");
            // Fallback para DisplayAlert se toast falhar
            try
            {
                await page.DisplayAlert("Error", message, "OK");
            }
            catch
            {
                // Se tudo falhar, só fazer log
                Debug.WriteLine($"❌ [TOAST] Fallback alert also failed for: {message}");
            }
        }
    }

    /// <summary>
    /// ✅ Toast de informação padronizado
    /// </summary>
    public static async Task ShowInfoToast(this Page page, string message)
    {
        try
        {
            var toast = Toast.Make(
                $"{NotificationConstants.ICON_INFO} {message}",
                ToastDuration.Short,
                NotificationConstants.TOAST_FONT_SIZE
            );
            await toast.Show();
            Debug.WriteLine($"{NotificationConstants.ICON_INFO} [TOAST] Info: {message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [TOAST] Info toast failed: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ Toast de aviso padronizado
    /// </summary>
    public static async Task ShowWarningToast(this Page page, string message)
    {
        try
        {
            var toast = Toast.Make(
                $"{NotificationConstants.ICON_WARNING} {message}",
                ToastDuration.Short,
                NotificationConstants.TOAST_FONT_SIZE
            );
            await toast.Show();
            Debug.WriteLine($"{NotificationConstants.ICON_WARNING} [TOAST] Warning: {message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [TOAST] Warning toast failed: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ Toast customizado para ações específicas
    /// </summary>
    public static async Task ShowActionToast(this Page page, string action, string itemName, bool success = true)
    {
        var icon = success ? NotificationConstants.ICON_SUCCESS : NotificationConstants.ICON_ERROR;
        var status = success ? "completed" : "failed";
        var message = $"{action} {status}: {itemName}";

        if (success)
        {
            await page.ShowSuccessToast(message);
        }
        else
        {
            await page.ShowErrorToast(message);
        }
    }

    #endregion

    #region 🗨️ Dialog Padronizados

    /// <summary>
    /// ✅ Dialog de confirmação de delete padronizado
    /// </summary>
    public static async Task<bool> ShowDeleteConfirmation(this Page page, string itemName, string itemType = "item")
    {
        try
        {
            return await page.DisplayAlert(
                $"Delete {itemType}",
                $"Are you sure you want to delete '{itemName}'?\n\nThis action cannot be undone.",
                TextConstants.DELETE_ITEM,
                TextConstants.CANCEL_CHANGES
            );
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [DIALOG] Delete confirmation failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// ✅ Dialog de confirmação de descarte de mudanças
    /// </summary>
    public static async Task<bool> ShowDiscardChangesConfirmation(this Page page)
    {
        try
        {
            return await page.DisplayAlert(
                "Discard Changes?",
                "You have unsaved changes. Are you sure you want to discard them?",
                "Discard",
                "Keep Editing"
            );
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [DIALOG] Discard confirmation failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// ✅ Dialog de confirmação genérico
    /// </summary>
    public static async Task<bool> ShowConfirmation(this Page page, string title, string message, string acceptText = "OK", string cancelText = "Cancel")
    {
        try
        {
            return await page.DisplayAlert(title, message, acceptText, cancelText);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [DIALOG] Confirmation failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// ✅ Dialog de informação simples
    /// </summary>
    public static async Task ShowInformation(this Page page, string title, string message)
    {
        try
        {
            await page.DisplayAlert(title, message, "OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [DIALOG] Information dialog failed: {ex.Message}");
        }
    }

    #endregion

    #region 🎯 Extensions para ViewModels

    /// <summary>
    /// ✅ Extension para ViewModels mostrarem toasts
    /// Busca a página atual automaticamente
    /// </summary>
    public static async Task ShowSuccessToast(this BaseViewModel viewModel, string message)
    {
        var page = GetCurrentPage();
        if (page != null)
        {
            await page.ShowSuccessToast(message);
        }
        else
        {
            Debug.WriteLine($"✅ [VM_TOAST] {message} (page not available)");
        }
    }

    /// <summary>
    /// ✅ Extension para ViewModels mostrarem erros
    /// </summary>
    public static async Task ShowErrorToast(this BaseViewModel viewModel, string message)
    {
        var page = GetCurrentPage();
        if (page != null)
        {
            await page.ShowErrorToast(message);
        }
        else
        {
            Debug.WriteLine($"❌ [VM_TOAST] {message} (page not available)");
        }
    }

    /// <summary>
    /// ✅ Extension para ViewModels mostrarem confirmação
    /// </summary>
    public static async Task<bool> ShowConfirmation(this BaseViewModel viewModel, string title, string message, string acceptText = "OK", string cancelText = "Cancel")
    {
        var page = GetCurrentPage();
        if (page != null)
        {
            return await page.ShowConfirmation(title, message, acceptText, cancelText);
        }
        else
        {
            Debug.WriteLine($"❌ [VM_DIALOG] Confirmation not available: {title}");
            return false;
        }
    }

    #endregion

    #region 🔧 Helper Methods

    /// <summary>
    /// ✅ Busca a página atual da aplicação
    /// </summary>
    private static Page? GetCurrentPage()
    {
        try
        {
            return Application.Current?.MainPage;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [TOAST] Cannot get current page: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// ✅ Verifica se estamos na UI thread
    /// </summary>
    private static bool IsOnMainThread()
    {
        try
        {
            return Application.Current?.Dispatcher.IsDispatchRequired == false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// ✅ Executa na UI thread se necessário
    /// </summary>
    public static async Task ExecuteOnMainThread(Func<Task> action)
    {
        try
        {
            if (IsOnMainThread())
            {
                await action();
            }
            else
            {
                await Application.Current!.Dispatcher.DispatchAsync(action);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [TOAST] Main thread execution failed: {ex.Message}");
        }
    }

    #endregion

    #region 🎨 Feedback Visual Extensions

    /// <summary>
    /// ✅ Feedback completo para operações CRUD
    /// </summary>
    public static async Task ShowCrudFeedback(this Page page, string operation, string entityName, bool success, Exception? exception = null)
    {
        if (success)
        {
            await page.ShowSuccessToast($"{operation} completed: {entityName}");
        }
        else
        {
            var errorMsg = exception?.Message ?? "Operation failed";
            await page.ShowErrorToast($"{operation} failed: {errorMsg}");
        }
    }

    /// <summary>
    /// ✅ Feedback para operações de sincronização
    /// </summary>
    public static async Task ShowSyncFeedback(this Page page, int itemCount, bool success)
    {
        if (success)
        {
            await page.ShowSuccessToast($"Synced {itemCount} items successfully");
        }
        else
        {
            await page.ShowErrorToast("Sync failed. Please try again.");
        }
    }

    /// <summary>
    /// ✅ Feedback para operações de filtro
    /// </summary>
    public static async Task ShowFilterFeedback(this Page page, string filterType, int resultCount)
    {
        await page.ShowInfoToast($"Filter applied: {filterType} ({resultCount} results)");
    }

    #endregion
}