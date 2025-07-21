using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using Syncfusion.Maui.DataSource;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace OrchidPro.ViewModels.Families;

/// <summary>
/// ✅ LIMPO: FamiliesListViewModel otimizado para Syncfusion ListView
/// Usa DataSource nativo para filtros, ordenação e agrupamento
/// </summary>
public partial class FamiliesListSyncfusionViewModel : BaseViewModel
{
    private readonly IFamilyRepository _repository;
    private readonly INavigationService _navigationService;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<FamilyItemViewModel> items = new();

    [ObservableProperty]
    private ObservableCollection<FamilyItemViewModel> selectedItems = new();

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
    private string emptyStateMessage = "No families found";

    [ObservableProperty]
    private string statusFilter = "All";

    [ObservableProperty]
    private int totalCount;

    [ObservableProperty]
    private int activeCount;

    [ObservableProperty]
    private bool fabIsVisible = true;

    [ObservableProperty]
    private string fabText = "Add Family";

    [ObservableProperty]
    private string connectionStatus = "Connected";

    [ObservableProperty]
    private Color connectionStatusColor = Colors.Green;

    [ObservableProperty]
    private bool isConnected = true;

    [ObservableProperty]
    private DataSource dataSource;

    // ✅ NOVO: Propriedades de ordenação
    [ObservableProperty]
    private string sortOrder = "Name A→Z";

    #endregion

    #region Filter Options

    public List<string> StatusFilterOptions { get; } = new() { "All", "Active", "Inactive" };

    public List<string> SortOptions { get; } = new()
    {
        "Name A→Z",
        "Name Z→A",
        "Recent First",
        "Oldest First"
    };

    #endregion

    #region Constructor

    public FamiliesListSyncfusionViewModel(IFamilyRepository repository, INavigationService navigationService)
    {
        _repository = repository;
        _navigationService = navigationService;
        Title = "Families";
        IsConnected = true;
        ConnectionStatus = "Connected";
        ConnectionStatusColor = Colors.Green;
        FabText = "Add Family";

        // ✅ NOVO: Inicializar DataSource do Syncfusion
        DataSource = new DataSource();

        Debug.WriteLine("✅ [FAMILIES_SYNCFUSION_VM] Initialized with Syncfusion DataSource");
    }

    #endregion

    #region Data Loading

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            Debug.WriteLine($"📥 [FAMILIES_SYNCFUSION_VM] Loading families with filter: {StatusFilter}");

            await LoadItemsDataAsync();

            // Teste de conectividade em background
            _ = Task.Run(async () =>
            {
                await Task.Delay(500);
                await TestConnectionInBackgroundAsync();
            });

            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Loaded {Items.Count} families");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Load error: {ex.Message}");
            await ShowErrorAsync("Failed to load families", "Check your connection and try again.");
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
                var itemVm = new FamilyItemViewModel(entity);
                itemVm.SelectionChangedAction = OnItemSelectionChanged;
                return itemVm;
            }).ToList();

            Items.Clear();
            foreach (var item in itemViewModels)
            {
                Items.Add(item);
            }

            // ✅ ATUALIZADO: Configurar DataSource com ordenação
            DataSource.Source = Items;
            ApplySortingToDataSource();
            DataSource.RefreshFilter();

            TotalCount = entities.Count;
            ActiveCount = entities.Count(e => e.IsActive);
            HasData = entities.Any();

            Debug.WriteLine($"📊 [FAMILIES_SYNCFUSION_VM] Stats - Total: {TotalCount}, Active: {ActiveCount}, HasData: {HasData}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] LoadItemsDataAsync error: {ex.Message}");
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
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Refresh error: {ex.Message}");
            await ShowErrorAsync("Refresh Failed", "Failed to refresh data. Please try again.");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    #endregion

    #region Syncfusion Native Filtering

    /// <summary>
    /// ✅ Filtro nativo do Syncfusion - muito mais performático
    /// </summary>
    public bool FilterFamilies(object item)
    {
        if (item is not FamilyItemViewModel family) return false;

        try
        {
            var searchText = SearchText?.Trim()?.ToLowerInvariant() ?? string.Empty;
            var statusFilter = StatusFilter;

            // Filtro por texto
            bool matchesSearch = string.IsNullOrEmpty(searchText) ||
                               (family.Name?.ToLowerInvariant().Contains(searchText) == true) ||
                               (family.Description?.ToLowerInvariant().Contains(searchText) == true);

            // Filtro por status
            bool matchesStatus = statusFilter switch
            {
                "Active" => family.IsActive,
                "Inactive" => !family.IsActive,
                _ => true
            };

            return matchesSearch && matchesStatus;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Filter error: {ex.Message}");
            return true; // Em caso de erro, mostrar o item
        }
    }

    [RelayCommand]
    private void ApplyFilter()
    {
        try
        {
            Debug.WriteLine($"🔍 [FAMILIES_SYNCFUSION_VM] Applying filter - Search: '{SearchText}', Status: '{StatusFilter}', Sort: '{SortOrder}'");

            // ✅ NOVO: Aplicar ordenação ANTES do filtro
            ApplySortingToDataSource();

            // Aplicar filtro nativo do Syncfusion
            DataSource.Filter = FilterFamilies;
            DataSource.RefreshFilter();

            var filteredCount = DataSource.DisplayItems.Count;
            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Filter applied. Results: {filteredCount}/{Items.Count}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Apply filter error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ClearFilter()
    {
        SearchText = string.Empty;
        StatusFilter = "All";
        ApplyFilter();
    }

    #endregion

    #region Sort Commands - NOVO

    [RelayCommand]
    private void ToggleSort()
    {
        try
        {
            var currentIndex = SortOptions.IndexOf(SortOrder);
            var nextIndex = (currentIndex + 1) % SortOptions.Count;
            SortOrder = SortOptions[nextIndex];

            Debug.WriteLine($"🔄 [FAMILIES_SYNCFUSION_VM] Sort order changed to: {SortOrder}");

            // Aplicar nova ordenação imediatamente
            ApplySortingToDataSource();

            // Re-aplicar filtros com nova ordenação
            DataSource?.RefreshFilter();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Toggle sort error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ NOVO: Aplicar ordenação ao DataSource
    /// </summary>
    private void ApplySortingToDataSource()
    {
        try
        {
            if (DataSource?.SortDescriptors == null)
            {
                Debug.WriteLine($"⚠️ [FAMILIES_SYNCFUSION_VM] DataSource or SortDescriptors is null");
                return;
            }

            // Limpar ordenações existentes
            DataSource.SortDescriptors.Clear();

            // Aplicar nova ordenação
            switch (SortOrder)
            {
                case "Name A→Z":
                    DataSource.SortDescriptors.Add(new Syncfusion.Maui.DataSource.SortDescriptor
                    {
                        PropertyName = nameof(FamilyItemViewModel.Name),
                        Direction = Syncfusion.Maui.DataSource.ListSortDirection.Ascending
                    });
                    break;

                case "Name Z→A":
                    DataSource.SortDescriptors.Add(new Syncfusion.Maui.DataSource.SortDescriptor
                    {
                        PropertyName = nameof(FamilyItemViewModel.Name),
                        Direction = Syncfusion.Maui.DataSource.ListSortDirection.Descending
                    });
                    break;

                case "Recent First":
                    DataSource.SortDescriptors.Add(new Syncfusion.Maui.DataSource.SortDescriptor
                    {
                        PropertyName = nameof(FamilyItemViewModel.CreatedAt),
                        Direction = Syncfusion.Maui.DataSource.ListSortDirection.Descending
                    });
                    break;

                case "Oldest First":
                    DataSource.SortDescriptors.Add(new Syncfusion.Maui.DataSource.SortDescriptor
                    {
                        PropertyName = nameof(FamilyItemViewModel.CreatedAt),
                        Direction = Syncfusion.Maui.DataSource.ListSortDirection.Ascending
                    });
                    break;
            }

            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Sorting applied: {SortOrder}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] ApplySortingToDataSource error: {ex.Message}");
        }
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
            Debug.WriteLine($"➕ [FAMILIES_SYNCFUSION_VM] Navigating to add new family");
            await _navigationService.NavigateToAsync("familyedit");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Add new navigation error: {ex.Message}");
            await ShowErrorAsync("Navigation Error", "Failed to open add family page");
        }
    }

    [RelayCommand]
    private async Task NavigateToEditAsync(FamilyItemViewModel item)
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
            Debug.WriteLine($"📝 [FAMILIES_SYNCFUSION_VM] Navigating to edit family: {item.Name}");

            var parameters = new Dictionary<string, object>
            {
                ["FamilyId"] = item.Id
            };

            await _navigationService.NavigateToAsync("familyedit", parameters);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Edit navigation error: {ex.Message}");
            await ShowErrorAsync("Navigation Error", "Failed to open edit family page");
        }
    }

    [RelayCommand]
    private async Task DeleteSingleItemAsync(FamilyItemViewModel item)
    {
        if (item == null) return;

        if (!IsConnected)
        {
            await ShowErrorAsync("No Connection", "Cannot delete family without internet connection.");
            return;
        }

        if (item.IsSystemDefault)
        {
            await ShowErrorAsync("Cannot Delete", "This is a system default family and cannot be deleted.");
            return;
        }

        try
        {
            Debug.WriteLine($"🗑️ [FAMILIES_SYNCFUSION_VM] Attempting to delete single family: {item.Name}");

            var confirmed = await ShowConfirmAsync(
                "Delete Family",
                $"Are you sure you want to delete '{item.Name}'?");

            if (!confirmed) return;

            IsLoading = true;

            var success = await _repository.DeleteAsync(item.Id);

            if (success)
            {
                await ShowSuccessAsync($"Successfully deleted family '{item.Name}'");

                Debug.WriteLine($"🔄 [FAMILIES_SYNCFUSION_VM] === REFRESHING AFTER SINGLE DELETE ===");

                _repository.InvalidateCacheExternal();
                await _repository.RefreshCacheAsync();
                await LoadItemsDataAsync();

                Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] === REFRESH COMPLETE ===");
            }
            else
            {
                await ShowErrorAsync("Delete Failed", $"Failed to delete family '{item.Name}'");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Single delete error: {ex.Message}");

            if (ex.Message.Contains("connection") || ex.Message.Contains("internet"))
            {
                UpdateConnectionStatus(false);
                await ShowErrorAsync("Connection Error", "Failed to delete family. Check your internet connection.");
            }
            else
            {
                await ShowErrorAsync("Delete Error", "Failed to delete family. Please try again.");
            }
        }
        finally
        {
            IsLoading = false;
        }
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
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] No families selected for deletion");
            return;
        }

        if (!IsConnected)
        {
            await ShowErrorAsync("No Connection", "Cannot delete families without internet connection.");
            return;
        }

        try
        {
            var selectedIds = SelectedItems.Select(f => f.Id).ToList();
            var count = selectedIds.Count;

            Debug.WriteLine($"🗑️ [FAMILIES_SYNCFUSION_VM] Attempting to delete {count} families");

            var confirmed = await ShowConfirmAsync(
                "Delete Families",
                $"Are you sure you want to delete {count} {(count == 1 ? "family" : "families")}?");

            if (!confirmed) return;

            IsLoading = true;
            ExitMultiSelectMode();

            var deletedCount = await _repository.DeleteMultipleAsync(selectedIds);

            if (deletedCount > 0)
            {
                await ShowSuccessAsync($"Successfully deleted {deletedCount} {(deletedCount == 1 ? "family" : "families")}");

                Debug.WriteLine($"🔄 [FAMILIES_SYNCFUSION_VM] === FORCING COMPLETE REFRESH ===");

                _repository.InvalidateCacheExternal();
                await _repository.RefreshCacheAsync();
                await LoadItemsDataAsync();

                Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] === COMPLETE REFRESH DONE ===");
            }
            else
            {
                await ShowErrorAsync("Delete Failed", "No families were deleted");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Delete error: {ex.Message}");

            if (ex.Message.Contains("connection") || ex.Message.Contains("internet"))
            {
                UpdateConnectionStatus(false);
                await ShowErrorAsync("Connection Error", "Failed to delete families. Check your internet connection.");
            }
            else
            {
                await ShowErrorAsync("Delete Error", "Failed to delete families. Please try again.");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnItemSelectionChanged(BaseItemViewModel<Family> item)
    {
        if (item == null)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] OnItemSelectionChanged called with null family");
            return;
        }

        Debug.WriteLine($"🔘 [FAMILIES_SYNCFUSION_VM] OnItemSelectionChanged: {item.Name}, IsSelected: {item.IsSelected}");

        var typedItem = Items.FirstOrDefault(i => i.Id == item.Id);
        if (typedItem == null) return;

        if (item.IsSelected)
        {
            if (!SelectedItems.Contains(typedItem))
            {
                SelectedItems.Add(typedItem);
                Debug.WriteLine($"➕ [FAMILIES_SYNCFUSION_VM] Added {item.Name} to selection. Total: {SelectedItems.Count}");
            }

            if (!IsMultiSelectMode)
            {
                EnterMultiSelectMode();
            }
        }
        else
        {
            SelectedItems.Remove(typedItem);
            Debug.WriteLine($"➖ [FAMILIES_SYNCFUSION_VM] Removed {item.Name} from selection. Total: {SelectedItems.Count}");

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
        Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Entered multi-select mode");
    }

    private void ExitMultiSelectMode()
    {
        IsMultiSelectMode = false;
        DeselectAll();
        UpdateFabForSelection();
        Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Exited multi-select mode");
    }

    #endregion

    #region Connectivity

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        try
        {
            Debug.WriteLine($"🔍 [FAMILIES_SYNCFUSION_VM] Testing connection...");

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
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Connection test error: {ex.Message}");
            UpdateConnectionStatus(false);
            await ShowErrorAsync("Connection test failed", ex.Message);
        }
    }

    private async Task TestConnectionInBackgroundAsync()
    {
        try
        {
            Debug.WriteLine($"🔍 [FAMILIES_SYNCFUSION_VM] Background connection test...");

            var isConnected = await _repository.TestConnectionAsync();
            UpdateConnectionStatus(isConnected);

            Debug.WriteLine($"📡 [FAMILIES_SYNCFUSION_VM] Background test result: {(isConnected ? "Connected" : "Offline")}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Background connection test error: {ex.Message}");
            UpdateConnectionStatus(false);
        }
    }

    private void UpdateConnectionStatus(bool connected)
    {
        IsConnected = connected;
        ConnectionStatus = connected ? "Connected" : "Offline";
        ConnectionStatusColor = connected ? Colors.Green : Colors.Orange;

        UpdateFabForSelection();

        Debug.WriteLine($"📡 [FAMILIES_SYNCFUSION_VM] Connection status updated: {ConnectionStatus}");
    }

    #endregion

    #region UI State Management

    public void UpdateFabForSelection()
    {
        var selectedCount = SelectedItems.Count;
        Debug.WriteLine($"🎯 [FAMILIES_SYNCFUSION_VM] UpdateFabForSelection: {selectedCount} selected, MultiSelect: {IsMultiSelectMode}, Connected: {IsConnected}");

        if (selectedCount > 0)
        {
            FabText = $"Delete ({selectedCount})";
            FabIsVisible = IsConnected;
            Debug.WriteLine($"🎯 [FAMILIES_SYNCFUSION_VM] FAB set to DELETE mode: {FabText}");
        }
        else if (IsMultiSelectMode)
        {
            FabText = "Cancel";
            FabIsVisible = true;
            Debug.WriteLine($"🎯 [FAMILIES_SYNCFUSION_VM] FAB set to CANCEL mode: {FabText}");
        }
        else
        {
            FabText = IsConnected ? "Add Family" : "Offline";
            FabIsVisible = true;
            Debug.WriteLine($"🎯 [FAMILIES_SYNCFUSION_VM] FAB set to ADD mode: {FabText}");
        }
    }

    #endregion

    #region Search Text Changed Handler

    /// <summary>
    /// ✅ Handler otimizado para mudanças no texto de busca
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        Debug.WriteLine($"🔍 [FAMILIES_SYNCFUSION_VM] Search text changed: '{value}'");

        // Aplicar filtro nativo instantaneamente
        ApplyFilter();
    }

    /// <summary>
    /// ✅ Handler otimizado para mudanças no filtro de status
    /// </summary>
    partial void OnStatusFilterChanged(string value)
    {
        Debug.WriteLine($"🏷️ [FAMILIES_SYNCFUSION_VM] Status filter changed: '{value}'");

        // Aplicar filtro nativo instantaneamente
        ApplyFilter();
    }

    #endregion

    #region Initialization

    public virtual async Task OnAppearingAsync()
    {
        Debug.WriteLine($"👁️ [FAMILIES_SYNCFUSION_VM] OnAppearing");

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