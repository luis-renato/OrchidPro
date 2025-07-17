using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models.Base;
using OrchidPro.Services.Navigation;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace OrchidPro.ViewModels.Base;

/// <summary>
/// BaseListViewModel genérico - Extrai toda funcionalidade comum de listas
/// ✅ NOVO: Suporte completo para SwipeView com ações Edit e Delete
/// </summary>
public abstract partial class BaseListViewModel<T, TItemViewModel> : BaseViewModel
    where T : class, IBaseEntity, new()
    where TItemViewModel : BaseItemViewModel<T>
{
    protected readonly IBaseRepository<T> _repository;
    protected readonly INavigationService _navigationService;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<TItemViewModel> items = new();

    [ObservableProperty]
    private ObservableCollection<TItemViewModel> selectedItems = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private bool isMultiSelectMode;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasData;

    [ObservableProperty]
    private string emptyStateMessage = "No items found";

    [ObservableProperty]
    private string statusFilter = "All";

    [ObservableProperty]
    private int totalCount;

    [ObservableProperty]
    private int activeCount;

    [ObservableProperty]
    private bool fabIsVisible = true;

    [ObservableProperty]
    private string fabText = "Add";

    [ObservableProperty]
    private string connectionStatus = "Connected";

    [ObservableProperty]
    private Color connectionStatusColor = Colors.Green;

    [ObservableProperty]
    private bool isConnected = true;

    #endregion

    #region Abstract Properties

    public abstract string EntityName { get; }
    public abstract string EntityNamePlural { get; }
    public abstract string EditRoute { get; }

    #endregion

    #region Filter Options

    public List<string> StatusFilterOptions { get; } = new() { "All", "Active", "Inactive" };

    #endregion

    #region Constructor

    protected BaseListViewModel(IBaseRepository<T> repository, INavigationService navigationService)
    {
        _repository = repository;
        _navigationService = navigationService;
        Title = EntityNamePlural;
        IsConnected = true;
        ConnectionStatus = "Connected";
        ConnectionStatusColor = Colors.Green;
        FabText = $"Add {EntityName}";
        Debug.WriteLine($"✅ [BASE_LIST_VM] Initialized for {EntityNamePlural}");
    }

    #endregion

    #region Abstract Methods

    protected abstract TItemViewModel CreateItemViewModel(T entity);

    #endregion

    #region Data Loading

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            Debug.WriteLine($"📥 [BASE_LIST_VM] Loading {EntityNamePlural} with filter: {StatusFilter}");

            await LoadItemsDataAsync();

            // Teste de conectividade em background
            _ = Task.Run(async () =>
            {
                await Task.Delay(500);
                await TestConnectionInBackgroundAsync();
            });

            Debug.WriteLine($"✅ [BASE_LIST_VM] Loaded {Items.Count} {EntityNamePlural}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Load error: {ex.Message}");
            await ShowErrorAsync($"Failed to load {EntityNamePlural}", "Check your connection and try again.");
            UpdateConnectionStatus(false);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadItemsDataAsync()
    {
        try
        {
            bool? statusFilter = StatusFilter switch
            {
                "Active" => true,
                "Inactive" => false,
                _ => null
            };

            var entities = await _repository.GetFilteredAsync(SearchText, statusFilter);

            var itemViewModels = entities.Select(entity =>
            {
                var itemVm = CreateItemViewModel(entity);
                itemVm.SelectionChangedAction = OnItemSelectionChanged;
                return itemVm;
            }).ToList();

            Items.Clear();
            foreach (var item in itemViewModels)
            {
                Items.Add(item);
            }

            TotalCount = entities.Count;
            ActiveCount = entities.Count(e => e.IsActive);
            HasData = entities.Any();

            Debug.WriteLine($"📊 [BASE_LIST_VM] Stats - Total: {TotalCount}, Active: {ActiveCount}, HasData: {HasData}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] LoadItemsDataAsync error: {ex.Message}");
            throw;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        try
        {
            await _repository.RefreshCacheAsync();
            await LoadItemsDataAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Refresh error: {ex.Message}");
            await ShowErrorAsync("Refresh Failed", "Failed to refresh data. Please try again.");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    #endregion

    #region Search and Filter

    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadItemsDataAsync();
    }

    [RelayCommand]
    private async Task ClearSearchAsync()
    {
        SearchText = string.Empty;
        await LoadItemsDataAsync();
    }

    [RelayCommand]
    private async Task FilterByStatusAsync()
    {
        await LoadItemsDataAsync();
    }

    #endregion

    #region Navigation Commands

    [RelayCommand]
    private async Task AddNewAsync()
    {
        if (!IsConnected)
        {
            var canProceed = await ShowConfirmAsync(
                "Offline Mode",
                "You're currently offline. Creating new items may not work properly. Continue anyway?");

            if (!canProceed) return;
        }

        try
        {
            Debug.WriteLine($"➕ [BASE_LIST_VM] Navigating to add new {EntityName}");
            await _navigationService.NavigateToAsync(EditRoute);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Add new navigation error: {ex.Message}");
            await ShowErrorAsync("Navigation Error", $"Failed to open add {EntityName.ToLower()} page");
        }
    }

    /// <summary>
    /// Alias para AddNewAsync para compatibilidade
    /// </summary>
    [RelayCommand]
    private async Task AddItemAsync()
    {
        await AddNewAsync();
    }

    [RelayCommand]
    private async Task NavigateToEditAsync(TItemViewModel item)
    {
        if (item == null) return;

        if (!IsConnected)
        {
            var canProceed = await ShowConfirmAsync(
                "Offline Mode",
                "You're currently offline. Editing may not work properly. Continue anyway?");

            if (!canProceed) return;
        }

        try
        {
            Debug.WriteLine($"📝 [BASE_LIST_VM] Navigating to edit {EntityName}: {item.Name}");

            var parameters = new Dictionary<string, object>
            {
                [$"{EntityName}Id"] = item.Id
            };

            await _navigationService.NavigateToAsync(EditRoute, parameters);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Edit navigation error: {ex.Message}");
            await ShowErrorAsync("Navigation Error", $"Failed to open edit {EntityName.ToLower()} page");
        }
    }

    [RelayCommand]
    private async Task DeleteSingleItemAsync(TItemViewModel item)
    {
        if (item == null) return;

        if (!IsConnected)
        {
            await ShowErrorAsync("No Connection", $"Cannot delete {EntityName.ToLower()} without internet connection.");
            return;
        }

        if (item.IsSystemDefault)
        {
            await ShowErrorAsync("Cannot Delete", $"This is a system default {EntityName.ToLower()} and cannot be deleted.");
            return;
        }

        try
        {
            Debug.WriteLine($"🗑️ [BASE_LIST_VM] Attempting to delete single {EntityName}: {item.Name}");

            var confirmed = await ShowConfirmAsync(
                $"Delete {EntityName}",
                $"Are you sure you want to delete '{item.Name}'?");

            if (!confirmed) return;

            IsLoading = true;

            var success = await _repository.DeleteAsync(item.Id);

            if (success)
            {
                await ShowSuccessAsync($"Successfully deleted {EntityName.ToLower()} '{item.Name}'");

                Debug.WriteLine($"🔄 [BASE_LIST_VM] === REFRESHING AFTER SINGLE DELETE ===");

                _repository.InvalidateCacheExternal();
                await _repository.RefreshCacheAsync();
                await LoadItemsDataAsync();

                Debug.WriteLine($"✅ [BASE_LIST_VM] === REFRESH COMPLETE ===");
            }
            else
            {
                await ShowErrorAsync("Delete Failed", $"Failed to delete {EntityName.ToLower()} '{item.Name}'");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Single delete error: {ex.Message}");

            if (ex.Message.Contains("connection") || ex.Message.Contains("internet"))
            {
                UpdateConnectionStatus(false);
                await ShowErrorAsync("Connection Error", $"Failed to delete {EntityName.ToLower()}. Check your internet connection.");
            }
            else
            {
                await ShowErrorAsync("Delete Error", $"Failed to delete {EntityName.ToLower()}. Please try again.");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ItemTappedAsync(TItemViewModel item)
    {
        if (item == null) return;

        Debug.WriteLine($"👆 [BASE_LIST_VM] Item tapped: {item.Name}, MultiSelect: {IsMultiSelectMode}");

        if (IsMultiSelectMode)
        {
            item.IsSelected = !item.IsSelected;
            Debug.WriteLine($"🔘 [BASE_LIST_VM] Toggled selection for {item.Name}: {item.IsSelected}");
        }
        else
        {
            await NavigateToEditAsync(item);
        }
    }

    /// <summary>
    /// 🎯 NOVO: Command para LongPress - Ativa modo seleção
    /// </summary>
    [RelayCommand]
    private void ItemLongPress(TItemViewModel item)
    {
        if (item == null) return;

        Debug.WriteLine($"🔘 [BASE_LIST_VM] LongPress on: {item.Name}");

        // Entrar em modo multi-seleção se não estiver
        if (!IsMultiSelectMode)
        {
            IsMultiSelectMode = true;
            UpdateFabForSelection();
            Debug.WriteLine($"✅ [BASE_LIST_VM] Entered multi-select mode for {EntityNamePlural}");
        }

        // Selecionar o item que foi pressionado
        if (!item.IsSelected)
        {
            item.IsSelected = true;
            if (!SelectedItems.Contains(item))
            {
                SelectedItems.Add(item);
            }
        }

        Debug.WriteLine($"✅ [BASE_LIST_VM] Multi-select activated and item selected: {item.Name}. Total selected: {SelectedItems.Count}");
    }

    #endregion

    #region Multi-Selection

    [RelayCommand]
    private void ToggleMultiSelect()
    {
        if (IsMultiSelectMode)
        {
            ExitMultiSelectMode();
        }
        else
        {
            EnterMultiSelectMode();
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var item in Items)
        {
            if (!item.IsSelected)
            {
                item.IsSelected = true;
                SelectedItems.Add(item);
            }
        }
        UpdateFabForSelection();
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var item in Items)
        {
            item.IsSelected = false;
        }
        SelectedItems.Clear();
        UpdateFabForSelection();
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (!SelectedItems.Any())
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] No {EntityNamePlural} selected for deletion");
            return;
        }

        if (!IsConnected)
        {
            await ShowErrorAsync("No Connection", $"Cannot delete {EntityNamePlural.ToLower()} without internet connection.");
            return;
        }

        try
        {
            var selectedIds = SelectedItems.Select(f => f.Id).ToList();
            var count = selectedIds.Count;

            Debug.WriteLine($"🗑️ [BASE_LIST_VM] Attempting to delete {count} {EntityNamePlural}");

            var confirmed = await ShowConfirmAsync(
                $"Delete {EntityNamePlural}",
                $"Are you sure you want to delete {count} {(count == 1 ? EntityName.ToLower() : EntityNamePlural.ToLower())}?");

            if (!confirmed) return;

            IsLoading = true;
            Debug.WriteLine($"🗑️ [BASE_LIST_VM] Deleting {count} {EntityNamePlural}");
            ExitMultiSelectMode();

            var deletedCount = await _repository.DeleteMultipleAsync(selectedIds);

            if (deletedCount > 0)
            {
                await ShowSuccessAsync($"Successfully deleted {deletedCount} {(deletedCount == 1 ? EntityName.ToLower() : EntityNamePlural.ToLower())}");

                Debug.WriteLine($"🔄 [BASE_LIST_VM] === FORCING COMPLETE REFRESH FOR {EntityNamePlural} ===");

                _repository.InvalidateCacheExternal();
                await _repository.RefreshCacheAsync();
                await LoadItemsDataAsync();

                Debug.WriteLine($"✅ [BASE_LIST_VM] === COMPLETE REFRESH DONE FOR {EntityNamePlural} ===");
            }
            else
            {
                await ShowErrorAsync("Delete Failed", $"No {EntityNamePlural.ToLower()} were deleted");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Delete error: {ex.Message}");

            if (ex.Message.Contains("connection") || ex.Message.Contains("internet"))
            {
                UpdateConnectionStatus(false);
                await ShowErrorAsync("Connection Error", $"Failed to delete {EntityNamePlural.ToLower()}. Check your internet connection.");
            }
            else
            {
                await ShowErrorAsync("Delete Error", $"Failed to delete {EntityNamePlural.ToLower()}. Please try again.");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnItemSelectionChanged(BaseItemViewModel<T> item)
    {
        if (item == null)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] OnItemSelectionChanged called with null {EntityName}");
            return;
        }

        Debug.WriteLine($"🔘 [BASE_LIST_VM] OnItemSelectionChanged: {item.Name}, IsSelected: {item.IsSelected}");

        var typedItem = Items.FirstOrDefault(i => i.Id == item.Id);
        if (typedItem == null) return;

        if (item.IsSelected)
        {
            if (!SelectedItems.Contains(typedItem))
            {
                SelectedItems.Add(typedItem);
                Debug.WriteLine($"➕ [BASE_LIST_VM] Added {item.Name} to selection. Total: {SelectedItems.Count}");
            }

            if (!IsMultiSelectMode)
            {
                EnterMultiSelectMode();
            }
        }
        else
        {
            SelectedItems.Remove(typedItem);
            Debug.WriteLine($"➖ [BASE_LIST_VM] Removed {item.Name} from selection. Total: {SelectedItems.Count}");

            if (!SelectedItems.Any() && IsMultiSelectMode)
            {
                ExitMultiSelectMode();
            }
        }

        UpdateFabForSelection();
    }

    private void EnterMultiSelectMode()
    {
        IsMultiSelectMode = true;
        UpdateFabForSelection();
        Debug.WriteLine($"✅ [BASE_LIST_VM] Entered multi-select mode for {EntityNamePlural}");
    }

    private void ExitMultiSelectMode()
    {
        IsMultiSelectMode = false;
        DeselectAll();
        UpdateFabForSelection();
        Debug.WriteLine($"✅ [BASE_LIST_VM] Exited multi-select mode for {EntityNamePlural}");
    }

    #endregion

    #region Connectivity

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        try
        {
            Debug.WriteLine($"🔍 [BASE_LIST_VM] Testing connection for {EntityNamePlural}...");

            var isConnected = await _repository.TestConnectionAsync();
            UpdateConnectionStatus(isConnected);

            if (isConnected)
            {
                await ShowSuccessAsync("Connection restored! Data is now synchronized.");
                await RefreshAsync();
            }
            else
            {
                await ShowErrorAsync("Still offline", "Check your internet connection and try again.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Connection test error: {ex.Message}");
            UpdateConnectionStatus(false);
            await ShowErrorAsync("Connection test failed", ex.Message);
        }
    }

    private async Task TestConnectionInBackgroundAsync()
    {
        try
        {
            Debug.WriteLine($"🔍 [BASE_LIST_VM] Background connection test for {EntityNamePlural}...");

            var isConnected = await _repository.TestConnectionAsync();
            UpdateConnectionStatus(isConnected);

            Debug.WriteLine($"📡 [BASE_LIST_VM] Background test result: {(isConnected ? "Connected" : "Offline")}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Background connection test error: {ex.Message}");
            UpdateConnectionStatus(false);
        }
    }

    private void UpdateConnectionStatus(bool connected)
    {
        IsConnected = connected;
        ConnectionStatus = connected ? "Connected" : "Offline";
        ConnectionStatusColor = connected ? Colors.Green : Colors.Orange;

        UpdateFabForSelection();

        Debug.WriteLine($"📡 [BASE_LIST_VM] Connection status updated: {ConnectionStatus}");
    }

    #endregion

    #region UI State Management

    private void UpdateFabForSelection()
    {
        var selectedCount = SelectedItems.Count;
        Debug.WriteLine($"🎯 [BASE_LIST_VM] UpdateFabForSelection: {selectedCount} selected, MultiSelect: {IsMultiSelectMode}, Connected: {IsConnected}");

        if (selectedCount > 0)
        {
            FabText = $"Delete ({selectedCount})";
            FabIsVisible = IsConnected;
            Debug.WriteLine($"🎯 [BASE_LIST_VM] FAB set to DELETE mode: {FabText}");
        }
        else if (IsMultiSelectMode)
        {
            FabText = "Cancel";
            FabIsVisible = true;
            Debug.WriteLine($"🎯 [BASE_LIST_VM] FAB set to CANCEL mode: {FabText}");
        }
        else
        {
            FabText = IsConnected ? $"Add {EntityName}" : "Offline";
            FabIsVisible = true;
            Debug.WriteLine($"🎯 [BASE_LIST_VM] FAB set to ADD mode: {FabText}");
        }
    }

    #endregion

    #region Initialization

    public virtual async Task OnAppearingAsync()
    {
        Debug.WriteLine($"👁️ [BASE_LIST_VM] OnAppearing for {EntityNamePlural}");

        if (!Items.Any())
        {
            await LoadItemsAsync();
        }
        else
        {
            _ = TestConnectionInBackgroundAsync();
        }
    }

    #endregion
}