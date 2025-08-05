using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models.Base;
using OrchidPro.Services.Navigation;
using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Base;

/// <summary>
/// Base ViewModel for list views providing unified CRUD operations, filtering, sorting, and multi-selection.
/// Implements common patterns for data management and user interactions across entity list pages.
/// </summary>
public abstract partial class BaseListViewModel<T, TItemViewModel> : BaseViewModel
    where T : class, IBaseEntity, new()
    where TItemViewModel : BaseItemViewModel<T>
{
    #region Protected Fields

    protected readonly IBaseRepository<T> _repository;
    protected readonly INavigationService _navigationService;

    #endregion

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

        InitializeCommands();
        SetupPropertyChangeHandling();

        this.LogInfo($"Initialized list ViewModel for {EntityNamePlural}");
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initialize manual relay commands for complex operations
    /// </summary>
    private void InitializeCommands()
    {
        ApplyFilterCommand = new AsyncRelayCommand(ApplyFilterAsync);
        ClearFilterCommand = new RelayCommand(ClearFilterAction);
        ClearSelectionCommand = new RelayCommand(ClearSelectionAction);
        DeleteSingleItemCommand = new AsyncRelayCommand<TItemViewModel>(DeleteSingleItemAsync);
    }

    /// <summary>
    /// Setup property change monitoring for reactive UI updates
    /// </summary>
    private void SetupPropertyChangeHandling()
    {
        PropertyChanged += OnPropertyChanged;
    }

    #endregion

    #region Manual Command Methods

    /// <summary>
    /// Apply current filters and reload data from repository
    /// </summary>
    private async Task ApplyFilterAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            await LoadItemsDataAsync();
        }, "Apply Filter");
    }

    /// <summary>
    /// Clear all filters and reload original data from repository
    /// </summary>
    private async void ClearFilterAction()
    {
        await this.SafeExecuteAsync(async () =>
        {
            SearchText = string.Empty;
            StatusFilter = "All";
            SortOrder = "Name A→Z";
            await LoadItemsDataAsync();
            this.LogSuccess($"Filters cleared and data reloaded for {EntityNamePlural}");
        }, "Clear Filter");
    }

    /// <summary>
    /// Clear selection state and reset filters
    /// </summary>
    private async void ClearSelectionAction()
    {
        await this.SafeExecuteAsync(async () =>
        {
            SelectedItems.Clear();
            foreach (var item in Items)
            {
                item.IsSelected = false;
            }

            SearchText = string.Empty;
            StatusFilter = "All";
            SortOrder = "Name A→Z";
            IsMultiSelectMode = false;
            UpdateFabForSelection();

            await LoadItemsDataAsync();
            this.LogSuccess($"Selection and filters cleared for {EntityNamePlural}");
        }, "Clear Selection");
    }

    /// <summary>
    /// Delete single item with unified confirmation and feedback flow
    /// </summary>
    private async Task DeleteSingleItemAsync(TItemViewModel? item)
    {
        if (item == null) return;

        using (this.LogPerformance($"Delete Single {EntityName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                this.LogDataOperation("Requesting deletion", EntityName, item.Name);

                var confirmed = await ShowConfirmAsync(
                    $"Delete {EntityName}",
                    $"Are you sure you want to delete '{item.Name}'?");

                if (!confirmed)
                {
                    this.LogInfo("Delete cancelled by user");
                    return false; // ✅ FIXED: No toast on cancel, just return
                }

                IsBusy = true;
                var success = await _repository.DeleteAsync(item.Id);

                if (success)
                {
                    Items.Remove(item);
                    UpdateCounters();
                    await ShowSuccessToastAsync($"'{item.Name}' deleted successfully");
                    this.LogDataOperation("Deleted", EntityName, item.Name);
                }

                return success;
            }, EntityName);

            // ✅ FIXED: Only show error toast if operation failed AND was not cancelled
            if (!result.Success && result.Data == false && !result.Message.Contains("cancelled"))
            {
                await ShowErrorToastAsync($"Failed to delete {item.Name}: {result.Message}");
            }

            IsBusy = false;
        }
    }
    #endregion

    #region Property Change Handlers

    /// <summary>
    /// Handle property changes and trigger appropriate data refresh operations
    /// </summary>
    private async void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            switch (e.PropertyName)
            {
                case nameof(SearchText):
                case nameof(StatusFilter):
                    await LoadItemsDataAsync();
                    break;
                case nameof(SortOrder):
                    await ApplyFiltersAndSortAsync();
                    break;
                case nameof(SelectedItems):
                    UpdateFabForSelection();
                    break;
            }
        }, "Property Change Handler");
    }

    #endregion

    #region Abstract Methods

    protected abstract TItemViewModel CreateItemViewModel(T entity);

    #endregion

    #region Data Loading

    /// <summary>
    /// Handle view appearing lifecycle event with data loading
    /// </summary>
    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();

        await this.SafeExecuteAsync(async () =>
        {
            if (!HasData || Items.Count == 0)
            {
                await LoadDataAsync();
            }
            else
            {
                this.LogInfo($"Refreshing data on return to {EntityNamePlural} page");
                await RefreshInternalAsync(showLoading: false);
            }
        }, "View Appearing");
    }

    /// <summary>
    /// Initial data loading with error handling and connection testing
    /// </summary>
    private async Task LoadDataAsync()
    {
        using (this.LogPerformance($"Load {EntityNamePlural}"))
        {
            IsLoading = true;

            var result = await this.SafeDataExecuteAsync(async () =>
            {
                await TestConnectionInternalAsync();
                var entities = await _repository.GetAllAsync(true);
                await PopulateItemsAsync(entities);
                return entities;
            }, EntityNamePlural);

            if (result.Success && result.Data != null)
            {
                this.LogSuccess($"Loaded {result.Data.Count} {EntityNamePlural}");
            }
            else
            {
                EmptyStateMessage = $"Failed to load {EntityNamePlural.ToLower()}: {result.Message}";
            }

            IsLoading = false;
        }
    }

    /// <summary>
    /// Populate UI items collection from entity data
    /// </summary>
    private async Task PopulateItemsAsync(List<T> entities)
    {
        await this.SafeExecuteAsync(async () =>
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

            this.LogDataOperation("Populated", EntityNamePlural, $"{Items.Count} items");
        }, "Populate Items");
    }

    #endregion

    #region Navigation Commands

    /// <summary>
    /// Navigate to entity creation page
    /// </summary>
    [RelayCommand]
    private async Task NavigateToAddAsync()
    {
        var success = await this.SafeNavigationExecuteAsync(async () =>
        {
            await _navigationService.NavigateToAsync(EditRoute);
        }, $"Add {EntityName}");

        if (!success)
        {
            await ShowErrorToastAsync($"Failed to navigate to add {EntityName.ToLower()}");
        }
    }

    /// <summary>
    /// Navigate to entity edit page with parameters
    /// </summary>
    [RelayCommand]
    private async Task NavigateToEditAsync(TItemViewModel? item)
    {
        var isValid = this.SafeValidate(() => item?.Id != null, "Edit Navigation Validation");
        if (!isValid)
        {
            this.LogWarning("NavigateToEdit: item or Id is null");
            return;
        }

        var success = await this.SafeNavigationExecuteAsync(async () =>
        {
            this.LogNavigation($"EDIT {EntityName}", $"{item!.Name} (ID: {item.Id})");

            var parameters = new Dictionary<string, object>
            {
                [$"{EntityName}Id"] = item.Id.ToString()!
            };

            await _navigationService.NavigateToAsync(EditRoute, parameters);
        }, $"Edit {EntityName}");

        if (!success)
        {
            await ShowErrorToastAsync($"Failed to navigate to edit {EntityName.ToLower()}");
        }
    }

    /// <summary>
    /// Alternative add command for different UI contexts
    /// </summary>
    [RelayCommand]
    private async Task AddNewAsync()
    {
        await NavigateToAddAsync();
    }

    #endregion

    #region Refresh and Data Management

    /// <summary>
    /// Refresh data with user-initiated loading indicator
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        using (this.LogPerformance($"Refresh {EntityNamePlural}"))
        {
            IsRefreshing = true;

            var result = await this.SafeDataExecuteAsync(async () =>
            {
                await _repository.RefreshCacheAsync();
                var entities = await _repository.GetAllAsync(true);
                await PopulateItemsAsync(entities);
                return entities;
            }, EntityNamePlural);

            if (result.Success && result.Data != null)
            {
                this.LogSuccess($"Refresh completed - {result.Data.Count} {EntityNamePlural}");
            }

            IsRefreshing = false;
        }
    }

    /// <summary>
    /// Internal refresh without user loading indicator
    /// </summary>
    private async Task RefreshInternalAsync(bool showLoading = true)
    {
        if (showLoading)
            IsRefreshing = true;

        var result = await this.SafeDataExecuteAsync(async () =>
        {
            await _repository.RefreshCacheAsync();
            var entities = await _repository.GetAllAsync(true);
            await PopulateItemsAsync(entities);
            return entities;
        }, EntityNamePlural);

        if (showLoading)
            IsRefreshing = false;
    }

    #endregion

    #region Favorites Management

    /// <summary>
    /// Toggle favorite status for an entity (virtual method for override)
    /// </summary>
    [RelayCommand]
    protected virtual async Task ToggleFavoriteAsync(TItemViewModel item)
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (item?.Id == null) return;

            this.LogInfo($"Base ToggleFavorite called for: {item.Name}");
            this.LogWarning("Override this method in specific ViewModel if entity supports favorites");

            await Task.CompletedTask;
        }, "Toggle Favorite");
    }

    #endregion

    #region Multi-Selection

    /// <summary>
    /// Toggle multi-selection mode on/off
    /// </summary>
    [RelayCommand]
    private void ToggleMultiSelect()
    {
        this.SafeExecute(() =>
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
            this.LogInfo($"Multi-select mode: {IsMultiSelectMode}");
        }, "Toggle Multi-Select");
    }

    /// <summary>
    /// Delete multiple selected items with unified confirmation flow
    /// </summary>
    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        using (this.LogPerformance("Delete Selected Items"))
        {
            var selectedCount = SelectedItems.Count;
            if (selectedCount == 0)
            {
                this.LogWarning("DeleteSelected called with 0 items");
                return;
            }

            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var confirmed = await ShowConfirmAsync(
                    "Confirm Delete",
                    $"Delete {selectedCount} selected {(selectedCount == 1 ? EntityName.ToLower() : EntityNamePlural.ToLower())}?");

                if (!confirmed)
                {
                    this.LogInfo("Delete cancelled by user");
                    return 0;
                }

                var idsToDelete = SelectedItems.Select(item => item.Id).ToList();
                this.LogDataOperation("Deleting", EntityNamePlural, $"{selectedCount} items");

                var deletedCount = await _repository.DeleteMultipleAsync(idsToDelete);

                // Remove from UI
                foreach (var id in idsToDelete)
                {
                    var item = Items.FirstOrDefault(i => i.Id == id);
                    if (item != null)
                    {
                        Items.Remove(item);
                        this.LogDataOperation("Removed from UI", EntityName, item.Name);
                    }
                }

                // Complete cleanup
                foreach (var item in Items.Where(i => i.IsSelected))
                {
                    item.IsSelected = false;
                }

                SelectedItems.Clear();
                IsMultiSelectMode = false;
                UpdateCounters();
                UpdateFabForSelection();

                return deletedCount;
            }, EntityNamePlural);

            if (result.Success && result.Data > 0)
            {
                await ShowSuccessToastAsync($"{result.Data} {EntityNamePlural.ToLower()} deleted successfully");
                this.LogSuccess($"DeleteSelected completed successfully - {result.Data} items");
            }
            else
            {
                // Ensure cleanup even on error
                SelectedItems.Clear();
                IsMultiSelectMode = false;
                UpdateFabForSelection();
                await ShowErrorToastAsync($"Failed to delete {EntityNamePlural.ToLower()}: {result.Message}");
            }
        }
    }

    /// <summary>
    /// Public command for external delete operations (swipe actions, etc.)
    /// </summary>
    public IAsyncRelayCommand<TItemViewModel> DeleteSingleItemSafeCommand => DeleteSingleItemCommand;

    /// <summary>
    /// Update FAB appearance based on selection state
    /// </summary>
    public virtual void UpdateFabForSelection()
    {
        this.SafeExecute(() =>
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

            this.LogDebug($"FAB updated: '{FabText}' - Visible: {FabIsVisible}");
        }, "Update FAB");
    }

    #endregion

    #region Filtering and Sorting

    /// <summary>
    /// Cycle through status filter options
    /// </summary>
    [RelayCommand]
    private async Task ToggleStatusFilterAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            var currentIndex = StatusFilterOptions.IndexOf(StatusFilter);
            var nextIndex = (currentIndex + 1) % StatusFilterOptions.Count;
            StatusFilter = StatusFilterOptions[nextIndex];

            this.LogInfo($"Status filter changed to: {StatusFilter}");
            await LoadItemsDataAsync();
        }, "Toggle Status Filter");
    }

    /// <summary>
    /// Cycle through sort order options
    /// </summary>
    [RelayCommand]
    private void ToggleSort()
    {
        this.SafeExecute(() =>
        {
            var currentIndex = SortOptions.IndexOf(SortOrder);
            var nextIndex = (currentIndex + 1) % SortOptions.Count;
            SortOrder = SortOptions[nextIndex];

            this.LogInfo($"Sort order changed to: {SortOrder}");
            _ = ApplyFiltersAndSortAsync();
        }, "Toggle Sort");
    }

    /// <summary>
    /// Apply sorting to current items collection
    /// </summary>
    private async Task ApplyFiltersAndSortAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            await Task.Run(() =>
            {
                var allItems = Items.ToList();
                var sorted = ApplyEntitySpecificSort(allItems.AsEnumerable());
                var result = sorted.ToList();

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
                    this.LogDebug($"Applied sort: {SortOrder} - {Items.Count} items");
                });
            });
        }, "Apply Filters and Sort");
    }

    /// <summary>
    /// Apply entity-specific sorting logic
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
    /// Get favorite status using reflection for generic compatibility
    /// </summary>
    private bool GetIsFavorite(TItemViewModel item)
    {
        return this.SafeExecute(() =>
        {
            var property = typeof(TItemViewModel).GetProperty("IsFavorite");
            if (property != null && property.PropertyType == typeof(bool))
            {
                return (bool)(property.GetValue(item) ?? false);
            }
            return false;
        }, fallbackValue: false, "Get Is Favorite");
    }

    #endregion

    #region Counters and Status

    /// <summary>
    /// Update statistical counters for UI display
    /// </summary>
    protected virtual void UpdateCounters()
    {
        this.SafeExecute(() =>
        {
            var allEntities = Items.ToList();
            TotalCount = allEntities.Count;
            ActiveCount = allEntities.Count(e => e.IsActive);
            FavoriteCount = allEntities.Count(e => GetIsFavorite(e));

            this.LogDebug($"Counters - Total: {TotalCount}, Active: {ActiveCount}, Favorites: {FavoriteCount}");
        }, "Update Counters");
    }

    /// <summary>
    /// Test connection and update status indicators
    /// </summary>
    private async Task TestConnectionInternalAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            IsConnected = true;
            ConnectionStatus = "Connected";
            ConnectionStatusColor = Colors.Green;
        }, "Test Connection");
    }

    /// <summary>
    /// Update connection status UI indicators
    /// </summary>
    private void UpdateConnectionStatus(bool connected)
    {
        this.SafeExecute(() =>
        {
            IsConnected = connected;
            ConnectionStatus = connected ? "Connected" : "Offline";
            ConnectionStatusColor = connected ? Colors.Green : Colors.Orange;
            UpdateFabForSelection();

            this.LogConnectivity(ConnectionStatus);
        }, "Update Connection Status");
    }

    #endregion

    #region FAB Command

    /// <summary>
    /// Handle FAB button actions based on current mode
    /// </summary>
    [RelayCommand]
    private async Task FabActionAsync()
    {
        await this.SafeExecuteAsync(async () =>
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
        }, "FAB Action");
    }

    #endregion

    #region Data Loading Commands

    /// <summary>
    /// Load items with current filter settings
    /// </summary>
    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        if (IsLoading) return;

        using (this.LogPerformance($"Load {EntityNamePlural}"))
        {
            IsLoading = true;

            var result = await this.SafeDataExecuteAsync(async () =>
            {
                await LoadItemsDataAsync();
                await TestConnectionInBackgroundAsync();
                return Items.Count;
            }, EntityNamePlural);

            if (result.Success)
            {
                this.LogSuccess($"Loaded {result.Data} {EntityNamePlural}");
            }
            else
            {
                await ShowErrorToastAsync($"Failed to load {EntityNamePlural}. Check your connection and try again.");
                UpdateConnectionStatus(false);
            }

            IsLoading = false;
        }
    }

    /// <summary>
    /// Core data loading method with repository filtering
    /// </summary>
    private async Task LoadItemsDataAsync()
    {
        await this.SafeDataExecuteAsync(async () =>
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

            this.LogDataOperation("Loaded with filters", EntityNamePlural,
                $"Total: {TotalCount}, Active: {ActiveCount}, Favorites: {FavoriteCount}");

            return itemViewModels;
        }, EntityNamePlural);
    }

    #endregion

    #region Search and Filter Commands

    /// <summary>
    /// Execute search with current search text
    /// </summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadItemsDataAsync();
    }

    /// <summary>
    /// Clear search text and reload data
    /// </summary>
    [RelayCommand]
    private async Task ClearSearchAsync()
    {
        SearchText = string.Empty;
        await LoadItemsDataAsync();
    }

    /// <summary>
    /// Apply status filter to data
    /// </summary>
    [RelayCommand]
    private async Task FilterByStatusAsync()
    {
        await LoadItemsDataAsync();
    }

    #endregion

    #region Item Selection Management

    /// <summary>
    /// Handle individual item selection changes
    /// </summary>
    private void OnItemSelectionChanged(BaseItemViewModel<T> item)
    {
        this.SafeExecute(() =>
        {
            if (item == null)
            {
                this.LogWarning($"OnItemSelectionChanged called with null {EntityName}");
                return;
            }

            this.LogDebug($"Selection changed: {item.Name}, IsSelected: {item.IsSelected}");

            var typedItem = Items.FirstOrDefault(i => i.Id == item.Id);
            if (typedItem == null) return;

            if (item.IsSelected)
            {
                if (!SelectedItems.Contains(typedItem))
                {
                    SelectedItems.Add(typedItem);
                    this.LogDebug($"Added {item.Name} to selection. Total: {SelectedItems.Count}");
                }

                if (!IsMultiSelectMode)
                {
                    EnterMultiSelectMode();
                }
            }
            else
            {
                SelectedItems.Remove(typedItem);
                this.LogDebug($"Removed {item.Name} from selection. Total: {SelectedItems.Count}");

                if (!SelectedItems.Any() && IsMultiSelectMode)
                {
                    ExitMultiSelectMode();
                }
            }

            UpdateFabForSelection();
        }, "Item Selection Changed");
    }

    /// <summary>
    /// Enter multi-selection mode
    /// </summary>
    private void EnterMultiSelectMode()
    {
        this.SafeExecute(() =>
        {
            IsMultiSelectMode = true;
            UpdateFabForSelection();
            this.LogInfo($"Entered multi-select mode for {EntityNamePlural}");
        }, "Enter Multi-Select Mode");
    }

    /// <summary>
    /// Exit multi-selection mode and clear selections
    /// </summary>
    private void ExitMultiSelectMode()
    {
        this.SafeExecute(() =>
        {
            IsMultiSelectMode = false;
            DeselectAll();
            UpdateFabForSelection();
            this.LogInfo($"Exited multi-select mode for {EntityNamePlural}");
        }, "Exit Multi-Select Mode");
    }

    /// <summary>
    /// Select all items in current view
    /// </summary>
    [RelayCommand]
    private void SelectAll()
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"SelectAll called - Current IsMultiSelectMode: {IsMultiSelectMode}");

            if (!IsMultiSelectMode)
            {
                IsMultiSelectMode = true;
                this.LogInfo("Enabled multi-select mode for SelectAll");
            }

            SelectedItems.Clear();

            foreach (var item in Items)
            {
                if (!item.IsSelected)
                {
                    item.IsSelected = true;
                }

                if (!SelectedItems.Contains(item))
                {
                    SelectedItems.Add(item);
                }
            }

            UpdateFabForSelection();
            this.LogSuccess($"SelectAll completed - Selected: {SelectedItems.Count}/{Items.Count}");
        }, "Select All");
    }

    /// <summary>
    /// Deselect all items and exit multi-select mode
    /// </summary>
    [RelayCommand]
    private void DeselectAll()
    {
        this.SafeExecute(() =>
        {
            foreach (var item in Items.Where(i => i.IsSelected))
            {
                item.IsSelected = false;
            }

            SelectedItems.Clear();

            if (IsMultiSelectMode)
            {
                IsMultiSelectMode = false;
                this.LogInfo("Disabled multi-select mode after DeselectAll");
            }

            UpdateFabForSelection();
            this.LogSuccess($"DeselectAll completed - IsMultiSelectMode: {IsMultiSelectMode}");
        }, "Deselect All");
    }

    /// <summary>
    /// Handle item tap events for selection or navigation
    /// </summary>
    [RelayCommand]
    private async Task ItemTappedAsync(TItemViewModel item)
    {
        if (item == null) return;

        await this.SafeExecuteAsync(async () =>
        {
            this.LogDebug($"Item tapped: {item.Name}, MultiSelect: {IsMultiSelectMode}");

            if (IsMultiSelectMode)
            {
                item.IsSelected = !item.IsSelected;
                this.LogDebug($"Toggled selection for {item.Name}: {item.IsSelected}");
            }
            else
            {
                await NavigateToEditAsync(item);
            }
        }, "Item Tapped");
    }

    /// <summary>
    /// Handle long press to enter multi-select mode
    /// </summary>
    [RelayCommand]
    private void ItemLongPress(TItemViewModel item)
    {
        if (item == null) return;

        this.SafeExecute(() =>
        {
            this.LogDebug($"LongPress on: {item.Name}");

            if (!IsMultiSelectMode)
            {
                IsMultiSelectMode = true;
                UpdateFabForSelection();
                this.LogInfo($"Entered multi-select mode for {EntityNamePlural}");
            }

            if (!item.IsSelected)
            {
                item.IsSelected = true;
                if (!SelectedItems.Contains(item))
                {
                    SelectedItems.Add(item);
                }
            }

            this.LogSuccess($"Multi-select activated and item selected: {item.Name}. Total selected: {SelectedItems.Count}");
        }, "Item Long Press");
    }

    #endregion

    #region Connectivity

    /// <summary>
    /// Test network connectivity and refresh data if connected
    /// </summary>
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        using (this.LogPerformance("Connection Test"))
        {
            var result = await this.SafeNetworkExecuteAsync(async () =>
            {
                return await _repository.TestConnectionAsync();
            }, "Connection Test");

            var isConnected = result;
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
    }

    /// <summary>
    /// Background connectivity test without user feedback
    /// </summary>
    private async Task TestConnectionInBackgroundAsync()
    {
        var isConnected = await this.SafeNetworkExecuteAsync(async () =>
        {
            return await _repository.TestConnectionAsync();
        }, "Background Connection Test");
        UpdateConnectionStatus(isConnected);
        this.LogConnectivity($"Background test result: {(isConnected ? "Connected" : "Offline")}");
    }

    #endregion

    #region User Interface Methods

    /// <summary>
    /// Show confirmation dialog for destructive actions
    /// </summary>
    protected virtual async Task<bool> ShowConfirmAsync(string title, string message)
    {
        var result = await this.SafeExecuteAsync(async () =>
        {
            var mainPage = GetCurrentPage();
            if (mainPage != null)
            {
                return await mainPage.DisplayAlert(title, message, "Delete", "Cancel");
            }
            return false;
        }, fallbackValue: false, "Show Confirmation");

        return result;
    }

    /// <summary>
    /// Show success message using standardized toast extensions
    /// </summary>
    protected virtual async Task ShowSuccessToastAsync(string message)
    {
        await this.ShowSuccessToast(message);
    }

    /// <summary>
    /// Show error message using standardized toast extensions
    /// </summary>
    protected virtual async Task ShowErrorToastAsync(string message)
    {
        await this.ShowErrorToast(message);
    }

    /// <summary>
    /// Get current page from application window hierarchy
    /// </summary>
    private static Page? GetCurrentPage()
    {
        return new object().SafeExecute(() =>
        {
            return Application.Current?.Windows?.FirstOrDefault()?.Page;
        }, fallbackValue: null, "Get Current Page");
    }

    #endregion
}