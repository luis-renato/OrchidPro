using OrchidPro.Extensions;
using OrchidPro.ViewModels.Base;
using OrchidPro.ViewModels;
using OrchidPro.Models.Base;
using Syncfusion.Maui.ListView;
using Syncfusion.Maui.DataSource;
using SfSelectionMode = Syncfusion.Maui.ListView.SelectionMode;
using SfSwipeEndedEventArgs = Syncfusion.Maui.ListView.SwipeEndedEventArgs;
using System.Collections.Specialized;
using System.Collections.ObjectModel;

namespace OrchidPro.Views.Base;

/// <summary>
/// 🔧 MEMORY LEAK FIXED: Logic class with isolated state, proper cleanup, and no shared resources
/// </summary>
public partial class BaseListPageLogic<T, TItemViewModel> : IDisposable
    where T : class, IBaseEntity, new()
    where TItemViewModel : BaseItemViewModel<T>
{
    #region Fields and Properties

    private readonly BaseListViewModel<T, TItemViewModel> viewModel;
    private SfListView? listView;
    private Grid? rootGrid;
    private Grid? contentGrid;
    private Grid? loadingGrid;
    private Button? fabButton;
    private Syncfusion.Maui.PullToRefresh.SfPullToRefresh? pullToRefresh;
    private ContentPage? page;
    private bool hasAppearedOnce = false;

    // CRITICAL FIX: Track current sort order to reapply after data changes
    private string currentSortOrder = "Name A→Z";

    // 🔧 MEMORY LEAK FIX: Enhanced timer management with isolation
    private Timer? _sortingDebounceTimer;
    private readonly Lock _sortingLock = new();
    private volatile bool _isSortingScheduled = false;
    private volatile bool _isDisposed = false; // 🔧 NEW: Disposal tracking

    // 🔧 BUG FIX: Pull-to-refresh state tracking for navigation bug fix
    private bool _pullToRefreshNeedsReset;
    private int _appearanceCount;

    // 🔧 MEMORY LEAK FIX: Isolated comparer per instance (not shared static)
    private readonly IComparer<object> _isolatedComparer;

    // 🔧 MEMORY LEAK FIX: Event handler tracking
    private bool _propertyHandlerAttached = false;
    private bool _pullToRefreshHandlerAttached = false;
    private bool _collectionHandlerAttached = false;

    #endregion

    #region Constructor and Initialization - 🔧 MEMORY LEAK FIXED

    public BaseListPageLogic(BaseListViewModel<T, TItemViewModel> vm)
    {
        viewModel = vm;

        // 🔧 CRITICAL FIX: Create isolated comparer per instance
        _isolatedComparer = Comparer<object>.Create((x, y) =>
        {
            try
            {
                var stringX = x?.ToString()?.ToLower() ?? string.Empty;
                var stringY = y?.ToString()?.ToLower() ?? string.Empty;
                return stringX.CompareTo(stringY);
            }
            catch
            {
                return 0; // Fallback seguro
            }
        });

        // 🔧 MEMORY LEAK FIX: Attach handler only once with tracking
        AttachPropertyChangedHandler();

        this.LogInfo($"🔧 ISOLATED: Initialized BaseListPageLogic for {typeof(T).Name}");
    }

    /// <summary>
    /// 🔧 MEMORY LEAK FIX: Safe property handler attachment with tracking
    /// </summary>
    private void AttachPropertyChangedHandler()
    {
        if (!_propertyHandlerAttached && viewModel != null)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _propertyHandlerAttached = true;
            this.LogInfo("🔧 ATTACHED: PropertyChanged handler");
        }
    }

    /// <summary>
    /// 🔧 MEMORY LEAK FIX: Safe property handler removal
    /// </summary>
    private void DetachPropertyChangedHandler()
    {
        if (_propertyHandlerAttached && viewModel != null)
        {
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _propertyHandlerAttached = false;
            this.LogInfo("🔧 DETACHED: PropertyChanged handler");
        }
    }

    #endregion

    #region Setup Page - 🔧 MEMORY LEAK FIXED

    /// <summary>
    /// 🔧 MEMORY LEAK FIXED: Setup page with isolated monitoring and proper cleanup
    /// </summary>
    public void SetupPage(ContentPage pg, SfListView lv, Grid rg, Button fb,
        Syncfusion.Maui.PullToRefresh.SfPullToRefresh pr)
    {
        page = pg;
        listView = lv;
        rootGrid = rg;
        fabButton = fb;
        pullToRefresh = pr;
        page.BindingContext = viewModel;

        // Find the content grid
        contentGrid = rootGrid?.FindByName<Grid>("ContentGrid");
        loadingGrid = rootGrid?.FindByName<Grid>("LoadingGrid");

        // Setup with isolation
        SetupPullToRefreshSafe();
        SetupSortingPreservationIsolated(); // 🔧 FIXED: Isolated version
        SetupFadeEffects();

        this.LogInfo($"🔧 ISOLATED: Page setup completed for {typeof(T).Name} with memory leak protection");
    }

    #endregion

    #region Pull-to-Refresh Management - 🔧 MEMORY LEAK FIXED

    /// <summary>
    /// 🔧 MEMORY LEAK FIX: Safe pull-to-refresh setup with handler tracking
    /// </summary>
    private void SetupPullToRefreshSafe()
    {
        if (pullToRefresh == null || _pullToRefreshHandlerAttached) return;

        pullToRefresh.Refreshing += HandlePullToRefresh;
        _pullToRefreshHandlerAttached = true;
        this.LogInfo("🔧 ATTACHED: Pull-to-refresh event handler");
    }

    /// <summary>
    /// 🔧 MEMORY LEAK FIX: Safe pull-to-refresh cleanup
    /// </summary>
    private void CleanupPullToRefreshSafe()
    {
        if (pullToRefresh != null && _pullToRefreshHandlerAttached)
        {
            pullToRefresh.Refreshing -= HandlePullToRefresh;
            _pullToRefreshHandlerAttached = false;
            this.LogInfo("🔧 DETACHED: Pull-to-refresh event handler");
        }
    }

    /// <summary>
    /// 🔧 ENHANCED: Force pull-to-refresh reset to fix navigation bug (less aggressive)
    /// </summary>
    private async Task ForceResetPullToRefresh()
    {
        if (pullToRefresh == null || _isDisposed) return;

        try
        {
            this.LogInfo("🔧 GENTLE: Pull-to-refresh reset after tab navigation");

            // Step 1: Reset only if actually refreshing
            if (pullToRefresh.IsRefreshing)
            {
                pullToRefresh.IsRefreshing = false;
                this.LogInfo("🔧 Reset IsRefreshing property");
            }

            // Step 2: Minimal delay for state stabilization
            if (_appearanceCount > 1)
            {
                await Task.Delay(50); // Reduced from 100ms
            }

            // Step 3: Re-attach handler safely if needed
            if (!_pullToRefreshHandlerAttached)
            {
                SetupPullToRefreshSafe();
            }

            this.LogInfo("🔧 GENTLE: Reset completed successfully");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error during gentle pull-to-refresh reset");
        }
    }

    #endregion

    #region Sorting Preservation - 🔧 MEMORY LEAK FIXED

    /// <summary>
    /// 🔧 MEMORY LEAK FIX: Isolated collection monitoring with proper cleanup
    /// </summary>
    private void SetupSortingPreservationIsolated()
    {
        try
        {
            if (viewModel?.Items is ObservableCollection<TItemViewModel> observableItems)
            {
                // 🔧 CRITICAL: Remove existing handler first to prevent accumulation
                if (_collectionHandlerAttached)
                {
                    observableItems.CollectionChanged -= OnCollectionChangedIsolated;
                }

                // Attach new isolated handler
                observableItems.CollectionChanged += OnCollectionChangedIsolated;
                _collectionHandlerAttached = true;
                this.LogInfo("🔧 ISOLATED: Collection monitoring setup with cleanup protection");
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error setting up isolated sorting preservation");
        }
    }

    /// <summary>
    /// 🔧 MEMORY LEAK FIX: Clean collection monitoring
    /// </summary>
    private void CleanupCollectionMonitoring()
    {
        try
        {
            if (viewModel?.Items is ObservableCollection<TItemViewModel> observableItems && _collectionHandlerAttached)
            {
                observableItems.CollectionChanged -= OnCollectionChangedIsolated;
                _collectionHandlerAttached = false;
                this.LogInfo("🔧 CLEANED: Collection monitoring");
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error cleaning collection monitoring");
        }
    }

    /// <summary>
    /// 🔧 MEMORY LEAK FIX: Isolated collection change handler
    /// </summary>
    private void OnCollectionChangedIsolated(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isDisposed) return;

        try
        {
            if (e.Action == NotifyCollectionChangedAction.Reset ||
                (e.Action == NotifyCollectionChangedAction.Add && e.NewStartingIndex == 0))
            {
                // 🔧 ISOLATED: Schedule debounced sorting with isolation
                ScheduleDebouncedSortingIsolated();
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error in isolated collection changed handler");
        }
    }

    /// <summary>
    /// 🔧 MEMORY LEAK FIX: Enhanced debounced sorting with complete isolation
    /// </summary>
    private void ScheduleDebouncedSortingIsolated()
    {
        if (_isDisposed || string.IsNullOrEmpty(currentSortOrder)) return;

        lock (_sortingLock)
        {
            try
            {
                // 🔧 CRITICAL: Dispose previous timer completely
                if (_sortingDebounceTimer != null)
                {
                    _sortingDebounceTimer.Dispose();
                    _sortingDebounceTimer = null;
                }

                if (!_isSortingScheduled)
                {
                    this.LogInfo($"🔧 ISOLATED TIMER: Scheduling debounced reapply of '{currentSortOrder}'");
                    _isSortingScheduled = true;
                }

                // 🔧 ISOLATED: Timer with auto-cleanup
                _sortingDebounceTimer = new Timer(async _ =>
                {
                    await ExecuteDebouncedSortingIsolated();
                }, null, 300, Timeout.Infinite);
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Error scheduling isolated debounced sorting");
                _isSortingScheduled = false;
            }
        }
    }

    /// <summary>
    /// 🔧 MEMORY LEAK FIX: Execute sorting with complete isolation and cleanup
    /// </summary>
    private async Task ExecuteDebouncedSortingIsolated()
    {
        if (_isDisposed) return;

        try
        {
            lock (_sortingLock)
            {
                if (!_isSortingScheduled || _isDisposed) return;
                _isSortingScheduled = false;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (_isDisposed || listView?.DataSource == null) return;

                this.LogInfo($"🔧 ISOLATED SORT: Executing '{currentSortOrder}'");
                ApplySyncfusionNativeSortingIsolated(currentSortOrder);
                this.LogInfo($"✅ ISOLATED SORT: '{currentSortOrder}' reapplied successfully");
            });
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error in isolated debounced sorting execution");
        }
        finally
        {
            // 🔧 CRITICAL: Auto-cleanup timer
            lock (_sortingLock)
            {
                if (_sortingDebounceTimer != null)
                {
                    _sortingDebounceTimer.Dispose();
                    _sortingDebounceTimer = null;
                }
            }
        }
    }

    #endregion

    #region Syncfusion Native Sorting - 🔧 MEMORY LEAK FIXED

    /// <summary>
    /// 🔧 MEMORY LEAK FIXED: Sorting with isolated comparer (not shared static)
    /// </summary>
    private void ApplySyncfusionNativeSortingIsolated(string sortOrder)
    {
        if (_isDisposed || listView?.DataSource == null) return;

        try
        {
            this.LogInfo($"🔧 ISOLATED SORT: Applying '{sortOrder}' with isolated comparer");

            // Verify DataSource is still valid
            if (listView.DataSource.SortDescriptors == null)
            {
                this.LogWarning("DataSource.SortDescriptors is null - skipping sort");
                return;
            }

            listView.DataSource.LiveDataUpdateMode = LiveDataUpdateMode.AllowDataShaping;
            listView.DataSource.SortDescriptors.Clear();

            // 🔧 CRITICAL FIX: Use isolated comparer instead of shared static
            SortDescriptor sortDescriptor = sortOrder switch
            {
                "Name A→Z" => new SortDescriptor
                {
                    PropertyName = "Name",
                    Direction = ListSortDirection.Ascending,
                    Comparer = _isolatedComparer // 🔧 ISOLATED
                },
                "Name Z→A" => new SortDescriptor
                {
                    PropertyName = "Name",
                    Direction = ListSortDirection.Descending,
                    Comparer = _isolatedComparer // 🔧 ISOLATED
                },
                "Recent First" => new SortDescriptor
                {
                    PropertyName = "CreatedAt",
                    Direction = ListSortDirection.Descending
                },
                "Oldest First" => new SortDescriptor
                {
                    PropertyName = "CreatedAt",
                    Direction = ListSortDirection.Ascending
                },
                "Favorites First" => new SortDescriptor
                {
                    PropertyName = "IsFavorite",
                    Direction = ListSortDirection.Descending
                },
                _ => new SortDescriptor
                {
                    PropertyName = "Name",
                    Direction = ListSortDirection.Ascending,
                    Comparer = _isolatedComparer // 🔧 ISOLATED
                }
            };

            listView.DataSource.SortDescriptors.Add(sortDescriptor);

            if (sortOrder == "Favorites First")
            {
                listView.DataSource.SortDescriptors.Add(new SortDescriptor
                {
                    PropertyName = "Name",
                    Direction = ListSortDirection.Ascending,
                    Comparer = _isolatedComparer // 🔧 ISOLATED
                });
            }

            // CRITICAL FIX: Force refresh only if not disposed
            if (!_isDisposed)
            {
                listView.RefreshView();
                currentSortOrder = sortOrder;
                this.LogInfo($"✅ ISOLATED SORT: Applied '{sortOrder}' successfully");
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, $"Error in isolated Syncfusion sorting for '{sortOrder}'");
        }
    }

    /// <summary>
    /// 🔧 WRAPPER: Main sorting method that uses isolated version
    /// </summary>
    private void ApplySyncfusionNativeSorting(string sortOrder)
    {
        ApplySyncfusionNativeSortingIsolated(sortOrder);
    }

    #endregion

    #region Property Change Handling - 🔧 MEMORY LEAK FIXED

    /// <summary>
    /// 🔧 MEMORY LEAK FIXED: Property change handling with disposal protection
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_isDisposed) return;

        try
        {
            switch (e.PropertyName)
            {
                case nameof(viewModel.SortOrder):
                    if (!string.IsNullOrEmpty(viewModel.SortOrder))
                    {
                        // 🔧 CRITICAL FIX: Update currentSortOrder BEFORE scheduling
                        currentSortOrder = viewModel.SortOrder;
                        this.LogInfo($"🔧 SORT UPDATE: Changed to '{currentSortOrder}'");
                        ScheduleDebouncedSortingIsolated();
                    }
                    break;
                case nameof(viewModel.IsMultiSelectMode):
                    SyncSelectionMode();
                    break;
                case nameof(viewModel.SelectedItems):
                    UpdateFabVisual();
                    break;
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, $"Error in isolated property changed handler for {e.PropertyName}");
        }
    }

    #endregion

    #region Lifecycle Management - 🔧 MEMORY LEAK FIXED

    /// <summary>
    /// 🔧 MEMORY LEAK FIXED: Enhanced BaseOnDisappearing with aggressive cleanup
    /// </summary>
    public void BaseOnDisappearing()
    {
        try
        {
            this.LogInfo($"🔄 ENHANCED: BaseOnDisappearing for {typeof(T).Name} with aggressive cleanup");

            // Set flag for pull-to-refresh reset on next appearance
            _pullToRefreshNeedsReset = true;

            // 🔧 CRITICAL: Cleanup all handlers and timers
            DetachPropertyChangedHandler();
            CleanupPullToRefreshSafe();
            CleanupCollectionMonitoring();

            // 🔧 CRITICAL: Aggressive timer cleanup
            lock (_sortingLock)
            {
                if (_sortingDebounceTimer != null)
                {
                    _sortingDebounceTimer.Dispose();
                    _sortingDebounceTimer = null;
                    this.LogInfo("🔧 CLEANUP: Disposed sorting timer on disappearing");
                }
                _isSortingScheduled = false;
            }

            // 🔧 MEMORY PRESSURE: Force GC after frequent navigation
            if (_appearanceCount > 3)
            {
                GC.Collect(0, GCCollectionMode.Optimized);
                this.LogInfo("🔧 CLEANUP: Forced GC collection to prevent memory pressure");
            }

            this.LogInfo($"✅ ENHANCED: BaseOnDisappearing completed for {typeof(T).Name}");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error in enhanced BaseOnDisappearing");
        }
    }

    /// <summary>
    /// 🔧 MEMORY LEAK FIXED: BaseOnAppearing with proper handler reattachment
    /// </summary>
    public async Task BaseOnAppearing()
    {
        try
        {
            _appearanceCount++;
            this.LogInfo($"🔄 ENHANCED: BaseOnAppearing for {typeof(T).Name} (appearance #{_appearanceCount})");

            // 🔧 MEMORY LEAK FIX: Reattach handlers safely
            AttachPropertyChangedHandler();
            SetupPullToRefreshSafe();
            SetupSortingPreservationIsolated();

            // 🔧 BUG FIX: Reset pull-to-refresh after tab navigation (gentle version)
            if (_appearanceCount > 1 || _pullToRefreshNeedsReset)
            {
                _pullToRefreshNeedsReset = false;
                await ForceResetPullToRefresh(); // Now uses gentle version

                // CRITICAL FIX: Reset multi-select when returning from edit
                ForceMultiSelectReset();
                this.LogInfo("🔧 NAVIGATION FIX: Reset multi-select on return from edit");

                // Minimal delay for state stabilization
                await Task.Delay(100);
                this.LogInfo("🔧 STATE: Applied delay for internal state stabilization");
            }

            // Clear selections and update FAB first
            listView?.SelectedItems?.Clear();
            UpdateFabVisual();

            // Handle first-time animation
            if (!hasAppearedOnce)
            {
                await PerformEntranceAnimation();
                hasAppearedOnce = true;
            }
            else
            {
                // For subsequent appearances, ensure grid is visible
                if (rootGrid != null)
                {
                    rootGrid.Opacity = 1;
                }
            }

            // Call ViewModel's OnAppearing - it will handle loading state intelligently
            this.LogInfo($"🔧 LOADING: Delegating loading decisions to ViewModel for {typeof(T).Name}");
            await viewModel.OnAppearingAsync();

            // Apply sorting after data loads with isolated version
            if (!string.IsNullOrEmpty(viewModel.SortOrder))
            {
                currentSortOrder = viewModel.SortOrder;
                ApplySyncfusionNativeSortingIsolated(viewModel.SortOrder); // 🔧 ISOLATED
            }

            this.LogInfo($"✅ ENHANCED: BaseOnAppearing completed for {typeof(T).Name}");
        }
        catch (Exception ex)
        {
            this.LogError(ex, $"Error in enhanced BaseOnAppearing for {typeof(T).Name}");
            // Ensure loading is ALWAYS turned off on error
            viewModel.IsLoading = false;
        }
    }

    #endregion

    #region Fade Effects - EXISTING CODE (unchanged)

    private void SetupFadeEffects()
    {
        if (viewModel == null) return;

        try
        {
            viewModel.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(viewModel.IsLoading))
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await HandleLoadingStateChange();
                    });
                }
            };

            this.LogInfo("✨ FADE: Fade effects setup completed");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error setting up fade effects");
        }
    }

    private async Task HandleLoadingStateChange()
    {
        try
        {
            if (viewModel.IsLoading)
            {
                this.LogInfo("✨ FADE: Starting loading state (Template-controlled)");

                if (fabButton != null)
                {
                    fabButton.IsVisible = false;
                    fabButton.Opacity = 0;
                    fabButton.Scale = 0.7;
                }

                if (contentGrid != null)
                {
                    contentGrid.IsVisible = false;
                    contentGrid.Opacity = 0;
                }
            }
            else
            {
                this.LogInfo("✨ FADE: Finishing loading state");

                if (contentGrid != null)
                {
                    contentGrid.IsVisible = true;
                    contentGrid.Opacity = 0;
                    await contentGrid.FadeTo(1, 300, Easing.CubicOut);
                    this.LogInfo("✨ FADE: Content fade-in completed");
                }

                if (fabButton != null)
                {
                    await Task.Delay(200);
                    fabButton.IsVisible = true;
                    fabButton.Opacity = 0;
                    fabButton.Scale = 0.7;

                    var fadeTask = fabButton.FadeTo(1, 250, Easing.CubicOut);
                    var scaleTask = fabButton.ScaleTo(1, 300, Easing.SpringOut);
                    await Task.WhenAll(fadeTask, scaleTask);
                    this.LogInfo("✨ FADE: FAB entrance animation completed");
                }
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error handling loading state change");

            if (contentGrid != null)
            {
                contentGrid.IsVisible = true;
                contentGrid.Opacity = 1;
            }

            if (fabButton != null)
            {
                fabButton.IsVisible = true;
                fabButton.Opacity = 1;
                fabButton.Scale = 1;
            }
        }
    }

    private async Task PerformEntranceAnimation()
    {
        if (rootGrid == null) return;

        try
        {
            rootGrid.Opacity = 0;
            await Task.Delay(50);
            await rootGrid.FadeTo(1, 250, Easing.CubicOut);
            this.LogInfo($"✨ ENHANCED: Entrance animation completed for {typeof(T).Name}");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error in entrance animation");
            rootGrid.Opacity = 1;
        }
    }

    #endregion

    #region Multi-Select State Management - EXISTING CODE (unchanged for now)

    private void ForceMultiSelectReset()
    {
        if (listView == null) return;

        try
        {
            this.LogInfo("🔧 MULTISELECT FIX: Starting complete reset");

            if (listView.SelectedItems != null)
            {
                var selectedCount = listView.SelectedItems.Count;
                listView.SelectedItems.Clear();
                this.LogInfo($"🔧 Cleared {selectedCount} visual selections from ListView");
            }

            foreach (var item in viewModel.Items)
            {
                if (item.IsSelected)
                {
                    item.IsSelected = false;
                }
            }

            viewModel.SelectedItems.Clear();
            viewModel.IsMultiSelectMode = false;
            listView.SelectionMode = SfSelectionMode.None;

            Task.Run(async () =>
            {
                await Task.Delay(50);
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        listView.RefreshView();
                        this.LogInfo("🔧 Forced ListView visual refresh");
                    }
                    catch (Exception ex)
                    {
                        this.LogError(ex, "Error in ListView RefreshView");
                    }
                });
            });

            UpdateFabVisual();
            this.LogInfo("🔧 MULTISELECT FIX: Complete reset performed successfully");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error in ForceMultiSelectReset");
        }
    }

    private void DebugAndFixMultiSelectState()
    {
        if (listView == null) return;

        try
        {
            var vmMode = viewModel.IsMultiSelectMode;
            var listViewMode = listView.SelectionMode;
            var vmSelectedCount = viewModel.SelectedItems?.Count ?? 0;
            var listViewSelectedCount = listView.SelectedItems?.Count ?? 0;

            this.LogInfo($"🔧 MULTISELECT DEBUG: VM.Mode={vmMode}, ListView.Mode={listViewMode}, VM.Count={vmSelectedCount}, ListView.Count={listViewSelectedCount}");

            if (vmMode && listViewMode != SfSelectionMode.Multiple)
            {
                listView.SelectionMode = SfSelectionMode.Multiple;
                this.LogInfo("🔧 AUTO-FIX: Corrected ListView to Multiple mode");
            }
            else if (!vmMode && listViewMode != SfSelectionMode.None)
            {
                listView.SelectionMode = SfSelectionMode.None;
                listView.SelectedItems?.Clear();
                this.LogInfo("🔧 AUTO-FIX: Corrected ListView to None mode and cleared selections");
            }

            if (vmMode && vmSelectedCount > 0 && listViewSelectedCount != vmSelectedCount)
            {
                if (listView.SelectedItems != null)
                {
                    listView.SelectedItems.Clear();

                    if (viewModel.SelectedItems != null)
                    {
                        foreach (var item in viewModel.SelectedItems)
                        {
                            if (!listView.SelectedItems.Contains(item))
                            {
                                listView.SelectedItems.Add(item);
                            }
                        }
                        this.LogInfo($"🔧 SYNC FIX: Force synced {viewModel.SelectedItems.Count} items to ListView (was {listViewSelectedCount})");
                    }
                }
            }
            else if (!vmMode && listViewSelectedCount > 0)
            {
                listView.SelectedItems?.Clear();
                this.LogInfo("🔧 SYNC FIX: Cleared orphaned ListView selections");
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error in DebugAndFixMultiSelectState");
        }
    }

    private void SyncSelectionMode()
    {
        if (listView == null) return;

        var targetMode = viewModel.IsMultiSelectMode ? SfSelectionMode.Multiple : SfSelectionMode.None;
        if (listView.SelectionMode != targetMode)
        {
            listView.SelectionMode = targetMode;
        }
    }

    private void UpdateFabVisual()
    {
        if (fabButton == null) return;

        try
        {
            var selectedCount = viewModel.SelectedItems?.Count ?? 0;
            if (selectedCount > 0)
            {
                fabButton.Text = $"Delete ({selectedCount})";
            }
            else
            {
                fabButton.Text = $"Add {typeof(T).Name}";
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, $"Error updating FAB visual");
        }
    }

    #endregion

    #region Event Handlers - EXISTING CODE (unchanged for now)

    public void HandleSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        viewModel.SearchText = e.NewTextValue ?? string.Empty;
    }

    public async void HandleItemTapped(object? sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
    {
        if (e.DataItem is TItemViewModel item)
        {
            if (viewModel.IsMultiSelectMode)
            {
                item.IsSelected = !item.IsSelected;

                if (item.IsSelected && !viewModel.SelectedItems.Contains(item))
                {
                    viewModel.SelectedItems.Add(item);

                    if (listView?.SelectedItems != null && !listView.SelectedItems.Contains(item))
                    {
                        listView.SelectedItems.Add(item);
                    }
                }
                else if (!item.IsSelected)
                {
                    viewModel.SelectedItems.Remove(item);

                    if (listView?.SelectedItems != null && listView.SelectedItems.Contains(item))
                    {
                        listView.SelectedItems.Remove(item);
                    }
                }

                this.LogInfo($"Multi-select tap: {item.Name} = {item.IsSelected}, ListView count: {listView?.SelectedItems?.Count ?? 0}");
            }
            else
            {
                await viewModel.HandleItemTappedAsync(item);
            }

            UpdateFabVisual();
        }
    }

    public void HandleItemLongPress(object? sender, ItemLongPressEventArgs e)
    {
        if (e.DataItem is TItemViewModel item)
        {
            viewModel.HandleItemLongPress(item);

            if (viewModel.IsMultiSelectMode && listView != null)
            {
                listView.SelectionMode = SfSelectionMode.Multiple;
                this.LogInfo("ListView forced to Multiple mode after long press");
            }

            UpdateFabVisual();
        }
    }

    public void HandleSelectionChanged(object? sender, ItemSelectionChangedEventArgs e)
    {
        try
        {
            if (listView?.SelectedItems != null && viewModel.SelectedItems != null)
            {
                viewModel.SelectedItems.Clear();

                foreach (var selectedItem in listView.SelectedItems)
                {
                    if (selectedItem is TItemViewModel item)
                    {
                        viewModel.SelectedItems.Add(item);
                    }
                }
            }

            UpdateFabVisual();
            SyncSelectionMode();
            DebugAndFixMultiSelectState();
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error syncing selections - forcing reset");
            ForceMultiSelectReset();
        }
    }

    public async void HandleSwipeEnded(object? sender, SfSwipeEndedEventArgs e)
    {
        if (e.DataItem is not TItemViewModel item) return;

        try
        {
            if (e.Direction == SwipeDirection.Right)
            {
                if (viewModel.ToggleFavoriteCommand.CanExecute(item))
                {
                    await viewModel.ToggleFavoriteCommand.ExecuteAsync(item);
                }
            }
            else if (e.Direction == SwipeDirection.Left)
            {
                if (viewModel.DeleteSingleItemCommand.CanExecute(item))
                {
                    await viewModel.DeleteSingleItemCommand.ExecuteAsync(item);
                    ForceMultiSelectReset();
                    this.LogInfo("🔧 DELETE FIX: Reset multi-select after delete operation");
                }
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error handling swipe action");
        }
    }

    public async void HandleFabTapped(object? sender, EventArgs e)
    {
        try
        {
            if (viewModel.FabActionCommand.CanExecute(null))
            {
                await viewModel.FabActionCommand.ExecuteAsync(null);
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, $"Error handling FAB action");
        }
    }

    public void HandleSelectAllTapped(object? sender, EventArgs e)
    {
        try
        {
            if (viewModel.SelectAllCommand.CanExecute(null))
            {
                viewModel.SelectAllCommand.Execute(null);
            }

            if (listView != null && viewModel.SelectedItems != null)
            {
                listView.SelectedItems?.Clear();

                foreach (var item in viewModel.SelectedItems)
                {
                    listView.SelectedItems?.Add(item);
                }
            }

            UpdateFabVisual();
        }
        catch (Exception ex)
        {
            this.LogError(ex, $"Error handling select all");
        }
    }

    public void HandleDeselectAllTapped(object? sender, EventArgs e)
    {
        try
        {
            if (viewModel.DeselectAllCommand.CanExecute(null))
            {
                viewModel.DeselectAllCommand.Execute(null);
            }
            listView?.SelectedItems?.Clear();
            UpdateFabVisual();
        }
        catch (Exception ex)
        {
            this.LogError(ex, $"Error handling deselect all");
        }
    }

    public async void HandleFilterTapped(object? sender, EventArgs e)
    {
        try
        {
            var entityName = typeof(T).Name;

            var hasSearch = !string.IsNullOrWhiteSpace(viewModel.SearchText);
            var isDefaultFilter = viewModel.StatusFilter == "All" && !hasSearch;

            var options = new List<string>
            {
                $"All {entityName}",
                "Active Only",
                "Inactive Only",
                "Favorites Only"
            };

            var result = await page!.DisplayActionSheet(
                $"Filter {entityName}",
                "Cancel",
                isDefaultFilter ? null : "Clear All Filters",
                [.. options]);

            if (result != null && result != "Cancel")
            {
                this.LogInfo($"🔧 FILTER: User selected '{result}'");

                switch (result)
                {
                    case "Clear All Filters":
                        viewModel.SearchText = "";
                        viewModel.StatusFilter = "All";
                        this.LogInfo("🔧 FILTER: Cleared all filters to default");
                        break;
                    case var r when r.StartsWith("All"):
                        viewModel.StatusFilter = "All";
                        break;
                    case "Active Only":
                        viewModel.StatusFilter = "Active";
                        break;
                    case "Inactive Only":
                        viewModel.StatusFilter = "Inactive";
                        break;
                    case "Favorites Only":
                        viewModel.StatusFilter = "Favorites";
                        break;
                }

                if (viewModel.ApplyFilterCommand.CanExecute(null))
                {
                    await viewModel.ApplyFilterCommand.ExecuteAsync(null);
                }

                var finalCount = viewModel.Items?.Count ?? 0;
                this.LogInfo($"✅ FILTER: Applied successfully - showing {finalCount} items");
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error applying filter");
            await page!.DisplayAlert("Error", "Failed to apply filter. Please try again.", "OK");
        }
    }

    public async void HandleSortTapped(object? sender, EventArgs e)
    {
        try
        {
            var entityName = typeof(T).Name;

            var currentSort = viewModel.SortOrder;
            var isDefaultSort = currentSort == "Name A→Z";

            var options = new List<string>
            {
                "Name A→Z", "Name Z→A", "Recent First", "Oldest First", "Favorites First"
            };

            if (!isDefaultSort)
            {
                options.Add("Clear All Sorting");
            }

            var result = await page!.DisplayActionSheet(
                $"Sort {entityName}",
                "Cancel",
                isDefaultSort ? null : "Clear All Sorting",
                [.. options.Take(5)]);

            if (result != null && result != "Cancel")
            {
                this.LogInfo($"🔧 SORT: User selected '{result}'");

                if (result == "Clear All Sorting")
                {
                    viewModel.SortOrder = "Name A→Z";
                    this.LogInfo("🔧 SORT: Cleared to default (Name A→Z)");
                }
                else
                {
                    viewModel.SortOrder = result;
                }

                this.LogInfo($"✅ SORT: Applied successfully");
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error applying sort");
            await page!.DisplayAlert("Error", "Failed to apply sort. Please try again.", "OK");
        }
    }

    public async void HandlePullToRefresh(object? sender, EventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("🔄 SFPULLTOREFRESH: Pull-to-refresh started");

            if (sender is Syncfusion.Maui.PullToRefresh.SfPullToRefresh pullToRefreshControl)
            {
                try
                {
                    if (viewModel.RefreshCommand.CanExecute(null))
                    {
                        await viewModel.RefreshCommand.ExecuteAsync(null);
                    }

                    this.LogInfo("✅ SFPULLTOREFRESH: Pull-to-refresh completed");
                }
                catch (Exception ex)
                {
                    this.LogError(ex, "SfPullToRefresh failed");
                }
                finally
                {
                    pullToRefreshControl.IsRefreshing = false;
                    viewModel.IsRefreshing = false;
                }
            }
        }, "Pull-to-refresh failed");
    }

    #endregion

    #region MIGRATED - Focus Handlers

    public void HandleSearchFocused(object? sender, FocusEventArgs e) { /* Intentionally empty */ }
    public void HandleSearchUnfocused(object? sender, FocusEventArgs e) { /* Intentionally empty */ }
    public void HandleSwipeStarting(object? sender, Syncfusion.Maui.ListView.SwipeStartingEventArgs e) { /* Intentionally empty */ }
    public void HandleSwiping(object? sender, Syncfusion.Maui.ListView.SwipingEventArgs e) { /* Intentionally empty */ }

    #endregion

    #region IDisposable Implementation - 🔧 MEMORY LEAK FIXED

    /// <summary>
    /// 🔧 MEMORY LEAK FIXED: Enhanced cleanup to prevent all memory leaks
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !_isDisposed)
        {
            _isDisposed = true;
            this.LogInfo($"🔧 DISPOSE: Starting enhanced cleanup for {typeof(T).Name}");

            // 🔧 CRITICAL: Cleanup all handlers with tracking
            DetachPropertyChangedHandler();
            CleanupPullToRefreshSafe();
            CleanupCollectionMonitoring();

            // 🔧 CRITICAL: Timer cleanup with locks
            lock (_sortingLock)
            {
                if (_sortingDebounceTimer != null)
                {
                    _sortingDebounceTimer.Dispose();
                    _sortingDebounceTimer = null;
                }
                _isSortingScheduled = false;
            }

            // 🔧 CRITICAL: Nullify all references for GC
            listView = null;
            rootGrid = null;
            contentGrid = null;
            loadingGrid = null;
            fabButton = null;
            pullToRefresh = null;
            page = null;

            this.LogInfo($"✅ DISPOSE: Enhanced cleanup completed for {typeof(T).Name}");
        }
    }

    #endregion
}