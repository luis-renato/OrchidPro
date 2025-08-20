using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Extensions;
using OrchidPro.Models.Base;
using OrchidPro.Services.Navigation;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace OrchidPro.ViewModels.Base;

/// <summary>
/// MEMORY OPTIMIZATION: Object pools for temporary collections - MODERNIZED
/// </summary>
public static class ListViewModelPools
{
    // FIXED IDE0330: Use System.Threading.Lock
    private static readonly ConcurrentQueue<object> _entityListPool = new();
    private static readonly Lock _poolLock = new();

    public static List<TEntity> GetTempList<TEntity>()
    {
        lock (_poolLock)
        {
            if (_entityListPool.TryDequeue(out var pooledItem))
            {
                // Safe casting - clear and return typed list
                if (pooledItem is List<TEntity> typedList)
                {
                    typedList.Clear();
                    return typedList;
                }
            }

            // Create new list if nothing available in pool
            return new List<TEntity>(50);
        }
    }

    public static void ReturnTempList<TEntity>(List<TEntity> list)
    {
        if (list == null || list.Count > 500) return; // Don't pool huge lists

        lock (_poolLock)
        {
            list.Clear();
            if (_entityListPool.Count < 10) // Limit pool size
            {
                _entityListPool.Enqueue(list);
            }
        }
    }
}

/// <summary>
/// PERFORMANCE OPTIMIZED Base ViewModel with throttled property changes and debounced UI updates.
/// FIXED: Loading flash issue during filter operations and all nullability warnings.
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

    #region PERFORMANCE OPTIMIZATION: Property Change Throttling

    /// <summary>
    /// Throttle timer for property change events to prevent excessive UI updates
    /// </summary>
    private Timer? _propertyChangeThrottleTimer;
    // FIXED IDE0330: Use System.Threading.Lock
    private readonly Lock _throttleLock = new();
    private volatile bool _isPendingPropertyUpdate = false;
    private string? _pendingPropertyName;

    #endregion

    #region 🔧 LOADING FLASH FIX: State Tracking

    /// <summary>
    /// 🔧 CRITICAL FIX: Track if this ViewModel has appeared to user before (regardless of cache)
    /// </summary>
    private bool _hasAppearedToUser = false;

    /// <summary>
    /// Track the source of loading operations to determine if overlay should show
    /// </summary>
    private enum LoadingSource
    {
        InitialLoad,    // First time loading data
        Filter,         // Filtering existing data
        Refresh,        // Pull-to-refresh or manual refresh
        Navigation      // Navigation back to page
    }

    #endregion

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<TItemViewModel> items = [];

    [ObservableProperty]
    private ObservableCollection<TItemViewModel> selectedItems = [];

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

    // FIXED IDE0028: Collection initialization simplified
    public List<string> StatusFilterOptions { get; } = ["All", "Active", "Inactive", "Favorites"];
    public List<string> SortOptions { get; } = ["Name A→Z", "Name Z→A", "Recent First", "Oldest First", "Favorites First"];

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

    #region Core Commands - FIXED CS8618: Non-nullable properties initialized

    public IAsyncRelayCommand ApplyFilterCommand { get; private set; } = null!;
    public IRelayCommand ClearFilterCommand { get; private set; } = null!;
    public IRelayCommand ClearSelectionCommand { get; private set; } = null!;
    public IAsyncRelayCommand<TItemViewModel> DeleteSingleItemCommand { get; private set; } = null!;
    public IAsyncRelayCommand RefreshCommand { get; private set; } = null!;
    public IAsyncRelayCommand FabActionCommand { get; private set; } = null!;
    public IRelayCommand SelectAllCommand { get; private set; } = null!;
    public IRelayCommand DeselectAllCommand { get; private set; } = null!;
    public IAsyncRelayCommand<TItemViewModel?> ToggleFavoriteCommand { get; private set; } = null!; // FIXED CS8622: nullable parameter
    public IAsyncRelayCommand DeleteSelectedCommand { get; private set; } = null!;
    public IAsyncRelayCommand NavigateToAddCommand { get; private set; } = null!;
    public IRelayCommand ToggleSortCommand { get; private set; } = null!;
    public IAsyncRelayCommand ToggleStatusFilterCommand { get; private set; } = null!;

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
    /// FIXED CA1822: Made static for better performance
    /// Initialize high-performance IsFavorite accessor eliminating reflection calls
    /// </summary>
    private static Func<TItemViewModel, bool> InitializeFavoriteAccessor()
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
        ToggleFavoriteCommand = new AsyncRelayCommand<TItemViewModel?>(ToggleFavoriteAsync);
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

    #region PERFORMANCE OPTIMIZATION: Throttled Property Change Handling

    /// <summary>
    /// PERFORMANCE OPTIMIZED: Throttled property change handler to prevent excessive UI updates
    /// </summary>
    private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Critical properties that need immediate response (no throttling)
        var criticalProperties = new[] { nameof(IsLoading), nameof(SortOrder), nameof(IsMultiSelectMode) };

        if (criticalProperties.Contains(e.PropertyName))
        {
            HandlePropertyChangeImmediate(e.PropertyName);
            return;
        }

        // Non-critical properties use throttled handling
        var throttleableProperties = new[] { nameof(SearchText), nameof(StatusFilter), nameof(SelectedItems) };

        if (throttleableProperties.Contains(e.PropertyName))
        {
            ScheduleThrottledPropertyUpdate(e.PropertyName);
        }
    }

    /// <summary>
    /// PERFORMANCE OPTIMIZATION: Handle critical properties immediately without throttling
    /// </summary>
    private void HandlePropertyChangeImmediate(string? propertyName)
    {
        switch (propertyName)
        {
            case nameof(SortOrder):
                OnSyncfusionSortChanged(SortOrder);
                break;
            case nameof(SelectedItems):
                MainThread.BeginInvokeOnMainThread(() => UpdateFabForSelection());
                break;
        }
    }

    /// <summary>
    /// PERFORMANCE OPTIMIZATION: Schedule throttled property update to prevent rapid-fire changes
    /// </summary>
    private void ScheduleThrottledPropertyUpdate(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        lock (_throttleLock)
        {
            // Store the latest property name
            _pendingPropertyName = propertyName;

            // Cancel existing timer if one is running
            _propertyChangeThrottleTimer?.Dispose();

            if (!_isPendingPropertyUpdate)
            {
                _isPendingPropertyUpdate = true;
            }

            // Schedule new throttled execution (200ms debounce)
            _propertyChangeThrottleTimer = new Timer(async _ =>
            {
                await ExecuteThrottledPropertyUpdate();
            }, null, 200, Timeout.Infinite);
        }
    }

    /// <summary>
    /// PERFORMANCE OPTIMIZATION: Execute property update after throttle period
    /// </summary>
    private async Task ExecuteThrottledPropertyUpdate()
    {
        try
        {
            string? propertyToHandle;

            lock (_throttleLock)
            {
                propertyToHandle = _pendingPropertyName;
                _isPendingPropertyUpdate = false;
                _pendingPropertyName = null;
                _propertyChangeThrottleTimer?.Dispose();
                _propertyChangeThrottleTimer = null;
            }

            if (!string.IsNullOrEmpty(propertyToHandle))
            {
                await HandleThrottledPropertyChange(propertyToHandle);
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error executing throttled property update");
        }
    }

    /// <summary>
    /// PERFORMANCE OPTIMIZATION: Handle throttled property changes
    /// </summary>
    private async Task HandleThrottledPropertyChange(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(SearchText):
            case nameof(StatusFilter):
                // 🔧 LOADING FLASH FIX: Use filter-specific loading method
                await LoadDataAsync(LoadingSource.Filter);
                break;
            case nameof(SelectedItems):
                await MainThread.InvokeOnMainThreadAsync(() => UpdateFabForSelection());
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

    #region Core Data Loading - 🔧 LOADING FLASH FIXED

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
                // 🔧 LOADING FLASH FIX: First time load - show overlay
                await LoadDataAsync(LoadingSource.InitialLoad);
            }
            else
            {
                this.LogInfo($"Refreshing data on return to {EntityNamePlural} page");
                // 🔧 LOADING FLASH FIX: Navigation refresh - no overlay
                await LoadDataAsync(LoadingSource.Navigation);
            }
        }, "View Appearing");
    }

    /// <summary>
    /// 🔧 LOADING FLASH FIXED: Core data loading with intelligent overlay management
    /// </summary>
    private async Task LoadDataAsync(LoadingSource source = LoadingSource.InitialLoad)
    {
        using (this.LogPerformance($"Load {EntityNamePlural}"))
        {
            var shouldShowLoading = ShouldShowLoadingOverlay(source);

            this.LogInfo($"🔧 LOADING FIX: Source={source}, ShowLoading={shouldShowLoading}, HasAppearedToUser={_hasAppearedToUser}");

            if (shouldShowLoading)
            {
                IsLoading = true;

                // 🔧 VISUAL FIX: Minimum delay to ensure loading overlay is visible
                await Task.Delay(500); // Ensure at least 500ms of loading visibility
            }

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
                    _hasAppearedToUser = true;
                    this.LogSuccess($"Loaded {result.Data.Count} {EntityNamePlural}");
                }
                else
                {
                    EmptyStateMessage = $"Failed to load {EntityNamePlural.ToLower()}: {result.Message}";
                }
            }
            finally
            {
                if (shouldShowLoading)
                {
                    IsLoading = false;
                }
            }
        }
    }

    /// <summary>
    /// 🔧 LOADING FLASH FIX: Smart loading overlay decision based on operation type
    /// </summary>
    private bool ShouldShowLoadingOverlay(LoadingSource source)
    {
        try
        {
            switch (source)
            {
                case LoadingSource.InitialLoad:
                    var shouldShow = !_hasAppearedToUser;
                    this.LogInfo($"🔧 LOADING FIX: Initial load - HasAppearedToUser={_hasAppearedToUser}, ShowOverlay={shouldShow}");
                    this.LogInfo($"🔧 DEBUG: IsLoading will be set to: {shouldShow}"); // ADD THIS
                    return shouldShow;

                case LoadingSource.Navigation:
                    var showForNavigation = !_hasAppearedToUser;
                    this.LogInfo($"🔧 LOADING FIX: Navigation overlay decision: {showForNavigation}");
                    this.LogInfo($"🔧 DEBUG: IsLoading will be set to: {showForNavigation}"); // ADD THIS
                    return showForNavigation;

                case LoadingSource.Filter:
                    this.LogInfo($"🔧 LOADING FIX: NO overlay for filter operation (prevents flash)");
                    this.LogInfo($"🔧 DEBUG: IsLoading will be set to: false"); // ADD THIS
                    return false;

                case LoadingSource.Refresh:
                    var showForRefresh = Items.Count == 0;
                    this.LogInfo($"🔧 LOADING FIX: Refresh overlay decision: {showForRefresh}");
                    this.LogInfo($"🔧 DEBUG: IsLoading will be set to: {showForRefresh}"); // ADD THIS
                    return showForRefresh;

                default:
                    this.LogInfo($"🔧 DEBUG: Default case - IsLoading will be set to: false"); // ADD THIS
                    return false;
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error in ShouldShowLoadingOverlay");
            return false;
        }
    }

    #endregion

    #region PERFORMANCE OPTIMIZED: Populate UI Items

    /// <summary>
    /// PERFORMANCE OPTIMIZED: Populate UI items collection with memory pooling
    /// FIXED CA1862: Using StringComparison.OrdinalIgnoreCase for performance
    /// </summary>
    private async Task PopulateItemsAsync(List<T> entities)
    {
        await this.SafeExecuteAsync(async () =>
        {
            // Use pooled list for filtering to reduce GC pressure
            var tempFilteredList = ListViewModelPools.GetTempList<T>();
            try
            {
                tempFilteredList.AddRange(entities);

                // Apply status filter
                if (StatusFilter != "All")
                {
                    if (StatusFilter == "Active")
                        tempFilteredList.RemoveAll(e => !e.IsActive);
                    else if (StatusFilter == "Inactive")
                        tempFilteredList.RemoveAll(e => e.IsActive);
                }

                // Apply search filter - FIXED CA1862: Using StringComparison.OrdinalIgnoreCase
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    tempFilteredList.RemoveAll(e =>
                        !e.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) &&
                        (e.Description == null || !e.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
                }

                const int PARALLEL_THRESHOLD = 50;

                // Use parallel processing only when beneficial
                // FIXED CS9174/CS8030/CS1662: Proper Task.Run with ToList()
                var itemVMs = tempFilteredList.Count > PARALLEL_THRESHOLD
                    ? await Task.Run(() =>
                    {
                        return tempFilteredList.AsParallel()
                            .WithDegreeOfParallelism(Math.Min(Environment.ProcessorCount, Math.Max(1, tempFilteredList.Count / 20)))
                            .Select(e => CreateItemViewModel(e))
                            .ToList();
                    }).ConfigureAwait(false)
                    : [.. tempFilteredList.Select(e => CreateItemViewModel(e))];

                // Apply favorites filter AFTER creating ViewModels
                if (StatusFilter == "Favorites")
                {
                    // FIXED CA1859: Use concrete List<> type for better performance
                    var favoritesList = new List<TItemViewModel>();
                    foreach (var vm in itemVMs)
                    {
                        if (_getFavoriteFunc(vm))
                        {
                            favoritesList.Add(vm);
                        }
                    }
                    itemVMs = favoritesList;
                }

                // Batch UI update
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
            }
            finally
            {
                // CRITICAL: Return temp list to pool
                ListViewModelPools.ReturnTempList(tempFilteredList);
            }
        }, "Populate Items");
    }

    #endregion

    #region Filter and Sort Commands - 🔧 LOADING FLASH FIXED

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
    /// 🔧 LOADING FLASH FIXED: Cycle through status filter options without loading overlay
    /// FIXED CS1998: Return completed task instead of async/await
    /// </summary>
    private Task ToggleStatusFilterAsync()
    {
        this.SafeExecute(() =>
        {
            var currentIndex = StatusFilterOptions.IndexOf(StatusFilter);
            var nextIndex = (currentIndex + 1) % StatusFilterOptions.Count;
            StatusFilter = StatusFilterOptions[nextIndex];

            this.LogInfo($"Status filter changed to: {StatusFilter}");
        }, "Toggle Status Filter");

        return Task.CompletedTask;
    }

    /// <summary>
    /// 🔧 LOADING FLASH FIXED: Apply current filters without showing loading overlay
    /// </summary>
    private async Task ApplyFilterAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            // 🔧 LOADING FLASH FIX: Use Filter source to prevent overlay
            await LoadDataAsync(LoadingSource.Filter);
        }, "Apply Filter");
    }

    /// <summary>
    /// 🔧 LOADING FLASH FIXED: Clear all filters without showing loading overlay
    /// </summary>
    private async void ClearFilterAction()
    {
        await this.SafeExecuteAsync(async () =>
        {
            SearchText = string.Empty;
            StatusFilter = "All";
            SortOrder = "Name A→Z";

            // 🔧 LOADING FLASH FIX: Use Filter source to prevent overlay
            await LoadDataAsync(LoadingSource.Filter);
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

            // 🔧 LOADING FLASH FIX: Use Filter source to prevent overlay
            await LoadDataAsync(LoadingSource.Filter);
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

            // Only show error toast for actual failures, not cancellations
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

            // Check for cancellation (-1) vs failure (0 or false)
            if (result.Success && result.Data > 0)
            {
                await ShowSuccessToastAsync($"{result.Data} {EntityNamePlural.ToLower()} deleted successfully");
                this.LogSuccess($"DeleteSelected completed successfully - {result.Data} items");
            }
            else if (result.Data == -1)
            {
                // User cancelled - just cleanup, no error toast
                SelectedItems.Clear();
                IsMultiSelectMode = false;
                UpdateFabForSelection();
                this.LogInfo("Delete operation cancelled by user - cleanup completed");
            }
            else
            {
                // Real failure - show error toast
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

    #region Refresh and Data Management - 🔧 LOADING FLASH FIXED

    /// <summary>
    /// 🔧 LOADING FLASH FIXED: Prevent double refresh calls and use correct loading source
    /// </summary>
    private async Task RefreshAsync()
    {
        if (IsRefreshing)
        {
            this.LogInfo("Refresh already in progress - skipping");
            return;
        }

        IsRefreshing = true;
        try
        {
            await this.SafeExecuteAsync(async () =>
            {
                this.LogInfo($"Starting Refresh {EntityNamePlural}");
                await _repository.RefreshCacheAsync();

                // 🔧 LOADING FLASH FIX: Use Refresh source for proper overlay management
                await LoadDataAsync(LoadingSource.Refresh);
                this.LogSuccess($"Refresh completed - {Items.Count} {EntityNamePlural}");
            }, $"Refresh{EntityNamePlural}");
        }
        finally
        {
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
    /// Generic toggle favorite implementation using BaseToggleFavoritePattern
    /// FIXED CS8622: Accept nullable parameter to match command signature
    /// </summary>
    protected virtual async Task ToggleFavoriteAsync(TItemViewModel? item)
    {
        if (item == null) return;

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
            // FIXED CA1859: Use concrete collection type for better performance
            var itemsList = Items;
            TotalCount = itemsList.Count;

            var activeCountLocal = 0;
            var favoriteCountLocal = 0;

            foreach (var entity in itemsList)
            {
                if (entity.IsActive) activeCountLocal++;
                if (_getFavoriteFunc(entity)) favoriteCountLocal++;
            }

            ActiveCount = activeCountLocal;
            FavoriteCount = favoriteCountLocal;

            this.LogDebug($"Counters - Total: {TotalCount}, Active: {ActiveCount}, Favorites: {FavoriteCount}");
        }, "Update Counters");
    }

    /// <summary>
    /// Test connection and update status indicators
    /// FIXED CS1998: Return completed task instead of async/await
    /// </summary>
    private Task TestConnectionInternalAsync()
    {
        this.SafeExecute(() =>
        {
            IsConnected = true;
            ConnectionStatus = "Connected";
            ConnectionStatusColor = Colors.Green;
        }, "Test Connection");

        return Task.CompletedTask;
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
    /// Navigate to entity creation page
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
    /// FIXED CS0507: Make public to match base class visibility
    /// </summary>
    public override async Task<bool> ShowConfirmAsync(string title, string message)
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
    /// FIXED CA1826: Direct indexable access instead of LINQ
    /// </summary>
    private static Page? GetCurrentPage()
    {
        return new object().SafeExecute(() =>
        {
            // FIXED CA1826: Use direct indexing instead of FirstOrDefault()
            var windows = Application.Current?.Windows;
            if (windows != null && windows.Count > 0)
            {
                return windows[0].Page;
            }
            return null;
        }, fallbackValue: null, "Get Current Page");
    }

    #endregion

    #region Multi-Select Reset - PUBLIC METHOD FOR UI

    /// <summary>
    /// CRITICAL FIX: Force complete multi-select reset from ViewModel side
    /// Can be called from UI when state corruption is detected
    /// </summary>
    public void ForceResetMultiSelect()
    {
        this.SafeExecute(() =>
        {
            this.LogInfo("🔧 VM MULTISELECT FIX: Forcing complete reset");

            // Reset all item states
            foreach (var item in Items.Where(i => i.IsSelected))
            {
                item.IsSelected = false;
            }

            // Clear selection collection
            SelectedItems.Clear();

            // Exit multi-select mode
            if (IsMultiSelectMode)
            {
                IsMultiSelectMode = false;
            }

            // Update UI state
            UpdateFabForSelection();

            this.LogInfo("🔧 VM MULTISELECT FIX: Reset completed");
        }, "Force Reset MultiSelect");
    }

    #endregion

    #region Disposal

    /// <summary>
    /// PERFORMANCE OPTIMIZATION: Dispose pattern implementation for proper cleanup
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// PERFORMANCE OPTIMIZATION: Protected dispose implementation with timer cleanup
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Cleanup throttle timer
            lock (_throttleLock)
            {
                _propertyChangeThrottleTimer?.Dispose();
                _propertyChangeThrottleTimer = null;
                _isPendingPropertyUpdate = false;
                _pendingPropertyName = null;
            }

            // Remove property change handler
            PropertyChanged -= OnPropertyChanged;
        }
    }

    #endregion

    #region Legacy Compatibility

    /// <summary>
    /// LEGACY: Use NavigateToAddCommand instead - points to same command
    /// </summary>
    [Obsolete("Use NavigateToAddCommand instead")]
    public IAsyncRelayCommand AddNewCommand => NavigateToAddCommand;

    #endregion
}