using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace OrchidPro.ViewModels.Families;

/// <summary>
/// ✅ CORRIGIDO: FamiliesListSyncfusionViewModel com binding direto e sem DataSource
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
    private string sortOrder = "Name A→Z";

    // ✅ REMOÇÃO: Não usar DataSource - usar binding direto para Items
    // private DataSource dataSource; // REMOVIDO

    #endregion

    #region Filter Options

    public List<string> StatusFilterOptions { get; } = new() { "All", "Active", "Inactive" };

    public List<string> SortOptions { get; } = new()
    {
        "Name A→Z",
        "Name Z→A",
        "Recent First",
        "Oldest First",
        "Favorites First"
    };

    #endregion

    public FamiliesListSyncfusionViewModel(IFamilyRepository repository, INavigationService navigationService)
    {
        _repository = repository;
        _navigationService = navigationService;
        Title = "Families";

        Debug.WriteLine("✅ [FAMILIES_SYNCFUSION_VM] Initialized with direct Items binding");
    }

    #region Data Loading

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            Debug.WriteLine($"📥 [FAMILIES_SYNCFUSION_VM] Loading families...");

            await LoadItemsDataAsync();

            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Loaded {Items.Count} families");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Load error: {ex.Message}");
            await ShowErrorToast($"Error loading families: {ex.Message}");
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
            Debug.WriteLine($"📊 [FAMILIES_SYNCFUSION_VM] LoadItemsDataAsync - Filter: '{StatusFilter}', Search: '{SearchText}'");

            bool? statusFilter = StatusFilter switch
            {
                "Active" => true,
                "Inactive" => false,
                _ => null
            };

            var entities = await _repository.GetFilteredAsync(SearchText, statusFilter);
            Debug.WriteLine($"📊 [FAMILIES_SYNCFUSION_VM] Retrieved {entities.Count} families from repository");

            var itemViewModels = entities.Select(entity =>
            {
                var itemVm = new FamilyItemViewModel(entity);
                // ✅ Configurar callback de mudança de seleção
                itemVm.SelectionChanged = OnItemSelectionChanged;
                return itemVm;
            }).ToList();

            // ✅ CORREÇÃO PRINCIPAL: Update na UI thread com binding direto
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Items.Clear();

                // Aplicar sorting antes de adicionar
                var sortedItems = ApplySorting(itemViewModels);

                foreach (var item in sortedItems)
                {
                    Items.Add(item);
                }

                Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Items collection updated with {Items.Count} items");
                Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] First item: {Items.FirstOrDefault()?.Name ?? "None"}");
            });

            // Atualizar estatísticas
            TotalCount = entities.Count;
            ActiveCount = entities.Count(e => e.IsActive);
            HasData = entities.Any();

            // Atualizar status de conexão
            IsConnected = await _repository.TestConnectionAsync();
            ConnectionStatus = IsConnected ? "Connected" : "Offline";
            ConnectionStatusColor = IsConnected ? Colors.Green : Colors.Orange;

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
        try
        {
            IsRefreshing = true;
            Debug.WriteLine("🔄 [FAMILIES_SYNCFUSION_VM] Manual refresh triggered");

            await LoadItemsDataAsync();

            await ShowSuccessToast("Families refreshed successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Refresh error: {ex.Message}");
            await ShowErrorToast("Error refreshing families");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    #endregion

    #region Filtering

    [RelayCommand]
    private async Task ApplyFilterAsync()
    {
        try
        {
            Debug.WriteLine($"🔍 [FAMILIES_SYNCFUSION_VM] Applying filter - Search: '{SearchText}', Status: '{StatusFilter}'");

            // Reload data com filtros aplicados
            await LoadItemsDataAsync();

            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Filter applied. Results: {Items.Count}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Apply filter error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ClearFilterAsync()
    {
        try
        {
            SearchText = string.Empty;
            StatusFilter = "All";

            await ApplyFilterAsync();

            Debug.WriteLine("🧹 [FAMILIES_SYNCFUSION_VM] Filters cleared");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Clear filter error: {ex.Message}");
        }
    }

    #endregion

    #region Sort Commands

    [RelayCommand]
    private async Task ToggleSortAsync()
    {
        try
        {
            var currentIndex = SortOptions.IndexOf(SortOrder);
            var nextIndex = (currentIndex + 1) % SortOptions.Count;
            SortOrder = SortOptions[nextIndex];

            Debug.WriteLine($"🔄 [FAMILIES_SYNCFUSION_VM] Sort order changed to: {SortOrder}");

            // Recarregar para aplicar nova ordenação
            await LoadItemsDataAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Toggle sort error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ NOVO: Aplica sorting diretamente na lista de ViewModels
    /// </summary>
    private List<FamilyItemViewModel> ApplySorting(List<FamilyItemViewModel> items)
    {
        try
        {
            return SortOrder switch
            {
                "Name A→Z" => items.OrderBy(f => f.Name).ToList(),
                "Name Z→A" => items.OrderByDescending(f => f.Name).ToList(),
                "Recent First" => items.OrderByDescending(f => f.CreatedAt).ToList(),
                "Oldest First" => items.OrderBy(f => f.CreatedAt).ToList(),
                "Favorites First" => items.OrderByDescending(f => f.IsFavorite).ThenBy(f => f.Name).ToList(),
                _ => items.OrderBy(f => f.Name).ToList()
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] ApplySorting error: {ex.Message}");
            return items.OrderBy(f => f.Name).ToList();
        }
    }

    #endregion

    #region Navigation Commands

    [RelayCommand]
    private async Task AddNewAsync()
    {
        try
        {
            Debug.WriteLine($"➕ [FAMILIES_SYNCFUSION_VM] Navigating to add new family");
            await _navigationService.NavigateToAsync("familyedit");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Add new navigation error: {ex.Message}");
            await ShowErrorToast("Error navigating to new family page");
        }
    }

    [RelayCommand]
    private async Task NavigateToEditAsync(FamilyItemViewModel item)
    {
        if (item == null) return;

        try
        {
            Debug.WriteLine($"📝 [FAMILIES_SYNCFUSION_VM] Navigating to edit family: {item.Name}");

            var parameters = new Dictionary<string, object>
            {
                ["FamilyId"] = item.Id.ToString()
            };

            await _navigationService.NavigateToAsync("familyedit", parameters);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Edit navigation error: {ex.Message}");
            await ShowErrorToast("Error navigating to edit family");
        }
    }

    [RelayCommand]
    private async Task DeleteSingleAsync(FamilyItemViewModel item)
    {
        if (item == null || item.IsSystemDefault) return;

        try
        {
            Debug.WriteLine($"🗑️ [FAMILIES_SYNCFUSION_VM] Deleting family: {item.Name}");

            await _repository.DeleteAsync(item.Id);
            await LoadItemsDataAsync();

            await ShowSuccessToast($"Family '{item.Name}' deleted successfully");
            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Family deleted and list refreshed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Delete error: {ex.Message}");
            await ShowErrorToast($"Error deleting family: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(FamilyItemViewModel item)
    {
        if (item == null) return;

        try
        {
            Debug.WriteLine($"⭐ [FAMILIES_SYNCFUSION_VM] Toggling favorite for: {item.Name}");

            await _repository.ToggleFavoriteAsync(item.Id);
            await LoadItemsDataAsync(); // Refresh to get updated data

            var message = $"'{item.Name}' " + (item.IsFavorite ? "added to" : "removed from") + " favorites";
            await ShowSuccessToast(message);

            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Favorite toggled: {item.Name}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Toggle favorite error: {ex.Message}");
            await ShowErrorToast("Error updating favorite status");
        }
    }

    #endregion

    #region Multi-Selection

    [RelayCommand]
    private void ToggleMultiSelect()
    {
        IsMultiSelectMode = !IsMultiSelectMode;

        if (!IsMultiSelectMode)
        {
            // Clear all selections
            foreach (var item in Items)
            {
                item.IsSelected = false;
            }
            SelectedItems.Clear();
        }

        UpdateFabForSelection();
        Debug.WriteLine($"🔘 [FAMILIES_SYNCFUSION_VM] Multi-select mode: {IsMultiSelectMode}");
    }

    [RelayCommand]
    private void SelectAllItems()
    {
        try
        {
            foreach (var item in Items)
            {
                if (!item.IsSelected)
                {
                    item.IsSelected = true;
                    if (!SelectedItems.Contains(item))
                    {
                        SelectedItems.Add(item);
                    }
                }
            }

            UpdateFabForSelection();
            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Selected all {SelectedItems.Count} items");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Select all error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void DeselectAllItems()
    {
        try
        {
            foreach (var item in Items)
            {
                item.IsSelected = false;
            }
            SelectedItems.Clear();

            UpdateFabForSelection();
            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Deselected all items");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Deselect all error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (!SelectedItems.Any()) return;

        try
        {
            var itemsToDelete = SelectedItems.Where(i => !i.IsSystemDefault).ToList();
            if (!itemsToDelete.Any())
            {
                await ShowErrorToast("Cannot delete system default families");
                return;
            }

            Debug.WriteLine($"🗑️ [FAMILIES_SYNCFUSION_VM] Deleting {itemsToDelete.Count} selected families");

            var deletedCount = await _repository.DeleteMultipleAsync(itemsToDelete.Select(i => i.Id));

            // Exit multi-select mode and refresh
            ToggleMultiSelect();
            await LoadItemsDataAsync();

            await ShowSuccessToast($"Deleted {deletedCount} families successfully");
            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Deleted {deletedCount} families");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Delete selected error: {ex.Message}");
            await ShowErrorToast($"Error deleting families: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ Callback para mudanças de seleção nos items
    /// </summary>
    private void OnItemSelectionChanged(FamilyItemViewModel item)
    {
        try
        {
            if (item.IsSelected && !SelectedItems.Contains(item))
            {
                SelectedItems.Add(item);
            }
            else if (!item.IsSelected && SelectedItems.Contains(item))
            {
                SelectedItems.Remove(item);
            }

            UpdateFabForSelection();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] OnItemSelectionChanged error: {ex.Message}");
        }
    }

    public void UpdateFabForSelection()
    {
        try
        {
            var selectedCount = SelectedItems.Count;

            if (selectedCount > 0)
            {
                FabText = $"Delete ({selectedCount})";
                FabIsVisible = true;
            }
            else if (IsMultiSelectMode)
            {
                FabText = "Cancel Selection";
                FabIsVisible = true;
            }
            else
            {
                FabText = "Add Family";
                FabIsVisible = true;
            }

            Debug.WriteLine($"🏷️ [FAMILIES_SYNCFUSION_VM] FAB updated: '{FabText}', Selected: {selectedCount}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] UpdateFabForSelection error: {ex.Message}");
        }
    }

    #endregion

    #region Search Text Changed Handler

    /// <summary>
    /// ✅ CORREÇÃO: Debounce search para evitar muitas chamadas
    /// </summary>
    private CancellationTokenSource? _searchCancellationTokenSource;

    partial void OnSearchTextChanged(string value)
    {
        try
        {
            Debug.WriteLine($"🔍 [FAMILIES_SYNCFUSION_VM] Search text changed: '{value}'");

            // Cancel previous search
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource = new CancellationTokenSource();

            // Debounce search
            _ = Task.Run(async () =>
            {
                await Task.Delay(300, _searchCancellationTokenSource.Token);

                if (!_searchCancellationTokenSource.Token.IsCancellationRequested)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await ApplyFilterAsync();
                    });
                }
            }, _searchCancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] OnSearchTextChanged error: {ex.Message}");
        }
    }

    partial void OnStatusFilterChanged(string value)
    {
        try
        {
            Debug.WriteLine($"🏷️ [FAMILIES_SYNCFUSION_VM] Status filter changed: '{value}'");
            _ = ApplyFilterAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] OnStatusFilterChanged error: {ex.Message}");
        }
    }

    #endregion

    #region Toast Messages

    private async Task ShowSuccessToast(string message)
    {
        try
        {
            var toast = Toast.Make(message, ToastDuration.Short, 14);
            await toast.Show();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] ShowSuccessToast error: {ex.Message}");
        }
    }

    private async Task ShowErrorToast(string message)
    {
        try
        {
            var toast = Toast.Make(message, ToastDuration.Long, 14);
            await toast.Show();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] ShowErrorToast error: {ex.Message}");
        }
    }

    #endregion

    #region Initialization

    public virtual async Task OnAppearingAsync()
    {
        try
        {
            Debug.WriteLine($"👁️ [FAMILIES_SYNCFUSION_VM] OnAppearing");

            // Always load data to ensure fresh information
            await LoadItemsAsync();

            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] OnAppearing completed with {Items.Count} items");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] OnAppearing error: {ex.Message}");
            await ShowErrorToast("Error loading families");
        }
    }

    #endregion
}