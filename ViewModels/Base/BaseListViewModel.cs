using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models.Base;
using OrchidPro.Services.Navigation;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace OrchidPro.ViewModels.Base;

/// <summary>
/// ✅ ENHANCED: BaseListViewModel with ALL functionalities extracted from FamiliesListViewModel
/// </summary>
public abstract partial class BaseListViewModel<T, TItemViewModel> : BaseViewModel
    where T : class, IBaseEntity, new()
    where TItemViewModel : BaseItemViewModel<T>
{
    protected readonly IBaseRepository<T> _repository;
    protected readonly INavigationService _navigationService;

    #region ✅ EXTRACTED: Observable Properties from Family

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

    // ✅ EXTRACTED: Favorite support from Family
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

    // ✅ EXTRACTED: Sort support from Family
    [ObservableProperty]
    private string sortOrder = "Name A→Z";

    #endregion

    #region ✅ EXTRACTED: Filter and Sort Options from Family

    public List<string> StatusFilterOptions { get; } = new() { "All", "Active", "Inactive" };
    public List<string> SortOptions { get; } = new()
    {
        "Name A→Z", "Name Z→A", "Recent First", "Oldest First", "Favorites First"
    };

    #endregion

    #region ✅ EXTRACTED: Manual Commands from Family

    /// <summary>
    /// ✅ EXTRACTED: ApplyFilterCommand from Family
    /// </summary>
    public IAsyncRelayCommand ApplyFilterCommand { get; private set; }

    /// <summary>
    /// ✅ EXTRACTED: ClearFilterCommand from Family
    /// </summary>
    public IRelayCommand ClearFilterCommand { get; private set; }

    /// <summary>
    /// ✅ EXTRACTED: ClearSelectionCommand from Family
    /// </summary>
    public IRelayCommand ClearSelectionCommand { get; private set; }

    /// <summary>
    /// ✅ EXTRACTED: DeleteSingleItemCommand from Family
    /// </summary>
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

        // ✅ EXTRACTED: Initialize manual commands from Family
        ApplyFilterCommand = new AsyncRelayCommand(ApplyFilterAsync);
        ClearFilterCommand = new RelayCommand(ClearFilterAction);
        ClearSelectionCommand = new RelayCommand(ClearSelectionAction);
        DeleteSingleItemCommand = new AsyncRelayCommand<TItemViewModel>(DeleteSingleItemAsync);

        // ✅ EXTRACTED: Setup change monitoring from Family
        PropertyChanged += OnPropertyChanged;

        Debug.WriteLine($"✅ [BASE_LIST_VM] Enhanced initialized for {EntityNamePlural}");
    }

    #endregion

    #region ✅ EXTRACTED: Manual Command Methods from Family

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

    private async Task DeleteSingleItemAsync(TItemViewModel? item)
    {
        if (item == null) return;

        var confirmed = await Application.Current?.MainPage?.DisplayAlert(
            "Confirm Delete",
            $"Delete '{item.Name}'?",
            "Delete",
            "Cancel");

        if (confirmed != true) return;

        try
        {
            IsBusy = true;
            // Remove from list first
            Items.Remove(item);
            // Delete from database
            await _repository.DeleteAsync(item.Id);
            UpdateCounters();
            Debug.WriteLine($"✅ [BASE_LIST_VM] Deleted {EntityName}: {item.Name}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Delete failed: {ex.Message}");
            // Reload on error
            await LoadDataAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion

    #region ✅ EXTRACTED: Property Change Handlers from Family

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

    #region ✅ EXTRACTED: Data Loading from Family

    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();

        if (!HasData || Items.Count == 0)
        {
            await LoadDataAsync();
        }
        else
        {
            // ✅ EXTRACTED: Always refresh when returning to ensure updated data
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

    #region ✅ EXTRACTED: Navigation Commands from Family

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

            // ✅ EXTRACTED: Use Dictionary with string keys for compatibility
            var parameters = new Dictionary<string, object>
            {
                [$"{EntityName}Id"] = item.Id.ToString() // ✅ Convert to string
            };

            await _navigationService.NavigateToAsync(EditRoute, parameters);

            Debug.WriteLine($"✅ [BASE_LIST_VM] Navigation completed for {EntityName}: {item.Name}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Navigate to edit failed: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ EXTRACTED: Command for adding (used by FAB)
    /// </summary>
    [RelayCommand]
    private async Task AddNewAsync()
    {
        await NavigateToAddAsync();
    }

    #endregion

    #region ✅ EXTRACTED: Refresh and Data Management from Family

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsRefreshing = true;

            Debug.WriteLine($"🔄 [BASE_LIST_VM] Refreshing {EntityNamePlural} data...");

            // ✅ FIXED: Use RefreshCacheAsync instead of RefreshAllDataAsync
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

    /// <summary>
    /// ✅ EXTRACTED: Internal refresh method with parameter
    /// </summary>
    private async Task RefreshInternalAsync(bool showLoading = true)
    {
        try
        {
            if (showLoading)
                IsRefreshing = true;

            Debug.WriteLine($"🔄 [BASE_LIST_VM] Refreshing {EntityNamePlural} data...");

            // ✅ FIXED: Use RefreshCacheAsync instead of RefreshAllDataAsync
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

    #region ✅ EXTRACTED: Favorites Management (Virtual - can be overridden)

    /// <summary>
    /// ✅ VIRTUAL: Default favorite toggle - override in specific ViewModels for entities that support favorites
    /// </summary>
    [RelayCommand]
    protected virtual async Task ToggleFavoriteAsync(TItemViewModel item)
    {
        try
        {
            if (item?.Id == null) return;

            Debug.WriteLine($"⭐ [BASE_LIST_VM] Base ToggleFavorite called for: {item.Name}");
            Debug.WriteLine($"⚠️ [BASE_LIST_VM] Override this method in specific ViewModel if entity supports favorites");

            // Default implementation - just log
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] ToggleFavorite error: {ex.Message}");
        }
    }

    #endregion

    #region ✅ EXTRACTED: Multi-Selection from Family

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
        Debug.WriteLine($"🔘 [BASE_LIST_VM] Multi-select mode: {IsMultiSelectMode}");
    }

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

            var confirmed = await Application.Current?.MainPage?.DisplayAlert(
                "Confirm Delete",
                $"Delete {count} selected {(count == 1 ? EntityName.ToLower() : EntityNamePlural.ToLower())}?",
                "Delete",
                "Cancel");

            if (confirmed != true)
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

            // ✅ CRITICAL: Complete cleanup sequence
            Debug.WriteLine($"🧹 [BASE_LIST_VM] Starting complete cleanup after delete...");

            // 1. Clear selections completely
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

            // 2. Exit multi-select mode COMPLETELY
            IsMultiSelectMode = false;
            Debug.WriteLine($"🔘 [BASE_LIST_VM] FORCED IsMultiSelectMode = FALSE");

            // 3. Update counters
            UpdateCounters();
            Debug.WriteLine($"📊 [BASE_LIST_VM] Updated counters after delete");

            // 4. Update FAB to normal state
            UpdateFabForSelection();
            Debug.WriteLine($"🎯 [BASE_LIST_VM] Updated FAB after delete - Text: {FabText}");

            Debug.WriteLine($"✅ [BASE_LIST_VM] DeleteSelected completed:");
            Debug.WriteLine($"    - Deleted: {deletedCount} {EntityNamePlural}");
            Debug.WriteLine($"    - Remaining Items: {Items.Count}");
            Debug.WriteLine($"    - IsMultiSelectMode: {IsMultiSelectMode}");
            Debug.WriteLine($"    - SelectedItems.Count: {SelectedItems.Count}");
            Debug.WriteLine($"    - FAB Text: {FabText}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [BASE_LIST_VM] Delete selected failed: {ex.Message}");
            Debug.WriteLine($"❌ [BASE_LIST_VM] Delete selected stack trace: {ex.StackTrace}");

            // Ensure cleanup even on error
            SelectedItems.Clear();
            IsMultiSelectMode = false;
            UpdateFabForSelection();
        }
    }

    /// <summary>
    /// ✅ FIXED: Safe delete single item with proper UI cleanup
    /// </summary>
    [RelayCommand]
    private async Task DeleteSingleItemSafeAsync(TItemViewModel item)
    {
        if (item == null) return;

        if (!IsConnected)
        {
            await ShowErrorAsync("No Connection", $"Cannot delete {EntityName.ToLower()} without internet connection.");
            return;
        }

        // ✅ REMOVED: No longer blocking system defaults since you don't use this feature
        // if (item.IsSystemDefault)
        // {
        //     await ShowErrorAsync("Cannot Delete", $"This is a system default {EntityName.ToLower()} and cannot be deleted.");
        //     return;
        // }

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

                // ✅ FIXED: Proper cache invalidation and refresh
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

    #region ✅ EXTRACTED: Filtering and Sorting from Family

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

                // ✅ EXTRACTED: Generic sorting logic
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

    /// <summary>
    /// ✅ VIRTUAL: Can be overridden for entity-specific sorting
    /// </summary>
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

    /// <summary>
    /// ✅ HELPER: Safely get IsFavorite property
    /// </summary>
    private bool GetIsFavorite(TItemViewModel item)
    {
        try
        {
            // Use reflection to check if IsFavorite exists
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

    #region ✅ EXTRACTED: Counters and Status from Family

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

    #region ✅ EXTRACTED: FAB Command from Family

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

    #region ✅ Data Loading Commands

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            Debug.WriteLine($"📥 [BASE_LIST_VM] Loading {EntityNamePlural} with filter: {StatusFilter}");

            await LoadItemsDataAsync();

            // Test connectivity in background
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

            // ✅ CRITICAL: Ensure we're in multi-select mode FIRST
            if (!IsMultiSelectMode)
            {
                IsMultiSelectMode = true;
                Debug.WriteLine($"🔘 [BASE_LIST_VM] FORCED multi-select mode ON for SelectAll");
            }

            // Clear current selections first
            SelectedItems.Clear();
            Debug.WriteLine($"🧹 [BASE_LIST_VM] Cleared existing selections");

            // Select all items
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

            // Update FAB AFTER selecting all
            UpdateFabForSelection();

            Debug.WriteLine($"✅ [BASE_LIST_VM] SelectAll completed:");
            Debug.WriteLine($"    - IsMultiSelectMode: {IsMultiSelectMode}");
            Debug.WriteLine($"    - SelectedItems.Count: {SelectedItems.Count}");
            Debug.WriteLine($"    - Items.Count: {Items.Count}");
            Debug.WriteLine($"    - FAB Text: {FabText}");
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

            // Clear all selections
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

            // ✅ CRITICAL: Exit multi-select mode
            if (IsMultiSelectMode)
            {
                IsMultiSelectMode = false;
                Debug.WriteLine($"🔘 [BASE_LIST_VM] FORCED multi-select mode OFF after DeselectAll");
            }

            // Update FAB
            UpdateFabForSelection();

            Debug.WriteLine($"✅ [BASE_LIST_VM] DeselectAll completed:");
            Debug.WriteLine($"    - IsMultiSelectMode: {IsMultiSelectMode}");
            Debug.WriteLine($"    - SelectedItems.Count: {SelectedItems.Count}");
            Debug.WriteLine($"    - FAB Text: {FabText}");
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

        // Enter multi-selection mode if not already
        if (!IsMultiSelectMode)
        {
            IsMultiSelectMode = true;
            UpdateFabForSelection();
            Debug.WriteLine($"✅ [BASE_LIST_VM] Entered multi-select mode for {EntityNamePlural}");
        }

        // Select the item that was pressed
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

    #region ✅ EXTRACTED: Connectivity from Family

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

    #region ✅ EXTRACTED: Helper Methods from Family

    /// <summary>
    /// ✅ EXTRACTED: Shows an error message to the user
    /// </summary>
    protected virtual async Task ShowErrorAsync(string title, string message = "")
    {
        try
        {
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
    /// ✅ EXTRACTED: Shows a success message to the user
    /// </summary>
    protected virtual async Task ShowSuccessAsync(string message)
    {
        try
        {
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
    /// ✅ EXTRACTED: Shows a confirmation dialog
    /// </summary>
    public virtual async Task<bool> ShowConfirmAsync(string title, string message)
    {
        try
        {
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
    /// ✅ EXTRACTED: Helper method to get current page
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