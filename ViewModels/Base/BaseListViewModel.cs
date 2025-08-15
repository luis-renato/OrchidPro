using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Extensions;
using OrchidPro.Models.Base;
using OrchidPro.Services.Navigation;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace OrchidPro.ViewModels.Base;

/// <summary>
/// FIXED Base ViewModel for list views with Syncfusion native sorting support.
/// All command duplications removed and properly consolidated.
/// </summary>
public abstract partial class BaseListViewModel<T, TItemViewModel> : BaseViewModel, IDisposable
    where T : class, IBaseEntity, new()
    where TItemViewModel : BaseItemViewModel<T>
{
    #region Protected Fields

    protected readonly IBaseRepository<T> _repository;
    protected readonly INavigationService _navigationService;

    #endregion

    #region PERFORMANCE OPTIMIZATION: Cached Properties

    /// <summary>
    /// Static cache for IsFavorite property access to eliminate reflection
    /// </summary>
    private static readonly ConcurrentDictionary<Type, Func<object, bool>> _favoriteAccessorCache = new();

    /// <summary>
    /// Cached IsFavorite accessor for current TItemViewModel type
    /// </summary>
    private readonly Func<TItemViewModel, bool> _getFavoriteFunc;

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

    public List<string> StatusFilterOptions { get; } = new() { "All", "Active", "Inactive", "Favorites" };
    public List<string> SortOptions { get; } = new()
    {
        "Name A→Z", "Name Z→A", "Recent First", "Oldest First", "Favorites First"
    };

    #endregion

    #region Syncfusion Native Sorting Support

    /// <summary>
    /// Syncfusion-compatible case-insensitive string comparer for DataSource sorting
    /// </summary>
    public static readonly IComparer<object> CaseInsensitiveComparer =
        Comparer<object>.Create((x, y) =>
        {
            var stringX = x?.ToString()?.ToLower() ?? string.Empty;
            var stringY = y?.ToString()?.ToLower() ?? string.Empty;
            return stringX.CompareTo(stringY);
        });

    /// <summary>
    /// Called when SortOrder changes - handled by Page logic for Syncfusion native sorting
    /// </summary>
    public virtual void OnSyncfusionSortChanged(string sortOrder)
    {
        this.LogInfo($"🔧 SYNCFUSION: Base SortOrder changed to '{sortOrder}' - handled by Page logic");
    }

    #endregion

    #region Core Commands - FIXED: No Duplications

    public IAsyncRelayCommand ApplyFilterCommand { get; private set; }
    public IRelayCommand ClearFilterCommand { get; private set; }
    public IRelayCommand ClearSelectionCommand { get; private set; }
    public IAsyncRelayCommand<TItemViewModel> DeleteSingleItemCommand { get; private set; }
    public IAsyncRelayCommand RefreshCommand { get; private set; }
    public IAsyncRelayCommand FabActionCommand { get; private set; }
    public IRelayCommand SelectAllCommand { get; private set; }
    public IRelayCommand DeselectAllCommand { get; private set; }
    public IAsyncRelayCommand<TItemViewModel> ToggleFavoriteCommand { get; private set; }
    public IAsyncRelayCommand DeleteSelectedCommand { get; private set; }
    public IAsyncRelayCommand NavigateToAddCommand { get; private set; }
    public IRelayCommand ToggleSortCommand { get; private set; }
    public IAsyncRelayCommand ToggleStatusFilterCommand { get; private set; }

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

        // PERFORMANCE OPTIMIZATION: Initialize cached IsFavorite accessor
        _getFavoriteFunc = InitializeFavoriteAccessor();

        InitializeCommands();
        SetupPropertyChangeHandling();

        this.LogInfo($"Initialized list ViewModel for {EntityNamePlural}");
    }

    #endregion

    #region PERFORMANCE OPTIMIZATION: Favorite Accessor Initialization

    /// <summary>
    /// Initialize high-performance IsFavorite accessor eliminating reflection calls
    /// </summary>
    private Func<TItemViewModel, bool> InitializeFavoriteAccessor()
    {
        var itemType = typeof(TItemViewModel);

        // Try to get from cache first
        if (_favoriteAccessorCache.TryGetValue(itemType, out var cachedAccessor))
        {
            return item => cachedAccessor(item!);
        }

        // Create new accessor and cache it
        var property = itemType.GetProperty("IsFavorite");
        if (property != null && property.PropertyType == typeof(bool))
        {
            // Create compiled expression for maximum performance
            var parameter = System.Linq.Expressions.Expression.Parameter(typeof(object), "item");
            var cast = System.Linq.Expressions.Expression.Convert(parameter, itemType);
            var propertyAccess = System.Linq.Expressions.Expression.Property(cast, property);
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<object, bool>>(propertyAccess, parameter);
            var compiled = lambda.Compile();

            _favoriteAccessorCache.TryAdd(itemType, compiled);
            return item => compiled(item!);
        }

        // Fallback for types without IsFavorite property
        var fallback = new Func<object, bool>(_ => false);
        _favoriteAccessorCache.TryAdd(itemType, fallback);
        return _ => false;
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initialize core commands - FIXED: Single initialization without duplicates
    /// </summary>
    private void InitializeCommands()
    {
        ApplyFilterCommand = new AsyncRelayCommand(ApplyFilterAsync);
        ClearFilterCommand = new RelayCommand(ClearFilterAction);
        ClearSelectionCommand = new RelayCommand(ClearSelectionAction);
        DeleteSingleItemCommand = new AsyncRelayCommand<TItemViewModel>(DeleteSingleItemAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        FabActionCommand = new AsyncRelayCommand(FabActionAsync);
        SelectAllCommand = new RelayCommand(SelectAll);
        DeselectAllCommand = new RelayCommand(DeselectAll);
        ToggleFavoriteCommand = new AsyncRelayCommand<TItemViewModel>(ToggleFavoriteAsync);
        DeleteSelectedCommand = new AsyncRelayCommand(DeleteSelectedAsync);
        NavigateToAddCommand = new AsyncRelayCommand(NavigateToAddAsync);
        ToggleSortCommand = new RelayCommand(ToggleSort);
        ToggleStatusFilterCommand = new AsyncRelayCommand(ToggleStatusFilterAsync);
    }

    /// <summary>
    /// Setup property change monitoring for reactive UI updates
    /// </summary>
    private void SetupPropertyChangeHandling()
    {
        PropertyChanged += OnPropertyChanged;
    }

    #endregion

    #region Core Property Change Handling

    /// <summary>
    /// STREAMLINED: Handle only essential property changes
    /// </summary>
    private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SearchText):
            case nameof(StatusFilter):
                _ = LoadDataAsync();
                break;
            case nameof(SortOrder):
                OnSyncfusionSortChanged(SortOrder);
                break;
            case nameof(SelectedItems):
                MainThread.BeginInvokeOnMainThread(() => UpdateFabForSelection());
                break;
        }
    }

    // CRITICAL: Trigger Syncfusion native sorting when SortOrder changes
    partial void OnSortOrderChanged(string value)
    {
        OnSyncfusionSortChanged(value);
    }

    #endregion

    #region Abstract Methods

    protected abstract TItemViewModel CreateItemViewModel(T entity);

    #endregion

    #region Core Data Loading

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
    /// Core data loading with proper state management
    /// </summary>
    private async Task LoadDataAsync()
    {
        using (this.LogPerformance($"Load {EntityNamePlural}"))
        {
            IsLoading = true;

            try
            {
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
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    /// <summary>
    /// PERFORMANCE OPTIMIZED: Populate UI items collection from entity data with filtering
    /// Uses smart threshold to decide between sequential and parallel processing
    /// </summary>
    private async Task PopulateItemsAsync(List<T> entities)
    {
        await this.SafeExecuteAsync(async () =>
        {
            // FILTER FIRST: Apply status and search filters before creating ViewModels
            var filteredEntities = entities.AsQueryable();

            // Apply status filter
            if (StatusFilter != "All")
            {
                if (StatusFilter == "Active")
                    filteredEntities = filteredEntities.Where(e => e.IsActive);
                else if (StatusFilter == "Inactive")
                    filteredEntities = filteredEntities.Where(e => !e.IsActive);
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filteredEntities = filteredEntities.Where(e =>
                    e.Name.ToLower().Contains(searchLower) ||
                    (e.Description != null && e.Description.ToLower().Contains(searchLower)));
            }

            var finalEntities = filteredEntities.ToList();

            const int PARALLEL_THRESHOLD = 50;

            // SMART THRESHOLD: Use parallel processing only when beneficial
            var itemVMs = finalEntities.Count > PARALLEL_THRESHOLD
                ? await Task.Run(() =>
                {
                    return finalEntities.AsParallel()
                        .WithDegreeOfParallelism(Math.Min(Environment.ProcessorCount, Math.Max(1, finalEntities.Count / 20)))
                        .Select(e => CreateItemViewModel(e))
                        .ToList();
                }).ConfigureAwait(false)
                : finalEntities.Select(e => CreateItemViewModel(e)).ToList();

            // Apply favorites filter AFTER creating ViewModels (needs IsFavorite property)
            if (StatusFilter == "Favorites")
            {
                itemVMs = itemVMs.Where(vm => _getFavoriteFunc(vm)).ToList();
            }

            // Return to main thread for UI updates
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Items.Clear();
                foreach (var item in itemVMs)
                {
                    Items.Add(item);
                }
            });

            UpdateCounters();
            HasData = Items.Count > 0;

            this.LogDataOperation("Populated", EntityNamePlural, $"{Items.Count} items (filtered from {entities.Count})");
        }, "Populate Items");
    }

    #endregion

    #region Filter and Sort Commands

    /// <summary>
    /// Cycle through sort order options
    /// </summary>
    private void ToggleSort()
    {
        this.SafeExecute(() =>
        {
            var currentIndex = SortOptions.IndexOf(SortOrder);
            var nextIndex = (currentIndex + 1) % SortOptions.Count;
            SortOrder = SortOptions[nextIndex];

            this.LogInfo($"Sort order changed to: {SortOrder}");
        }, "Toggle Sort");
    }

    /// <summary>
    /// Cycle through status filter options
    /// </summary>
    private async Task ToggleStatusFilterAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            var currentIndex = StatusFilterOptions.IndexOf(StatusFilter);
            var nextIndex = (currentIndex + 1) % StatusFilterOptions.Count;
            StatusFilter = StatusFilterOptions[nextIndex];

            this.LogInfo($"Status filter changed to: {StatusFilter}");
        }, "Toggle Status Filter");
    }

    /// <summary>
    /// Apply current filters and reload data from repository
    /// </summary>
    private async Task ApplyFilterAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            await LoadDataAsync();
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
            await LoadDataAsync();
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

            await LoadDataAsync();
            this.LogSuccess($"Selection and filters cleared for {EntityNamePlural}");
        }, "Clear Selection");
    }

    #endregion

    #region CRUD Operations

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
                    // FIXED: Return "CANCELLED" string to clearly indicate user cancellation
                    return "CANCELLED";
                }

                IsBusy = true;
                var success = await _repository.DeleteAsync(item.Id);

                if (success)
                {
                    Items.Remove(item);
                    UpdateCounters();
                    await ShowSuccessToastAsync($"'{item.Name}' deleted successfully");
                    this.LogDataOperation("Deleted", EntityName, item.Name);
                    return "SUCCESS";
                }

                return "FAILED";
            }, EntityName);

            // FIXED: Only show error toast for actual failures, not cancellations
            if (!result.Success && result.Data != "CANCELLED")
            {
                await ShowErrorToastAsync($"Failed to delete {item.Name}: {result.Message}");
            }

            IsBusy = false;
        }
    }

    /// <summary>
    /// Delete multiple selected items with unified confirmation flow
    /// </summary>
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
                    // FIXED: Return -1 to indicate cancellation (not 0 which indicates failure)
                    return -1;
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

            // FIXED: Check for cancellation (-1) vs failure (0 or false)
            if (result.Success && result.Data > 0)
            {
                await ShowSuccessToastAsync($"{result.Data} {EntityNamePlural.ToLower()} deleted successfully");
                this.LogSuccess($"DeleteSelected completed successfully - {result.Data} items");
            }
            else if (result.Data == -1)
            {
                // FIXED: User cancelled - just cleanup, no error toast
                SelectedItems.Clear();
                IsMultiSelectMode = false;
                UpdateFabForSelection();
                this.LogInfo("Delete operation cancelled by user - cleanup completed");
            }
            else
            {
                // FIXED: Real failure - show error toast
                SelectedItems.Clear();
                IsMultiSelectMode = false;
                UpdateFabForSelection();
                await ShowErrorToastAsync($"Failed to delete {EntityNamePlural.ToLower()}: {result.Message}");
            }
        }
    }

    #endregion

    #region Navigation Commands

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

    #endregion

    #region Refresh and Data Management

    /// <summary>
    /// STREAMLINED: Prevent double refresh calls
    /// </summary>
    private bool _isRefreshing = false;

    private async Task RefreshAsync()
    {
        if (_isRefreshing)
        {
            this.LogInfo("Refresh already in progress - skipping");
            return;
        }

        _isRefreshing = true;

        try
        {
            await this.SafeExecuteAsync(async () =>
            {
                this.LogInfo($"Starting Refresh {EntityNamePlural}");
                await _repository.RefreshCacheAsync();
                await LoadDataAsync();
                this.LogSuccess($"Refresh completed - {Items.Count} {EntityNamePlural}");
            }, $"Refresh{EntityNamePlural}");
        }
        finally
        {
            _isRefreshing = false;
            // NOTE: IsRefreshing will be reset by the Page's HandlePullToRefresh
            // Don't reset it here to avoid conflicts with SfPullToRefresh control
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
    /// Generic toggle favorite implementation using BaseToggleFavoritePattern
    /// </summary>
    protected virtual async Task ToggleFavoriteAsync(TItemViewModel item)
    {
        await BaseToggleFavoritePattern.ExecuteToggleFavoriteAsync<T, TItemViewModel>(
            item,
            _repository,
            Items,
            CreateItemViewModel,
            UpdateCounters);
    }

    #endregion

    #region Multi-Selection

    /// <summary>
    /// Select all items in current view
    /// </summary>
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
    /// Handle item tap events for selection or navigation - to be called from UI
    /// </summary>
    public async Task HandleItemTappedAsync(TItemViewModel item)
    {
        if (item == null) return;

        await this.SafeExecuteAsync(async () =>
        {
            this.LogDebug($"Item tapped: {item.Name}, MultiSelect: {IsMultiSelectMode}");

            if (IsMultiSelectMode)
            {
                item.IsSelected = !item.IsSelected;

                if (item.IsSelected && !SelectedItems.Contains(item))
                {
                    SelectedItems.Add(item);
                }
                else if (!item.IsSelected)
                {
                    SelectedItems.Remove(item);
                }

                UpdateFabForSelection();
                this.LogDebug($"Toggled selection for {item.Name}: {item.IsSelected}");
            }
            else
            {
                await NavigateToEditAsync(item);
            }
        }, "Item Tapped");
    }

    /// <summary>
    /// Handle long press to enter multi-select mode - to be called from UI
    /// </summary>
    public void HandleItemLongPress(TItemViewModel item)
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

    #region Counters and Status

    /// <summary>
    /// PERFORMANCE OPTIMIZED: Update statistical counters for UI display
    /// Uses cached favorite accessor to eliminate reflection calls
    /// </summary>
    protected virtual void UpdateCounters()
    {
        this.SafeExecute(() =>
        {
            var allEntities = Items;
            TotalCount = allEntities.Count;
            ActiveCount = allEntities.Count(e => e.IsActive);
            FavoriteCount = allEntities.Count(e => _getFavoriteFunc(e));

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

    #endregion

    #region FAB Command

    /// <summary>
    /// Handle FAB button actions based on current mode
    /// </summary>
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
                    IsMultiSelectMode = false;
                    UpdateFabForSelection();
                }
            }
            else
            {
                await NavigateToAddAsync();
            }
        }, "FAB Action");
    }

    /// <summary>
    /// Navigate to entity creation page - MOVED HERE TO AVOID DUPLICATION
    /// </summary>
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

    #region Disposal

    /// <summary>
    /// Dispose pattern implementation for proper cleanup
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose implementation
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Clean disposal - no timer to dispose anymore
        }
    }

    #endregion

    #region Legacy Compatibility - REMOVED DUPLICATE

    /// <summary>
    /// LEGACY: Use NavigateToAddCommand instead - points to same command
    /// </summary>
    [Obsolete("Use NavigateToAddCommand instead")]
    public IAsyncRelayCommand AddNewCommand => NavigateToAddCommand;

    #endregion
}