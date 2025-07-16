using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services.Navigation;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace OrchidPro.ViewModels;

/// <summary>
/// PASSO 5.1: BaseListViewModel corrigido - sem problemas de tipos genéricos
/// Extrai toda funcionalidade comum: listagem, filtros, multisseleção, etc.
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

    #region Abstract Properties - Deve ser implementado pela classe filha

    /// <summary>
    /// Nome da entidade (ex: "Family", "Species")
    /// </summary>
    public abstract string EntityName { get; }

    /// <summary>
    /// Nome da entidade no plural (ex: "Families", "Species")
    /// </summary>
    public abstract string EntityNamePlural { get; }

    /// <summary>
    /// Rota para navegação de edição (ex: "familyedit")
    /// </summary>
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

    #region Abstract Methods - Deve ser implementado pela classe filha

    /// <summary>
    /// Cria ItemViewModel a partir da entidade
    /// </summary>
    protected abstract TItemViewModel CreateItemViewModel(T entity);

    #endregion

    #region Data Loading

    /// <summary>
    /// Carrega dados
    /// </summary>
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

    /// <summary>
    /// ✅ CORRIGIDO: Carrega dados internamente usando Action simples
    /// </summary>
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

            var entityList = await _repository.GetFilteredAsync(SearchText, statusFilter);

            var itemViewModels = entityList.Select(entity =>
            {
                var vm = CreateItemViewModel(entity);
                vm.IsSelected = SelectedItems.Any(si => si.Id == entity.Id);

                // ✅ CORRIGIDO: Usar Action simples para evitar problemas de tipos
                vm.SelectionChangedAction = item => OnItemSelectionChanged((TItemViewModel)item);

                return vm;
            }).ToList();

            Items.Clear();
            foreach (var item in itemViewModels)
            {
                Items.Add(item);
            }

            await UpdateStatisticsAsync();

            HasData = Items.Count > 0;

            if (!HasData)
            {
                EmptyStateMessage = GetEmptyStateMessage();
            }

            Debug.WriteLine($"📊 [BASE_LIST_VM] Data loaded: {Items.Count} {EntityNamePlural}, HasData: {HasData}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Load data error: {ex.Message}");
            UpdateConnectionStatus(false);
            throw;
        }
    }

    #endregion

    #region Refresh

    /// <summary>
    /// Refresh dos dados
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            Debug.WriteLine($"🔄 [BASE_LIST_VM] === STARTING REFRESH {EntityNamePlural} ===");
            IsRefreshing = true;

            var isConnectedNow = await _repository.TestConnectionAsync();
            Debug.WriteLine($"🔍 [BASE_LIST_VM] Connection test result: {isConnectedNow}");

            if (!isConnectedNow)
            {
                UpdateConnectionStatus(false);
                await ShowErrorAsync("No Connection", "Cannot refresh data without internet connection.");
                return;
            }

            UpdateConnectionStatus(true);

            Debug.WriteLine($"🔄 [BASE_LIST_VM] Forcing cache refresh for {EntityNamePlural}...");
            await _repository.RefreshCacheAsync();

            Debug.WriteLine($"🔄 [BASE_LIST_VM] Reloading data after cache refresh for {EntityNamePlural}...");
            await LoadItemsDataAsync();

            Debug.WriteLine($"✅ [BASE_LIST_VM] Refresh completed successfully for {EntityNamePlural}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Refresh error: {ex.Message}");

            if (ex.Message.Contains("connection") || ex.Message.Contains("internet"))
            {
                UpdateConnectionStatus(false);
                await ShowErrorAsync("Connection Error", "Failed to refresh data. Check your internet connection.");
            }
            else
            {
                await ShowErrorAsync("Refresh Failed", "Failed to refresh data from server.");
            }
        }
        finally
        {
            Debug.WriteLine($"🔄 [BASE_LIST_VM] === REFRESH COMPLETED FOR {EntityNamePlural} - RESETTING STATE ===");
            IsRefreshing = false;
        }
    }

    #endregion

    #region Connectivity

    /// <summary>
    /// Teste de conectividade em background
    /// </summary>
    private async Task TestConnectionInBackgroundAsync()
    {
        try
        {
            Debug.WriteLine($"🔍 [BASE_LIST_VM] Testing connection in background for {EntityNamePlural}...");

            var connected = await _repository.TestConnectionAsync();

            if (IsConnected != connected)
            {
                UpdateConnectionStatus(connected);

                if (!connected)
                {
                    EmptyStateMessage = GetEmptyStateMessage();
                }
            }

            Debug.WriteLine($"🔍 [BASE_LIST_VM] Background test result for {EntityNamePlural}: {connected}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Background connectivity test failed for {EntityNamePlural}: {ex.Message}");
            UpdateConnectionStatus(false);
        }
    }

    /// <summary>
    /// Teste manual de conectividade
    /// </summary>
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        try
        {
            Debug.WriteLine($"🔍 [BASE_LIST_VM] Manual connection test for {EntityNamePlural}...");

            var connected = await _repository.TestConnectionAsync();
            UpdateConnectionStatus(connected);

            Debug.WriteLine($"🔍 [BASE_LIST_VM] Manual test result for {EntityNamePlural}: {connected}");

            var message = connected ? "Connected to server" : "Connection failed";
            await ShowSuccessAsync(message);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Manual connection test failed for {EntityNamePlural}: {ex.Message}");
            UpdateConnectionStatus(false);
            await ShowErrorAsync("Connection Test", "Connection test failed");
        }
    }

    /// <summary>
    /// Atualiza status de conectividade
    /// </summary>
    private void UpdateConnectionStatus(bool connected)
    {
        IsConnected = connected;

        if (connected)
        {
            ConnectionStatus = "Connected";
            ConnectionStatusColor = Colors.Green;
        }
        else
        {
            ConnectionStatus = "Disconnected";
            ConnectionStatusColor = Colors.Red;
        }

        UpdateFabForSelection();
    }

    #endregion

    #region Search and Filters

    /// <summary>
    /// Busca
    /// </summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        Debug.WriteLine($"🔍 [BASE_LIST_VM] Searching {EntityNamePlural} for: '{SearchText}'");
        await LoadItemsAsync();
    }

    /// <summary>
    /// Limpa busca
    /// </summary>
    [RelayCommand]
    private async Task ClearSearchAsync()
    {
        SearchText = string.Empty;
        await LoadItemsAsync();
    }

    #endregion

    #region Navigation

    /// <summary>
    /// Navega para adicionar
    /// </summary>
    [RelayCommand]
    private async Task AddItemAsync()
    {
        if (!IsConnected)
        {
            var canProceed = await ShowConfirmAsync(
                "Offline Mode",
                "You're currently offline. Some features may not work properly. Continue anyway?");

            if (!canProceed) return;
        }

        try
        {
            Debug.WriteLine($"➕ [BASE_LIST_VM] Navigating to add {EntityName}");
            await _navigationService.NavigateToAsync(EditRoute);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Navigation error: {ex.Message}");
            await ShowErrorAsync("Navigation Error", $"Failed to open add {EntityName.ToLower()} page");
        }
    }

    /// <summary>
    /// Navega para editar
    /// </summary>
    [RelayCommand]
    private async Task EditItemAsync(TItemViewModel? item)
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

    #endregion

    #region Multi-Selection

    /// <summary>
    /// Alterna modo de multisseleção
    /// </summary>
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

    /// <summary>
    /// Seleciona todos
    /// </summary>
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

    /// <summary>
    /// Deseleciona todos
    /// </summary>
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

    /// <summary>
    /// Deleta selecionados
    /// </summary>
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

    /// <summary>
    /// ✅ CORRIGIDO: Manipula mudança de seleção
    /// </summary>
    private void OnItemSelectionChanged(TItemViewModel? item)
    {
        if (item == null)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] OnItemSelectionChanged called with null {EntityName}");
            return;
        }

        Debug.WriteLine($"🔘 [BASE_LIST_VM] OnItemSelectionChanged: {item.Name}, IsSelected: {item.IsSelected}");

        if (item.IsSelected)
        {
            if (!SelectedItems.Contains(item))
            {
                SelectedItems.Add(item);
                Debug.WriteLine($"➕ [BASE_LIST_VM] Added {item.Name} to selection. Total: {SelectedItems.Count}");
            }

            if (!IsMultiSelectMode)
            {
                EnterMultiSelectMode();
            }
        }
        else
        {
            SelectedItems.Remove(item);
            Debug.WriteLine($"➖ [BASE_LIST_VM] Removed {item.Name} from selection. Total: {SelectedItems.Count}");

            if (!SelectedItems.Any() && IsMultiSelectMode)
            {
                ExitMultiSelectMode();
            }
        }

        UpdateFabForSelection();
    }

    /// <summary>
    /// Entra em modo de multisseleção
    /// </summary>
    private void EnterMultiSelectMode()
    {
        IsMultiSelectMode = true;
        UpdateFabForSelection();
        Debug.WriteLine($"✅ [BASE_LIST_VM] Entered multi-select mode for {EntityNamePlural}");
    }

    /// <summary>
    /// Sai do modo de multisseleção
    /// </summary>
    private void ExitMultiSelectMode()
    {
        IsMultiSelectMode = false;
        DeselectAll();
        UpdateFabForSelection();
        Debug.WriteLine($"✅ [BASE_LIST_VM] Exited multi-select mode for {EntityNamePlural}");
    }

    #endregion

    #region UI State Management

    /// <summary>
    /// Atualiza FAB baseado na seleção
    /// </summary>
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

        OnPropertyChanged(nameof(FabText));
        OnPropertyChanged(nameof(FabIsVisible));
    }

    /// <summary>
    /// Atualiza estatísticas
    /// </summary>
    private async Task UpdateStatisticsAsync()
    {
        try
        {
            var stats = await _repository.GetStatisticsAsync();
            TotalCount = stats.TotalCount;
            ActiveCount = stats.ActiveCount;

            Debug.WriteLine($"📊 [BASE_LIST_VM] Stats updated for {EntityNamePlural} - Total: {TotalCount}, Active: {ActiveCount}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Stats update error for {EntityNamePlural}: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtém mensagem de estado vazio
    /// </summary>
    private string GetEmptyStateMessage()
    {
        if (!IsConnected)
        {
            return $"No internet connection\nConnect to view your {EntityNamePlural.ToLower()}";
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            return $"No {EntityNamePlural.ToLower()} found matching '{SearchText}'";
        }

        if (StatusFilter != "All")
        {
            return $"No {StatusFilter.ToLower()} {EntityNamePlural.ToLower()} found";
        }

        return $"No {EntityNamePlural.ToLower()} yet. Add your first {EntityName.ToLower()} to get started!";
    }

    #endregion

    #region Lifecycle

    /// <summary>
    /// Página aparecendo
    /// </summary>
    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();

        Debug.WriteLine($"👁️ [BASE_LIST_VM] Page appearing for {EntityNamePlural} - loading data optimistically");
        await LoadItemsAsync();
    }

    #endregion

    #region Property Change Handlers

    /// <summary>
    /// Handler para mudança de texto de busca
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(300);
            if (SearchText == value)
            {
                await SearchAsync();
            }
        });
    }

    /// <summary>
    /// Handler para mudança de filtro de status
    /// </summary>
    partial void OnStatusFilterChanged(string value)
    {
        Debug.WriteLine($"🔽 [BASE_LIST_VM] Status filter changed to: {value} for {EntityNamePlural}");
        _ = LoadItemsAsync();
    }

    /// <summary>
    /// Observer da propriedade SelectedItems
    /// </summary>
    partial void OnSelectedItemsChanged(ObservableCollection<TItemViewModel> value)
    {
        Debug.WriteLine($"🔘 [BASE_LIST_VM] SelectedItems changed for {EntityNamePlural} - Count: {value.Count}");
        UpdateFabForSelection();
    }

    #endregion
}