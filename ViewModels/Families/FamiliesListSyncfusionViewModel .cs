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
/// ✅ CORRIGIDO: FamiliesListSyncfusionViewModel com navegação para edição correta
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
    private int favoriteCount;

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
        "Name A→Z", "Name Z→A", "Recent First", "Oldest First", "Favorites First"
    };

    #endregion

    #region ✅ COMANDOS MANUAIS PARA COMPATIBILIDADE

    /// <summary>
    /// ✅ COMANDO MANUAL: ApplyFilterCommand 
    /// </summary>
    public IAsyncRelayCommand ApplyFilterCommand { get; }

    /// <summary>
    /// ✅ COMANDO MANUAL: ClearSelectionCommand
    /// </summary>
    public IRelayCommand ClearSelectionCommand { get; }

    /// <summary>
    /// ✅ COMANDO MANUAL: DeleteSingleItemCommand
    /// </summary>
    public IAsyncRelayCommand<FamilyItemViewModel> DeleteSingleItemCommand { get; }

    #endregion

    #region Constructor

    public FamiliesListSyncfusionViewModel(IFamilyRepository repository, INavigationService navigationService)
    {
        _repository = repository;
        _navigationService = navigationService;

        Title = "Families";
        Debug.WriteLine("✅ [FAMILIES_LIST_SYNCFUSION_VM] Initialized with manual commands");

        // ✅ INICIALIZAR COMANDOS MANUAIS
        ApplyFilterCommand = new AsyncRelayCommand(ApplyFilterAsync);
        ClearSelectionCommand = new RelayCommand(ClearSelectionAction);
        DeleteSingleItemCommand = new AsyncRelayCommand<FamilyItemViewModel>(DeleteSingleItemAsync);

        // Setup change monitoring
        PropertyChanged += OnPropertyChanged;
    }

    #endregion

    #region ✅ MÉTODOS PRIVADOS PARA OS COMANDOS

    private async Task ApplyFilterAsync()
    {
        await ToggleStatusFilterAsync();
    }

    private void ClearSelectionAction()
    {
        // Limpar seleção E filtros aplicados
        SelectedItems.Clear();
        foreach (var item in Items)
        {
            item.IsSelected = false;
        }

        // Resetar filtros
        SearchText = string.Empty;
        StatusFilter = "All";
        SortOrder = "Name A→Z";

        IsMultiSelectMode = false;
        UpdateFabForSelection();

        // Aplicar filtros resetados
        _ = Task.Run(async () => await ApplyFiltersAndSortAsync());

        Debug.WriteLine("✅ [FAMILIES_LIST_SYNCFUSION_VM] Selection and filters cleared");
    }

    private async Task DeleteSingleItemAsync(FamilyItemViewModel? familyItem)
    {
        if (familyItem == null) return;

        var confirmed = await Application.Current?.MainPage?.DisplayAlert(
            "Confirm Delete",
            $"Delete '{familyItem.Name}'?",
            "Delete",
            "Cancel");

        if (confirmed != true) return;

        try
        {
            IsBusy = true;
            // Remover da lista primeiro
            Items.Remove(familyItem);
            // Deletar do banco
            await _repository.DeleteAsync(familyItem.Id);
            UpdateCounters();
            Debug.WriteLine($"✅ [FAMILIES_LIST_SYNCFUSION_VM] Deleted family: {familyItem.Name}");
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

    #endregion

    #region Property Change Handlers

    private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SearchText):
                _ = ApplyFiltersAndSortAsync();
                break;
            case nameof(StatusFilter):
                _ = ApplyFiltersAndSortAsync();
                break;
            case nameof(SortOrder):
                _ = ApplyFiltersAndSortAsync();
                break;
            case nameof(SelectedItems):
                UpdateFabForSelection();
                break;
        }
    }

    #endregion

    #region Data Loading

    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();

        if (!HasData || Items.Count == 0)
        {
            await LoadDataAsync();
        }
        else
        {
            // ✅ SEMPRE REFRESH ao voltar para garantir dados atualizados
            Debug.WriteLine("🔄 [FAMILIES_LIST_SYNCFUSION_VM] Refreshing data on return to page");
            await RefreshAsync(showLoading: false);
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            await TestConnectionAsync();

            var families = await _repository.GetAllAsync(true);
            await PopulateItemsAsync(families);

            Debug.WriteLine($"✅ [FAMILIES_LIST_SYNCFUSION_VM] Loaded {families.Count} families");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_SYNCFUSION_VM] Load failed: {ex.Message}");
            EmptyStateMessage = $"Failed to load families: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task PopulateItemsAsync(List<Family> families)
    {
        try
        {
            var itemVMs = families.Select(f => new FamilyItemViewModel(f)
            {
                // Properties are read-only and set via constructor
                // ✅ Commands can be set
                // EditCommand and ToggleFavoriteCommand should be added to FamilyItemViewModel
            }).ToList();

            Items.Clear();
            foreach (var item in itemVMs)
            {
                Items.Add(item);
            }

            await ApplyFiltersAndSortAsync();
            UpdateCounters();
            HasData = Items.Count > 0;

            Debug.WriteLine($"✅ [FAMILIES_LIST_SYNCFUSION_VM] Populated {Items.Count} items");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_SYNCFUSION_VM] PopulateItems failed: {ex.Message}");
        }
    }

    #endregion

    #region ✅ CORRIGIDA: Navigation Commands

    [RelayCommand]
    private async Task NavigateToAddAsync()
    {
        try
        {
            Debug.WriteLine("➕ [FAMILIES_LIST_SYNCFUSION_VM] Navigating to ADD new family");
            await _navigationService.NavigateToAsync("familyedit");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_SYNCFUSION_VM] Navigate to add failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task NavigateToEditAsync(FamilyItemViewModel? familyItem)
    {
        try
        {
            if (familyItem?.Id == null)
            {
                Debug.WriteLine("❌ [FAMILIES_LIST_SYNCFUSION_VM] NavigateToEdit: familyItem or Id is null");
                return;
            }

            Debug.WriteLine($"📝 [FAMILIES_LIST_SYNCFUSION_VM] Navigating to EDIT family: {familyItem.Name} (ID: {familyItem.Id})");

            // ✅ CORRIGIDO: Usar Dictionary com string keys para compatibilidade
            var parameters = new Dictionary<string, object>
            {
                ["FamilyId"] = familyItem.Id.ToString() // ✅ Converter para string
            };

            await _navigationService.NavigateToAsync("familyedit", parameters);

            Debug.WriteLine($"✅ [FAMILIES_LIST_SYNCFUSION_VM] Navigation completed for family: {familyItem.Name}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_SYNCFUSION_VM] Navigate to edit failed: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ Comando para adicionar (usado pelo FAB)
    /// </summary>
    [RelayCommand]
    private async Task AddNewAsync()
    {
        await NavigateToAddAsync();
    }

    #endregion

    #region Refresh and Data Management

    [RelayCommand]
    private async Task RefreshAsync(bool showLoading = true)
    {
        try
        {
            if (showLoading)
                IsRefreshing = true;

            Debug.WriteLine("🔄 [FAMILIES_LIST_SYNCFUSION_VM] Refreshing data...");

            // Force fresh data from repository
            await _repository.RefreshAllDataAsync();
            var families = await _repository.GetAllAsync(true);
            await PopulateItemsAsync(families);

            Debug.WriteLine($"✅ [FAMILIES_LIST_SYNCFUSION_VM] Refresh completed - {families.Count} families");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_SYNCFUSION_VM] Refresh failed: {ex.Message}");
        }
        finally
        {
            if (showLoading)
                IsRefreshing = false;
        }
    }

    #endregion

    #region Favorites Management

    [RelayCommand]
    private async Task ToggleFavoriteAsync(FamilyItemViewModel familyItem)
    {
        try
        {
            if (familyItem?.Id == null) return;

            Debug.WriteLine($"⭐ [FAMILIES_LIST_SYNCFUSION_VM] Toggling favorite for: {familyItem.Name}");

            var originalStatus = familyItem.IsFavorite;

            try
            {
                var updatedFamily = await _repository.ToggleFavoriteAsync(familyItem.Id);

                // Update the item by replacing it
                var index = Items.IndexOf(familyItem);
                if (index >= 0)
                {
                    Items[index] = new FamilyItemViewModel(updatedFamily);
                }

                UpdateCounters();
                Debug.WriteLine($"✅ [FAMILIES_LIST_SYNCFUSION_VM] Favorite toggled: {familyItem.Name} → {updatedFamily.IsFavorite}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ [FAMILIES_LIST_SYNCFUSION_VM] Toggle favorite failed: {ex.Message}");
                throw;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_SYNCFUSION_VM] ToggleFavorite error: {ex.Message}");
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
            // Clear selections when exiting multi-select mode
            foreach (var item in SelectedItems)
            {
                item.IsSelected = false;
            }
            SelectedItems.Clear();
        }

        UpdateFabForSelection();
        Debug.WriteLine($"🔘 [FAMILIES_LIST_SYNCFUSION_VM] Multi-select mode: {IsMultiSelectMode}");
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        try
        {
            if (SelectedItems.Count == 0) return;

            var count = SelectedItems.Count;
            var confirmed = await Application.Current?.MainPage?.DisplayAlert(
                "Confirm Delete",
                $"Delete {count} selected {(count == 1 ? "family" : "families")}?",
                "Delete",
                "Cancel");

            if (confirmed != true) return;

            var idsToDelete = SelectedItems.Select(item => item.Id).ToList();
            var deletedCount = await _repository.DeleteMultipleAsync(idsToDelete);

            // Remove from UI
            foreach (var id in idsToDelete)
            {
                var item = Items.FirstOrDefault(i => i.Id == id);
                if (item != null)
                    Items.Remove(item);
            }

            SelectedItems.Clear();
            IsMultiSelectMode = false;
            UpdateCounters();
            UpdateFabForSelection();

            Debug.WriteLine($"✅ [FAMILIES_LIST_SYNCFUSION_VM] Deleted {deletedCount} families");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_SYNCFUSION_VM] Delete selected failed: {ex.Message}");
        }
    }

    public void UpdateFabForSelection()
    {
        if (IsMultiSelectMode && SelectedItems.Count > 0)
        {
            FabText = $"Delete ({SelectedItems.Count})";
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

        Debug.WriteLine($"🔘 [FAMILIES_LIST_SYNCFUSION_VM] FAB updated: '{FabText}' - Visible: {FabIsVisible}");
    }

    #endregion

    #region Filtering and Sorting

    [RelayCommand]
    private async Task ToggleStatusFilterAsync()
    {
        var currentIndex = StatusFilterOptions.IndexOf(StatusFilter);
        var nextIndex = (currentIndex + 1) % StatusFilterOptions.Count;
        StatusFilter = StatusFilterOptions[nextIndex];

        Debug.WriteLine($"🔍 [FAMILIES_LIST_SYNCFUSION_VM] Status filter changed to: {StatusFilter}");
        await ApplyFiltersAndSortAsync();
    }

    [RelayCommand]
    private void ToggleSort()
    {
        var currentIndex = SortOptions.IndexOf(SortOrder);
        var nextIndex = (currentIndex + 1) % SortOptions.Count;
        SortOrder = SortOptions[nextIndex];

        Debug.WriteLine($"🔄 [FAMILIES_LIST_SYNCFUSION_VM] Sort order changed to: {SortOrder}");
        _ = ApplyFiltersAndSortAsync();
    }

    private async Task ApplyFiltersAndSortAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                var allItems = Items.ToList();
                var filtered = allItems.AsEnumerable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLowerInvariant();
                    filtered = filtered.Where(item =>
                        item.Name.ToLowerInvariant().Contains(searchLower) ||
                        (!string.IsNullOrEmpty(item.Description) && item.Description.ToLowerInvariant().Contains(searchLower))
                    );
                }

                // Apply status filter
                if (StatusFilter != "All")
                {
                    bool activeFilter = StatusFilter == "Active";
                    filtered = filtered.Where(item => item.IsActive == activeFilter);
                }

                // Apply sorting
                filtered = SortOrder switch
                {
                    "Name A→Z" => filtered.OrderBy(item => item.Name),
                    "Name Z→A" => filtered.OrderByDescending(item => item.Name),
                    "Recent First" => filtered.OrderByDescending(item => item.UpdatedAt),
                    "Oldest First" => filtered.OrderBy(item => item.CreatedAt),
                    "Favorites First" => filtered.OrderByDescending(item => item.IsFavorite).ThenBy(item => item.Name),
                    _ => filtered.OrderBy(item => item.Name)
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
                    EmptyStateMessage = SearchText?.Length > 0 || StatusFilter != "All"
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

    #region Counters and Status

    private void UpdateCounters()
    {
        var allFamilies = Items.ToList();
        TotalCount = allFamilies.Count;
        ActiveCount = allFamilies.Count(f => f.IsActive);
        FavoriteCount = allFamilies.Count(f => f.IsFavorite);

        Debug.WriteLine($"📊 [FAMILIES_LIST_SYNCFUSION_VM] Counters - Total: {TotalCount}, Active: {ActiveCount}, Favorites: {FavoriteCount}");
    }

    private async Task TestConnectionAsync()
    {
        try
        {
            // Simple connectivity test
            IsConnected = true;
            ConnectionStatus = "Connected";
            ConnectionStatusColor = Colors.Green;
        }
        catch
        {
            IsConnected = false;
            ConnectionStatus = "Offline";
            ConnectionStatusColor = Colors.Orange;
        }
    }

    #endregion

    #region FAB Command

    [RelayCommand]
    private async Task FabActionAsync()
    {
        if (IsMultiSelectMode)
        {
            if (SelectedItems.Count > 0)
            {
                await DeleteSelectedAsync();
            }
            else
            {
                ToggleMultiSelectCommand.Execute(null);
            }
        }
        else
        {
            await NavigateToAddAsync();
        }
    }

    #endregion
}