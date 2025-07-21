using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using Syncfusion.Maui.DataSource;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace OrchidPro.ViewModels.Families;

/// <summary>
/// ✅ CORRIGIDO: FamiliesListViewModel com favoritos, sem erros de modificadores de acesso
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

        // ✅ MELHORADO: Configurar DataSource com filtro nativo
        DataSource = new DataSource
        {
            Source = Items,
            Filter = FilterFamilies
        };

        Debug.WriteLine("✅ [FAMILIES_SYNCFUSION_VM] Initialized with improved UX and favorites support");
    }

    #endregion

    #region Lifecycle

    public async Task OnAppearingAsync()
    {
        try
        {
            Debug.WriteLine("🔄 [FAMILIES_SYNCFUSION_VM] OnAppearing - Loading data...");

            await TestConnectionAsync();
            await LoadItemsAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] OnAppearing error: {ex.Message}");
            await ShowErrorAsync("Load Error", "Failed to load families. Please try again.");
        }
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

            await LoadItemsDataAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] LoadItems error: {ex.Message}");
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

            // ✅ NOVO: Toast de confirmação ao invés de alerta bloqueante
            var toast = Toast.Make("Families refreshed", ToastDuration.Short, 14);
            await toast.Show();
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

    #region Sort Commands

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
    /// ✅ MELHORADO: Aplicar ordenação ao DataSource com favoritos primeiro
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

            // ✅ NOVO: SEMPRE ordenar favoritos primeiro
            DataSource.SortDescriptors.Add(new Syncfusion.Maui.DataSource.SortDescriptor
            {
                PropertyName = nameof(FamilyItemViewModel.IsFavorite),
                Direction = Syncfusion.Maui.DataSource.ListSortDirection.Descending
            });

            // Aplicar ordenação secundária
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

            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Sorting applied: Favorites first, then {SortOrder}");
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
            var canProceed = await ShowConfirmationAsync(
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
            var canProceed = await ShowConfirmationAsync(
                "Offline Mode",
                "You're currently offline. Editing may not work properly. Continue anyway?");

            if (!canProceed) return;
        }

        try
        {
            Debug.WriteLine($"✏️ [FAMILIES_SYNCFUSION_VM] Navigating to edit family: {item.Name}");

            var parameters = new Dictionary<string, object>
            {
                { "FamilyId", item.Id.ToString() }
            };

            await _navigationService.NavigateToAsync("familyedit", parameters);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Edit navigation error: {ex.Message}");
            await ShowErrorAsync("Navigation Error", "Failed to open edit family page");
        }
    }

    #endregion

    #region Selection Management

    /// <summary>
    /// ✅ MELHORADO: Toggle multi-select mode com feedback visual
    /// </summary>
    [RelayCommand]
    private void ToggleMultiSelect()
    {
        try
        {
            IsMultiSelectMode = !IsMultiSelectMode;
            Debug.WriteLine($"🔘 [FAMILIES_SYNCFUSION_VM] Multi-select mode: {IsMultiSelectMode}");

            if (!IsMultiSelectMode)
            {
                // ✅ NOVO: Auto-cancelar seleções quando sair do modo multi-select
                ClearSelection();
            }

            UpdateFabForSelection();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] ToggleMultiSelect error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ NOVO: Comando para cancelar seleção explicitamente
    /// </summary>
    [RelayCommand]
    private void CancelSelection()
    {
        try
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Canceling selection mode");

            IsMultiSelectMode = false;
            ClearSelection();
            UpdateFabForSelection();

            // ✅ NOVO: Toast de confirmação
            var toast = Toast.Make("Selection cancelled", ToastDuration.Short, 14);
            _ = toast.Show();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] CancelSelection error: {ex.Message}");
        }
    }

    private void ClearSelection()
    {
        try
        {
            foreach (var item in SelectedItems.ToList())
            {
                item.IsSelected = false;
            }
            SelectedItems.Clear();

            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] All selections cleared");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] ClearSelection error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        try
        {
            Debug.WriteLine($"☑️ [FAMILIES_SYNCFUSION_VM] Selecting all items");

            foreach (var item in Items)
            {
                if (!item.IsSelected)
                {
                    item.IsSelected = true;
                }
            }

            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Selected {SelectedItems.Count} items");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] SelectAll error: {ex.Message}");
        }
    }

    public void OnItemSelectionChanged(BaseItemViewModel<Family> item)
    {
        try
        {
            if (item is not FamilyItemViewModel familyItem) return;

            Debug.WriteLine($"🔄 [FAMILIES_SYNCFUSION_VM] Selection changed: {familyItem.Name} -> {familyItem.IsSelected}");

            if (familyItem.IsSelected)
            {
                if (!SelectedItems.Contains(familyItem))
                {
                    SelectedItems.Add(familyItem);
                }
            }
            else
            {
                if (SelectedItems.Contains(familyItem))
                {
                    SelectedItems.Remove(familyItem);
                }
            }

            UpdateFabForSelection();

            Debug.WriteLine($"📊 [FAMILIES_SYNCFUSION_VM] Total selected: {SelectedItems.Count}");
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
            if (IsMultiSelectMode && SelectedItems.Count > 0)
            {
                FabText = $"Delete ({SelectedItems.Count})";
                FabIsVisible = true;
            }
            else if (IsMultiSelectMode)
            {
                FabText = "Select Items";
                FabIsVisible = true;
            }
            else
            {
                FabText = "Add Family";
                FabIsVisible = true;
            }

            Debug.WriteLine($"🔄 [FAMILIES_SYNCFUSION_VM] FAB updated: {FabText}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] UpdateFabForSelection error: {ex.Message}");
        }
    }

    #endregion

    #region Delete Operations

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (SelectedItems.Count == 0)
        {
            await ShowErrorAsync("No Selection", "Please select items to delete first.");
            return;
        }

        try
        {
            var itemsToDelete = SelectedItems.ToList();
            var systemItems = itemsToDelete.Where(i => i.IsSystemDefault).ToList();

            if (systemItems.Any())
            {
                await ShowErrorAsync("Cannot Delete",
                    $"Cannot delete system default families: {string.Join(", ", systemItems.Select(i => i.Name))}");
                return;
            }

            // ✅ MELHORADO: Confirmação com opção de cancelar seleção
            var result = await Application.Current.MainPage.DisplayActionSheet(
                $"Delete {itemsToDelete.Count} families?",
                "Cancel Selection", // ✅ NOVO: Opção para cancelar seleção
                "Delete Forever",
                itemsToDelete.Select(i => i.Name).Take(3).ToArray()
            );

            if (result == "Cancel Selection")
            {
                // ✅ NOVO: Cancelar seleção ao invés de apenas cancelar operação
                CancelSelection();
                return;
            }

            if (result != "Delete Forever") return;

            Debug.WriteLine($"🗑️ [FAMILIES_SYNCFUSION_VM] Deleting {itemsToDelete.Count} families");

            foreach (var item in itemsToDelete)
            {
                await _repository.DeleteAsync(item.Id);
                Items.Remove(item);
            }

            ClearSelection();
            IsMultiSelectMode = false;
            UpdateFabForSelection();

            // ✅ NOVO: Toast ao invés de alerta bloqueante
            var toast = Toast.Make($"Deleted {itemsToDelete.Count} families", ToastDuration.Short, 14);
            await toast.Show();

            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Successfully deleted {itemsToDelete.Count} families");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] DeleteSelected error: {ex.Message}");
            await ShowErrorAsync("Delete Error", "Failed to delete selected families. Please try again.");
        }
    }

    [RelayCommand]
    private async Task DeleteSingleAsync(FamilyItemViewModel item)
    {
        if (item == null) return;

        if (item.IsSystemDefault)
        {
            await ShowErrorAsync("Cannot Delete", "System default families cannot be deleted.");
            return;
        }

        try
        {
            var confirm = await ShowConfirmationAsync("Delete Family", $"Are you sure you want to delete '{item.Name}'?");
            if (!confirm) return;

            Debug.WriteLine($"🗑️ [FAMILIES_SYNCFUSION_VM] Deleting single family: {item.Name}");

            await _repository.DeleteAsync(item.Id);
            Items.Remove(item);

            if (SelectedItems.Contains(item))
            {
                SelectedItems.Remove(item);
            }

            // ✅ NOVO: Toast ao invés de alerta bloqueante
            var toast = Toast.Make($"Deleted '{item.Name}'", ToastDuration.Short, 14);
            await toast.Show();

            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Successfully deleted family: {item.Name}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] DeleteSingle error: {ex.Message}");
            await ShowErrorAsync("Delete Error", "Failed to delete family. Please try again.");
        }
    }

    #endregion

    #region Swipe Actions

    /// <summary>
    /// ✅ NOVO: Toggle favorite via swipe
    /// </summary>
    [RelayCommand]
    private async Task ToggleFavoriteAsync(FamilyItemViewModel item)
    {
        if (item == null) return;

        try
        {
            Debug.WriteLine($"⭐ [FAMILIES_SYNCFUSION_VM] Toggling favorite for: {item.Name}");

            // ✅ NOVO: Usar o serviço para toggle favorite
            var supabaseService = Application.Current.Handler.MauiContext.Services.GetService<SupabaseFamilyService>();
            if (supabaseService != null)
            {
                var updatedFamily = await supabaseService.ToggleFavoriteAsync(item.Id);

                // Atualizar o item na lista
                var existingItem = Items.FirstOrDefault(i => i.Id == item.Id);
                if (existingItem != null)
                {
                    var index = Items.IndexOf(existingItem);
                    var newItem = new FamilyItemViewModel(updatedFamily);
                    newItem.SelectionChangedAction = OnItemSelectionChanged;

                    Items[index] = newItem;
                }

                // Re-aplicar ordenação para colocar favoritos no topo
                ApplySortingToDataSource();
                DataSource.RefreshFilter();

                // ✅ NOVO: Toast ao invés de alerta
                var message = updatedFamily.IsFavorite ? "Added to favorites" : "Removed from favorites";
                var toast = Toast.Make($"'{updatedFamily.Name}' {message}", ToastDuration.Short, 14);
                await toast.Show();

                Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Favorite toggled: {updatedFamily.Name} -> {updatedFamily.IsFavorite}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] ToggleFavorite error: {ex.Message}");
            await ShowErrorAsync("Favorite Error", "Failed to update favorite status. Please try again.");
        }
    }

    #endregion

    #region Connection Management

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        try
        {
            Debug.WriteLine("🔄 [FAMILIES_SYNCFUSION_VM] Testing connection...");

            var isConnected = await _repository.TestConnectionAsync();
            UpdateConnectionStatus(isConnected);

            Debug.WriteLine($"📶 [FAMILIES_SYNCFUSION_VM] Connection test result: {isConnected}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Connection test error: {ex.Message}");
            UpdateConnectionStatus(false);
        }
    }

    private void UpdateConnectionStatus(bool isConnected)
    {
        IsConnected = isConnected;
        ConnectionStatus = isConnected ? "Connected" : "Offline";
        ConnectionStatusColor = isConnected ? Colors.Green : Colors.Orange;

        Debug.WriteLine($"📶 [FAMILIES_SYNCFUSION_VM] Connection status updated: {ConnectionStatus}");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// ✅ CORRIGIDO: Usar método público da base ao invés de protected override
    /// </summary>
    public async Task ShowErrorAsync(string title, string message)
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
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] ShowErrorAsync error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ CORRIGIDO: Usar método público da base ao invés de protected override
    /// </summary>
    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        try
        {
            if (Application.Current?.MainPage != null)
            {
                return await Application.Current.MainPage.DisplayAlert(title, message, "Yes", "No");
            }
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] ShowConfirmationAsync error: {ex.Message}");
            return false;
        }
    }

    #endregion
}