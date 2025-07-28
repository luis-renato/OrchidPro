using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models.Base;
using OrchidPro.Services.Navigation;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace OrchidPro.ViewModels.Base;

/// <summary>
/// ✅ FIXED: BaseListViewModel with UNIFIED DELETE FLOW - No duplicate confirmations
/// </summary>
public abstract partial class BaseListViewModel<T, TItemViewModel> : BaseViewModel
    where T : class, IBaseEntity, new()
    where TItemViewModel : BaseItemViewModel<T>
{
    protected readonly IBaseRepository<T> _repository;
    protected readonly INavigationService _navigationService;

    #region ✅ Observable Properties

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
    private int favoriteCount;

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

    #region Manual Commands

    public IAsyncRelayCommand ApplyFilterCommand { get; private set; }
    public IRelayCommand ClearFilterCommand { get; private set; }
    public IRelayCommand ClearSelectionCommand { get; private set; }
    public IAsyncRelayCommand<TItemViewModel> DeleteSingleItemCommand { get; private set; }

    #endregion

    #region Abstract Properties

    public abstract string EntityName { get; }
    public abstract string EntityNamePlural { get; }
    public abstract string EditRoute { get; }

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

        // Initialize manual commands
        ApplyFilterCommand = new AsyncRelayCommand(ApplyFilterAsync);
        ClearFilterCommand = new RelayCommand(ClearFilterAction);
        ClearSelectionCommand = new RelayCommand(ClearSelectionAction);
        DeleteSingleItemCommand = new AsyncRelayCommand<TItemViewModel>(DeleteSingleItemAsync);

        // Setup change monitoring
        PropertyChanged += OnPropertyChanged;

        Debug.WriteLine($"✅ [BASE_LIST_VM] Enhanced initialized for {EntityNamePlural}");
    }

    #endregion

    #region Manual Command Methods

    private async Task ApplyFilterAsync()
    {
        await ApplyFiltersAndSortAsync();
    }

    private void ClearFilterAction()
    {
        // Clear only filters, not selection
        SearchText = string.Empty;
        StatusFilter = "All";
        SortOrder = "Name A→Z";

        // Apply reset filters
        _ = Task.Run(async () => await ApplyFiltersAndSortAsync());

        Debug.WriteLine($"✅ [BASE_LIST_VM] Filters cleared for {EntityNamePlural}");
    }

    private void ClearSelectionAction()
    {
        // Clear selection AND applied filters
        SelectedItems.Clear();
        foreach (var item in Items)
        {
            item.IsSelected = false;
        }

        // Reset filters
        SearchText = string.Empty;
        StatusFilter = "All";
        SortOrder = "Name A→Z";

        IsMultiSelectMode = false;
        UpdateFabForSelection();

        // Apply reset filters
        _ = Task.Run(async () => await ApplyFiltersAndSortAsync());

        Debug.WriteLine($"✅ [BASE_LIST_VM] Selection and filters cleared for {EntityNamePlural}");
    }

    /// <summary>
    /// ✅ FIXED: UNIFIED DELETE FLOW - Single confirmation, single success message
    /// Used by: Swipe delete, manual delete commands
    /// </summary>
    private async Task DeleteSingleItemAsync(TItemViewModel? item)
    {
        if (item == null) return;

        try
        {
            Debug.WriteLine($"🗑️ [BASE_LIST_VM] UNIFIED Delete flow for: {item.Name}");

            // ✅ SINGLE CONFIRMATION DIALOG
            var confirmed = await ShowConfirmAsync(
                $"Delete {EntityName}",
                $"Are you sure you want to delete '{item.Name}'?");

            if (!confirmed)
            {
                Debug.WriteLine($"❌ [BASE_LIST_VM] Delete cancelled by user");
                return;
            }

            // ✅ PERFORM DELETE
            IsBusy = true;
            var success = await _repository.DeleteAsync(item.Id);

            if (success)
            {
                // ✅ REMOVE FROM UI IMMEDIATELY
                Items.Remove(item);
                UpdateCounters();

                // ✅ SINGLE SUCCESS MESSAGE (Toast only)
                await ShowSuccessToastAsync($"'{item.Name}' deleted successfully");

                Debug.WriteLine($"✅ [BASE_LIST_VM] UNIFIED Delete completed: {item.Name}");
            }
            else
            {
                await ShowErrorToastAsync($"Failed to delete '{item.Name}'");
                Debug.WriteLine($"❌ [BASE_LIST_VM] Delete failed: {item.Name}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Delete error: {ex.Message}");
            await ShowErrorToastAsync($"Delete failed: {ex.Message}");
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

    #region Abstract Methods

    protected abstract TItemViewModel CreateItemViewModel(T entity);

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
            Debug.WriteLine($"🔄 [BASE_LIST_VM] Refreshing data on return to {EntityNamePlural} page");
            await RefreshInternalAsync(showLoading: false);
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            await TestConnectionInternalAsync();

            var entities = await _repository.GetAllAsync(true);
            await PopulateItemsAsync(entities);

            Debug.WriteLine($"✅ [BASE_LIST_VM] Loaded {entities.Count} {EntityNamePlural}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Load failed: {ex.Message}");
            EmptyStateMessage = $"Failed to load {EntityNamePlural.ToLower()}: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task PopulateItemsAsync(List<T> entities)
    {
        try
        {
            var itemVMs = entities.Select(e => CreateItemViewModel(e)).ToList();

            Items.Clear();
            foreach (var item in itemVMs)
            {
                Items.Add(item);
            }

            await ApplyFiltersAndSortAsync();
            UpdateCounters();
            HasData = Items.Count > 0;

            Debug.WriteLine($"✅ [BASE_LIST_VM] Populated {Items.Count} {EntityNamePlural}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] PopulateItems failed: {ex.Message}");
        }
    }

    #endregion

    #region Navigation Commands

    [RelayCommand]
    private async Task NavigateToAddAsync()
    {
        try
        {
            Debug.WriteLine($"➕ [BASE_LIST_VM] Navigating to ADD new {EntityName}");
            await _navigationService.NavigateToAsync(EditRoute);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Navigate to add failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task NavigateToEditAsync(TItemViewModel? item)
    {
        try
        {
            if (item?.Id == null)
            {
                Debug.WriteLine($"❌ [BASE_LIST_VM] NavigateToEdit: item or Id is null");
                return;
            }

            Debug.WriteLine($"📝 [BASE_LIST_VM] Navigating to EDIT {EntityName}: {item.Name} (ID: {item.Id})");

            var parameters = new Dictionary<string, object>
            {
                [$"{EntityName}Id"] = item.Id.ToString()
            };

            await _navigationService.NavigateToAsync(EditRoute, parameters);

            Debug.WriteLine($"✅ [BASE_LIST_VM] Navigation completed for {EntityName}: {item.Name}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Navigate to edit failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task AddNewAsync()
    {
        await NavigateToAddAsync();
    }

    #endregion

    #region Refresh and Data Management

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsRefreshing = true;

            Debug.WriteLine($"🔄 [BASE_LIST_VM] Refreshing {EntityNamePlural} data...");

            await _repository.RefreshCacheAsync();
            var entities = await _repository.GetAllAsync(true);
            await PopulateItemsAsync(entities);

            Debug.WriteLine($"✅ [BASE_LIST_VM] Refresh completed - {entities.Count} {EntityNamePlural}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Refresh failed: {ex.Message}");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task RefreshInternalAsync(bool showLoading = true)
    {
        try
        {
            if (showLoading)
                IsRefreshing = true;

            Debug.WriteLine($"🔄 [BASE_LIST_VM] Refreshing {EntityNamePlural} data...");

            await _repository.RefreshCacheAsync();
            var entities = await _repository.GetAllAsync(true);
            await PopulateItemsAsync(entities);

            Debug.WriteLine($"✅ [BASE_LIST_VM] Refresh completed - {entities.Count} {EntityNamePlural}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Refresh failed: {ex.Message}");
        }
        finally
        {
            if (showLoading)
                IsRefreshing = false;
        }
    }

    #endregion

    #region Favorites Management (Virtual)

    [RelayCommand]
    protected virtual async Task ToggleFavoriteAsync(TItemViewModel item)
    {
        try
        {
            if (item?.Id == null) return;

            Debug.WriteLine($"⭐ [BASE_LIST_VM] Base ToggleFavorite called for: {item.Name}");
            Debug.WriteLine($"⚠️ [BASE_LIST_VM] Override this method in specific ViewModel if entity supports favorites");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] ToggleFavorite error: {ex.Message}");
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
            foreach (var item in SelectedItems)
            {
                item.IsSelected = false;
            }
            SelectedItems.Clear();
        }

        UpdateFabForSelection();
        Debug.WriteLine($"🔘 [BASE_LIST_VM] Multi-select mode: {IsMultiSelectMode}");
    }

    /// <summary>
    /// ✅ FIXED: UNIFIED DELETE FLOW for multiple items
    /// </summary>
    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        try
        {
            if (SelectedItems.Count == 0)
            {
                Debug.WriteLine($"❌ [BASE_LIST_VM] DeleteSelected called with 0 items");
                return;
            }

            var count = SelectedItems.Count;
            Debug.WriteLine($"🗑️ [BASE_LIST_VM] DeleteSelected starting with {count} items");

            // ✅ SINGLE CONFIRMATION DIALOG
            var confirmed = await ShowConfirmAsync(
                "Confirm Delete",
                $"Delete {count} selected {(count == 1 ? EntityName.ToLower() : EntityNamePlural.ToLower())}?");

            if (!confirmed)
            {
                Debug.WriteLine($"❌ [BASE_LIST_VM] Delete cancelled by user");
                return;
            }

            var idsToDelete = SelectedItems.Select(item => item.Id).ToList();
            Debug.WriteLine($"🗑️ [BASE_LIST_VM] Will delete IDs: {string.Join(", ", idsToDelete)}");

            var deletedCount = await _repository.DeleteMultipleAsync(idsToDelete);
            Debug.WriteLine($"✅ [BASE_LIST_VM] Repository deleted {deletedCount} items");

            // Remove from UI
            foreach (var id in idsToDelete)
            {
                var item = Items.FirstOrDefault(i => i.Id == id);
                if (item != null)
                {
                    Items.Remove(item);
                    Debug.WriteLine($"🗑️ [BASE_LIST_VM] Removed {item.Name} from UI Items");
                }
            }

            // Complete cleanup sequence
            Debug.WriteLine($"🧹 [BASE_LIST_VM] Starting complete cleanup after delete...");

            foreach (var item in Items)
            {
                if (item.IsSelected)
                {
                    item.IsSelected = false;
                    Debug.WriteLine($"🔘 [BASE_LIST_VM] Deselected remaining item: {item.Name}");
                }
            }

            SelectedItems.Clear();
            Debug.WriteLine($"🧹 [BASE_LIST_VM] Cleared SelectedItems collection");

            IsMultiSelectMode = false;
            Debug.WriteLine($"🔘 [BASE_LIST_VM] FORCED IsMultiSelectMode = FALSE");

            UpdateCounters();
            UpdateFabForSelection();

            // ✅ SINGLE SUCCESS MESSAGE (Toast only)
            await ShowSuccessToastAsync($"{deletedCount} {EntityNamePlural.ToLower()} deleted successfully");

            Debug.WriteLine($"✅ [BASE_LIST_VM] DeleteSelected completed successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Delete selected failed: {ex.Message}");

            // Ensure cleanup even on error
            SelectedItems.Clear();
            IsMultiSelectMode = false;
            UpdateFabForSelection();

            await ShowErrorToastAsync($"Failed to delete {EntityNamePlural.ToLower()}: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ PUBLIC: Safe delete single command for external use (swipe, etc)
    /// </summary>
    public IAsyncRelayCommand<TItemViewModel> DeleteSingleItemSafeCommand => DeleteSingleItemCommand;

    public virtual void UpdateFabForSelection()
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
            FabText = $"Add {EntityName}";
            FabIsVisible = true;
        }

        Debug.WriteLine($"🔘 [BASE_LIST_VM] FAB updated: '{FabText}' - Visible: {FabIsVisible}");
    }

    #endregion

    #region Filtering and Sorting

    [RelayCommand]
    private async Task ToggleStatusFilterAsync()
    {
        var currentIndex = StatusFilterOptions.IndexOf(StatusFilter);
        var nextIndex = (currentIndex + 1) % StatusFilterOptions.Count;
        StatusFilter = StatusFilterOptions[nextIndex];

        Debug.WriteLine($"🔍 [BASE_LIST_VM] Status filter changed to: {StatusFilter}");
        await ApplyFiltersAndSortAsync();
    }

    [RelayCommand]
    private void ToggleSort()
    {
        var currentIndex = SortOptions.IndexOf(SortOrder);
        var nextIndex = (currentIndex + 1) % SortOptions.Count;
        SortOrder = SortOptions[nextIndex];

        Debug.WriteLine($"🔄 [BASE_LIST_VM] Sort order changed to: {SortOrder}");
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

                filtered = ApplyEntitySpecificSort(filtered);

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
                        ? $"No {EntityNamePlural.ToLower()} match your filters"
                        : $"No {EntityNamePlural.ToLower()} found. Tap + to add one";

                    UpdateCounters();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ [BASE_LIST_VM] Filter/sort failed: {ex.Message}");
            }
        });
    }

    protected virtual IOrderedEnumerable<TItemViewModel> ApplyEntitySpecificSort(IEnumerable<TItemViewModel> filtered)
    {
        return SortOrder switch
        {
            "Name A→Z" => filtered.OrderBy(item => item.Name),
            "Name Z→A" => filtered.OrderByDescending(item => item.Name),
            "Recent First" => filtered.OrderByDescending(item => item.UpdatedAt),
            "Oldest First" => filtered.OrderBy(item => item.CreatedAt),
            "Favorites First" => filtered.OrderByDescending(item => GetIsFavorite(item)).ThenBy(item => item.Name),
            _ => filtered.OrderBy(item => item.Name)
        };
    }

    private bool GetIsFavorite(TItemViewModel item)
    {
        try
        {
            var property = typeof(TItemViewModel).GetProperty("IsFavorite");
            if (property != null && property.PropertyType == typeof(bool))
            {
                return (bool)(property.GetValue(item) ?? false);
            }
        }
        catch
        {
            // If IsFavorite doesn't exist, return false
        }
        return false;
    }

    #endregion

    #region Counters and Status

    protected virtual void UpdateCounters()
    {
        var allEntities = Items.ToList();
        TotalCount = allEntities.Count;
        ActiveCount = allEntities.Count(e => e.IsActive);
        FavoriteCount = allEntities.Count(e => GetIsFavorite(e));

        Debug.WriteLine($"📊 [BASE_LIST_VM] Counters - Total: {TotalCount}, Active: {ActiveCount}, Favorites: {FavoriteCount}");
    }

    private async Task TestConnectionInternalAsync()
    {
        try
        {
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

    private void UpdateConnectionStatus(bool connected)
    {
        IsConnected = connected;
        ConnectionStatus = connected ? "Connected" : "Offline";
        ConnectionStatusColor = connected ? Colors.Green : Colors.Orange;

        UpdateFabForSelection();

        Debug.WriteLine($"📡 [BASE_LIST_VM] Connection status updated: {ConnectionStatus}");
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
                ToggleMultiSelect();
            }
        }
        else
        {
            await NavigateToAddAsync();
        }
    }

    #endregion

    #region Data Loading Commands

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            Debug.WriteLine($"📥 [BASE_LIST_VM] Loading {EntityNamePlural} with filter: {StatusFilter}");

            await LoadItemsDataAsync();

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
            await ShowErrorToastAsync($"Failed to load {EntityNamePlural}. Check your connection and try again.");
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
            FavoriteCount = entities.Count(e => e.IsFavorite);
            HasData = entities.Any();

            Debug.WriteLine($"📊 [BASE_LIST_VM] Stats - Total: {TotalCount}, Active: {ActiveCount}, Favorites: {FavoriteCount}, HasData: {HasData}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] LoadItemsDataAsync error: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Search and Filter Commands

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

    #region Item Selection Management

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

    [RelayCommand]
    private void SelectAll()
    {
        try
        {
            Debug.WriteLine($"✅ [BASE_LIST_VM] SelectAll called - Current IsMultiSelectMode: {IsMultiSelectMode}");
            Debug.WriteLine($"✅ [BASE_LIST_VM] Current SelectedItems count: {SelectedItems.Count}");

            if (!IsMultiSelectMode)
            {
                IsMultiSelectMode = true;
                Debug.WriteLine($"🔘 [BASE_LIST_VM] FORCED multi-select mode ON for SelectAll");
            }

            SelectedItems.Clear();
            Debug.WriteLine($"🧹 [BASE_LIST_VM] Cleared existing selections");

            foreach (var item in Items)
            {
                if (!item.IsSelected)
                {
                    item.IsSelected = true;
                    Debug.WriteLine($"🔘 [BASE_LIST_VM] Selected item: {item.Name}");
                }

                if (!SelectedItems.Contains(item))
                {
                    SelectedItems.Add(item);
                    Debug.WriteLine($"➕ [BASE_LIST_VM] Added {item.Name} to SelectedItems");
                }
            }

            UpdateFabForSelection();

            Debug.WriteLine($"✅ [BASE_LIST_VM] SelectAll completed - Selected: {SelectedItems.Count}/{Items.Count}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] SelectAll error: {ex.Message}");
            Debug.WriteLine($"❌ [BASE_LIST_VM] SelectAll stack trace: {ex.StackTrace}");
        }
    }

    [RelayCommand]
    private void DeselectAll()
    {
        try
        {
            Debug.WriteLine($"✅ [BASE_LIST_VM] DeselectAll called");
            Debug.WriteLine($"✅ [BASE_LIST_VM] Current SelectedItems count: {SelectedItems.Count}");

            foreach (var item in Items)
            {
                if (item.IsSelected)
                {
                    item.IsSelected = false;
                    Debug.WriteLine($"🔘 [BASE_LIST_VM] Deselected item: {item.Name}");
                }
            }

            SelectedItems.Clear();
            Debug.WriteLine($"🧹 [BASE_LIST_VM] Cleared SelectedItems collection");

            if (IsMultiSelectMode)
            {
                IsMultiSelectMode = false;
                Debug.WriteLine($"🔘 [BASE_LIST_VM] FORCED multi-select mode OFF after DeselectAll");
            }

            UpdateFabForSelection();

            Debug.WriteLine($"✅ [BASE_LIST_VM] DeselectAll completed - IsMultiSelectMode: {IsMultiSelectMode}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] DeselectAll error: {ex.Message}");
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

    [RelayCommand]
    private void ItemLongPress(TItemViewModel item)
    {
        if (item == null) return;

        Debug.WriteLine($"🔘 [BASE_LIST_VM] LongPress on: {item.Name}");

        if (!IsMultiSelectMode)
        {
            IsMultiSelectMode = true;
            UpdateFabForSelection();
            Debug.WriteLine($"✅ [BASE_LIST_VM] Entered multi-select mode for {EntityNamePlural}");
        }

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
                await ShowSuccessToastAsync("Connection restored! Data is now synchronized.");
                await RefreshAsync();
            }
            else
            {
                await ShowErrorToastAsync("Still offline. Check your internet connection and try again.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Connection test error: {ex.Message}");
            UpdateConnectionStatus(false);
            await ShowErrorToastAsync($"Connection test failed: {ex.Message}");
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

    #endregion

    #region ✅ UNIFIED MESSAGE SYSTEM - Toast-based, no dialogs

    /// <summary>
    /// ✅ UNIFIED: Shows confirmation dialog (DisplayAlert)
    /// </summary>
    protected virtual async Task<bool> ShowConfirmAsync(string title, string message)
    {
        try
        {
            var mainPage = GetCurrentPage();
            if (mainPage != null)
            {
                return await mainPage.DisplayAlert(title, message, "Delete", "Cancel");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Confirmation dialog error: {ex.Message}");
        }
        return false;
    }

    /// <summary>
    /// ✅ UNIFIED: Shows success message via Toast (consistent UX)
    /// </summary>
    protected virtual async Task ShowSuccessToastAsync(string message)
    {
        try
        {
            var toast = Toast.Make($"✅ {message}", ToastDuration.Short, 16);
            await toast.Show();
            Debug.WriteLine($"✅ [BASE_LIST_VM] Success toast: {message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Success toast error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ UNIFIED: Shows error message via Toast (consistent UX)
    /// </summary>
    protected virtual async Task ShowErrorToastAsync(string message)
    {
        try
        {
            var toast = Toast.Make($"❌ {message}", ToastDuration.Short, 16);
            await toast.Show();
            Debug.WriteLine($"❌ [BASE_LIST_VM] Error toast: {message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Error toast error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ Helper method to get current page
    /// </summary>
    private static Page? GetCurrentPage()
    {
        try
        {
            return Application.Current?.Windows?.FirstOrDefault()?.Page;
        }
        catch
        {
            return null;
        }
    }

    #endregion
}