using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using OrchidPro.ViewModels;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace OrchidPro.ViewModels.Families;

/// <summary>
/// ✅ CORRIGIDO: FamiliesListSyncfusionViewModel com ToggleFavorite funcional
/// - Adicionado comando para salvar favoritos no banco
/// - Corrigido contador de favoritos 
/// - Otimizado para não recarregar lista desnecessariamente
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
    private int favoriteCount; // ✅ NOVO: Contador de favoritos

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

    #endregion

    #region Filter and Sort Options

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

    #region Constructor

    public FamiliesListSyncfusionViewModel(IFamilyRepository repository, INavigationService navigationService)
    {
        _repository = repository;
        _navigationService = navigationService;

        Debug.WriteLine("✅ [FAMILIES_LIST_SYNCFUSION_VM] Constructor - Repository and navigation initialized");

        // ✅ Subscribe to search text changes
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SearchText))
            {
                _ = Task.Run(async () => await PerformSearchAsync());
            }
        };

        // ✅ Initialize color using static resource
        try
        {
            var primaryColor = Application.Current?.Resources.TryGetValue("Primary", out var colorValue) == true
                ? (Color)colorValue
                : Color.FromArgb("#A47764");
            ConnectionStatusColor = primaryColor;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_SYNCFUSION_VM] Error loading primary color: {ex.Message}");
            ConnectionStatusColor = Color.FromArgb("#A47764");
        }
    }

    #endregion

    #region ✅ COMANDO CORRIGIDO - ToggleFavorite

    /// <summary>
    /// ✅ NOVO: Comando para toggle favorito com salvamento no banco
    /// </summary>
    [RelayCommand]
    private async Task ToggleFavoriteAsync(FamilyItemViewModel familyItem)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            Debug.WriteLine($"⭐ [FAMILIES_LIST_SYNCFUSION_VM] Toggling favorite for: {familyItem.Name} (Current: {familyItem.IsFavorite})");

            // ✅ Converter para modelo e toggle
            var family = familyItem.ToModel();
            family.ToggleFavorite(); // Método que já existe no modelo Family

            // ✅ Salvar no banco de dados
            var updatedFamily = await _repository.UpdateAsync(family);
            Debug.WriteLine($"✅ [FAMILIES_LIST_SYNCFUSION_VM] Family updated in database: {updatedFamily.Name} (Favorite: {updatedFamily.IsFavorite})");

            // ✅ Atualizar o item na lista sem recarregar tudo
            var newFamilyItem = new FamilyItemViewModel(updatedFamily);
            var index = Items.IndexOf(familyItem);

            if (index >= 0)
            {
                // Manter estado de seleção se existir
                newFamilyItem.IsSelected = familyItem.IsSelected;

                Items[index] = newFamilyItem;
                Debug.WriteLine($"✅ [FAMILIES_LIST_SYNCFUSION_VM] Updated item in list at index {index}");
            }

            // ✅ Atualizar contadores
            UpdateCounters();

            // ✅ REMOVIDO: Alert removido, apenas log - Toast será mostrado no code-behind
            Debug.WriteLine($"✅ [FAMILIES_LIST_SYNCFUSION_VM] Toggle favorite completed successfully - {(updatedFamily.IsFavorite ? "Added to" : "Removed from")} favorites");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_SYNCFUSION_VM] Toggle favorite failed: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", "Failed to update favorite status", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion

    #region Data Loading and Refresh

    public async Task OnAppearingAsync()
    {
        if (Items.Count == 0)
        {
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            IsLoading = true;
            Debug.WriteLine("🔄 [FAMILIES_LIST_SYNCFUSION_VM] Loading families data...");

            var families = await _repository.GetAllAsync(includeInactive: true);
            Debug.WriteLine($"✅ [FAMILIES_LIST_SYNCFUSION_VM] Loaded {families.Count} families from repository");

            Items.Clear();
            foreach (var family in families)
            {
                Items.Add(new FamilyItemViewModel(family));
            }

            await ApplyFiltersAndSortAsync();
            UpdateCounters();

            HasData = Items.Count > 0;
            Debug.WriteLine($"✅ [FAMILIES_LIST_SYNCFUSION_VM] Data loading completed - {Items.Count} items");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_SYNCFUSION_VM] Load data failed: {ex.Message}");
            EmptyStateMessage = "Failed to load data. Tap to retry.";
            HasData = false;
        }
        finally
        {
            IsLoading = false;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadDataAsync();
        IsRefreshing = false;
    }

    #endregion

    #region ✅ CONTADORES CORRIGIDOS

    /// <summary>
    /// ✅ NOVO: Atualiza contadores incluindo favoritos
    /// </summary>
    private void UpdateCounters()
    {
        try
        {
            TotalCount = Items.Count;
            ActiveCount = Items.Count(x => x.IsActive);
            FavoriteCount = Items.Count(x => x.IsFavorite); // ✅ NOVO: Contador de favoritos

            Debug.WriteLine($"📊 [FAMILIES_LIST_SYNCFUSION_VM] Counters updated - Total: {TotalCount}, Active: {ActiveCount}, Favorites: {FavoriteCount}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_SYNCFUSION_VM] Error updating counters: {ex.Message}");
        }
    }

    #endregion

    #region Search and Filter

    private async Task PerformSearchAsync()
    {
        try
        {
            await Task.Delay(300); // Debounce
            await ApplyFiltersAndSortAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_SYNCFUSION_VM] Search failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ToggleStatusFilterAsync()
    {
        var currentIndex = StatusFilterOptions.IndexOf(StatusFilter);
        var nextIndex = (currentIndex + 1) % StatusFilterOptions.Count;
        StatusFilter = StatusFilterOptions[nextIndex];

        await ApplyFiltersAndSortAsync();
        Debug.WriteLine($"🔄 [FAMILIES_LIST_SYNCFUSION_VM] Status filter changed to: {StatusFilter}");
    }

    [RelayCommand]
    private async Task ToggleSortAsync()
    {
        await ApplyFiltersAndSortAsync();
        Debug.WriteLine($"🔄 [FAMILIES_LIST_SYNCFUSION_VM] Sort order applied: {SortOrder}");
    }

    private async Task ApplyFiltersAndSortAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                var filtered = Items.AsEnumerable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLowerInvariant();
                    filtered = filtered.Where(x =>
                        x.Name.ToLowerInvariant().Contains(searchLower) ||
                        (x.Description?.ToLowerInvariant().Contains(searchLower) ?? false));
                }

                // Apply status filter
                filtered = StatusFilter switch
                {
                    "Active" => filtered.Where(x => x.IsActive),
                    "Inactive" => filtered.Where(x => !x.IsActive),
                    _ => filtered // "All"
                };

                // Apply sorting
                filtered = SortOrder switch
                {
                    "Name Z→A" => filtered.OrderByDescending(x => x.Name),
                    "Recent First" => filtered.OrderByDescending(x => x.CreatedAt),
                    "Oldest First" => filtered.OrderBy(x => x.CreatedAt),
                    "Favorites First" => filtered.OrderByDescending(x => x.IsFavorite).ThenBy(x => x.Name),
                    _ => filtered.OrderBy(x => x.Name) // "Name A→Z"
                };

                var result = filtered.ToList();

                Device.BeginInvokeOnMainThread(() =>
                {
                    Items.Clear();
                    foreach (var item in result)
                    {
                        Items.Add(item);
                    }

                    HasData = Items.Count > 0;
                    EmptyStateMessage = !string.IsNullOrWhiteSpace(SearchText) || StatusFilter != "All"
                        ? "No families match your filters"
                        : "No families found. Tap + to add one";

                    UpdateCounters();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ [FAMILIES_LIST_SYNCFUSION_VM] Filter/sort failed: {ex.Message}");
            }
        });
    }

    #endregion

    #region Navigation Commands

    [RelayCommand]
    private async Task NavigateToAddAsync()
    {
        await _navigationService.NavigateToAsync("familyedit");
    }

    [RelayCommand]
    private async Task NavigateToEditAsync(FamilyItemViewModel familyItem)
    {
        if (familyItem?.Id == null) return;

        // ✅ CORREÇÃO: Navegar com parâmetro correto para edição
        var parameters = new Dictionary<string, object>
        {
            ["FamilyId"] = familyItem.Id
        };

        await _navigationService.NavigateToAsync("familyedit", parameters);
        Debug.WriteLine($"✅ [FAMILIES_LIST_SYNCFUSION_VM] Navigating to edit family: {familyItem.Name} (ID: {familyItem.Id})");
    }

    /// <summary>
    /// ✅ NOVO: Comando para adicionar (usado pelo FAB e toolbar)
    /// </summary>
    [RelayCommand]
    private async Task AddNewAsync()
    {
        await NavigateToAddAsync();
    }

    /// <summary>
    /// ✅ NOVO: Comando para aplicar filtros
    /// </summary>
    [RelayCommand]
    private async Task ApplyFilterAsync()
    {
        await ToggleStatusFilterAsync();
    }

    /// <summary>
    /// ✅ NOVO: Comando para deletar item individual (swipe action)
    /// </summary>
    [RelayCommand]
    private async Task DeleteSingleItemAsync(FamilyItemViewModel familyItem)
    {
        if (familyItem == null) return;

        var confirmed = await Shell.Current.DisplayAlert(
            "Confirm Delete",
            $"Delete '{familyItem.Name}'?",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        try
        {
            IsBusy = true;

            // ✅ Remover da lista primeiro
            Items.Remove(familyItem);

            // Deletar do banco
            await _repository.DeleteAsync(familyItem.Id);

            UpdateCounters();

            // ✅ REMOVIDO: Alert de sucesso - apenas log para o Toast ser mostrado no code-behind
            Debug.WriteLine($"✅ [FAMILIES_LIST_SYNCFUSION_VM] Deleted family: {familyItem.Name}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_SYNCFUSION_VM] Delete single item failed: {ex.Message}");
            // Recarregar em caso de erro
            await LoadDataAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion

    #region Selection and Multi-select

    [RelayCommand]
    private void ToggleMultiSelect()
    {
        IsMultiSelectMode = !IsMultiSelectMode;

        if (!IsMultiSelectMode)
        {
            // Clear all selections when exiting multi-select mode
            SelectedItems.Clear();
            foreach (var item in Items)
            {
                item.IsSelected = false;
            }
        }

        UpdateFabForSelection();
        Debug.WriteLine($"🔄 [FAMILIES_LIST_SYNCFUSION_VM] Multi-select mode: {IsMultiSelectMode}");
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var item in Items)
        {
            if (!SelectedItems.Contains(item))
            {
                SelectedItems.Add(item);
                item.IsSelected = true;
            }
        }
        UpdateFabForSelection();
    }

    [RelayCommand]
    private void ClearSelection()
    {
        // ✅ NOVO: Limpar seleção E filtros aplicados
        SelectedItems.Clear();
        foreach (var item in Items)
        {
            item.IsSelected = false;
        }

        // ✅ Resetar filtros
        SearchText = string.Empty;
        StatusFilter = "All";
        SortOrder = "Name A→Z";

        IsMultiSelectMode = false;
        UpdateFabForSelection();

        // ✅ Aplicar filtros resetados
        _ = Task.Run(async () => await ApplyFiltersAndSortAsync());

        Debug.WriteLine("✅ [FAMILIES_LIST_SYNCFUSION_VM] Selection and filters cleared");
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (!SelectedItems.Any()) return;

        var selectedCount = SelectedItems.Count;
        var confirmed = await Shell.Current.DisplayAlert(
            "Confirm Delete",
            $"Delete {selectedCount} selected families?",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        try
        {
            IsBusy = true;
            var idsToDelete = SelectedItems.Select(x => x.Id).ToList();

            // ✅ OTIMIZAÇÃO: Remover da lista primeiro, depois do banco
            var itemsToRemove = SelectedItems.ToList();
            foreach (var item in itemsToRemove)
            {
                Items.Remove(item);
            }
            SelectedItems.Clear();

            // Deletar do banco
            await _repository.DeleteMultipleAsync(idsToDelete);

            IsMultiSelectMode = false;
            UpdateCounters();
            UpdateFabForSelection();

            // ✅ REMOVIDO: Alert de sucesso - apenas log
            Debug.WriteLine($"✅ [FAMILIES_LIST_SYNCFUSION_VM] Deleted {selectedCount} families successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_SYNCFUSION_VM] Delete failed: {ex.Message}");
            // Recarregar em caso de erro
            await LoadDataAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void UpdateFabForSelection()
    {
        var selectedCount = SelectedItems?.Count ?? 0;

        if (selectedCount > 0)
        {
            FabText = $"Delete ({selectedCount})";
            FabIsVisible = true;
        }
        else if (IsMultiSelectMode)
        {
            FabText = "Cancel";
            FabIsVisible = true;
        }
        else
        {
            FabText = "Add Family";
            FabIsVisible = true;
        }
    }

    #endregion

    #region FAB Actions

    [RelayCommand]
    private async Task FabActionAsync()
    {
        if (SelectedItems.Any())
        {
            await DeleteSelectedAsync();
        }
        else if (IsMultiSelectMode)
        {
            ClearSelection();
        }
        else
        {
            await NavigateToAddAsync();
        }
    }

    #endregion

    #region Connection Status

    private void UpdateConnectionStatus()
    {
        try
        {
            IsConnected = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
            ConnectionStatus = IsConnected ? "Connected" : "Offline";

            // ✅ Usar cores do ResourceDictionary
            var successColor = Application.Current?.Resources.TryGetValue("SuccessColor", out var success) == true
                ? (Color)success
                : Color.FromArgb("#388E3C");

            var errorColor = Application.Current?.Resources.TryGetValue("ErrorColor", out var error) == true
                ? (Color)error
                : Color.FromArgb("#D32F2F");

            ConnectionStatusColor = IsConnected ? successColor : errorColor;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_SYNCFUSION_VM] Connection status update failed: {ex.Message}");
        }
    }

    #endregion
}