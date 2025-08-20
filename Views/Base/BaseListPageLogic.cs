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
/// PERFORMANCE OPTIMIZED Logic class with debounced sorting, throttled collection monitoring, and pull-to-refresh bug fixes.
/// 🔧 LOADING FLASH FIXED: Maintains 100% compatibility while eliminating loading overlay flash during filter operations.
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

    // PERFORMANCE OPTIMIZATION: Debounce sorting to prevent excessive calls
    private Timer? _sortingDebounceTimer;
    private readonly Lock _sortingLock = new();
    private volatile bool _isSortingScheduled = false;

    // 🔧 BUG FIX: Pull-to-refresh state tracking for navigation bug fix
    private bool _pullToRefreshNeedsReset;
    private int _appearanceCount;

    #endregion

    #region Constructor and Initialization

    public BaseListPageLogic(BaseListViewModel<T, TItemViewModel> vm)
    {
        viewModel = vm;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        this.LogInfo($"Initialized BaseListPageLogic for {typeof(T).Name}");
    }

    /// <summary>
    /// ENHANCED: Setup page with template-based loading and pull-to-refresh bug fixes
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

        // 🔧 TEMPLATE NOTE: LoadingGrid is in ControlTemplate, controlled by IsVisible="{Binding IsLoading}"
        loadingGrid = rootGrid?.FindByName<Grid>("LoadingGrid"); // Will be null, but kept for compatibility

        // Setup pull-to-refresh with bug fix support
        SetupPullToRefresh();

        // Setup sorting preservation
        SetupSortingPreservation();

        // Monitor IsLoading for fade effects
        SetupFadeEffects();

        this.LogInfo($"🔧 Page setup completed for {typeof(T).Name} with template-based loading");
    }

    #endregion

    #region Pull-to-Refresh Management with Bug Fixes

    private void SetupPullToRefresh()
    {
        if (pullToRefresh == null) return;

        pullToRefresh.Refreshing += HandlePullToRefresh;
        this.LogInfo("Pull-to-refresh event handler attached");
    }

    /// <summary>
    /// 🔧 ENHANCED: Force pull-to-refresh reset to fix navigation bug
    /// This is a workaround for a known Syncfusion bug where pull-to-refresh stops working after tab navigation
    /// </summary>
    private async Task ForceResetPullToRefresh()
    {
        if (pullToRefresh == null) return;

        try
        {
            this.LogInfo("🔧 PULL-TO-REFRESH FIX: Forcing reset after tab navigation");

            // Step 1: Completely disconnect event handlers
            pullToRefresh.Refreshing -= HandlePullToRefresh;

            // Step 2: Reset all pull-to-refresh properties
            pullToRefresh.IsRefreshing = false;
            pullToRefresh.IsEnabled = false;

            // Step 3: Force layout update
            await Task.Delay(100);

            // Step 4: Re-enable with fresh state
            pullToRefresh.IsEnabled = true;

            // Step 5: Re-attach event handler
            pullToRefresh.Refreshing += HandlePullToRefresh;

            // Step 6: Force a visual refresh of the control
            if (pullToRefresh.Parent is Layout layout)
            {
                pullToRefresh.InvalidateMeasure();
                if (pullToRefresh.Parent is View parent)
                {
                    parent.InvalidateMeasure();
                }
            }

            this.LogInfo("🔧 PULL-TO-REFRESH FIX: Reset completed successfully");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error during pull-to-refresh reset");

            // Fallback: Ensure event handler is at least reattached
            try
            {
                if (pullToRefresh != null)
                {
                    pullToRefresh.Refreshing -= HandlePullToRefresh;
                    pullToRefresh.Refreshing += HandlePullToRefresh;
                }
            }
            catch
            {
                // Silent fallback
            }
        }
    }

    #endregion

    #region Fade Effects and Loading State Management - 🔧 LOADING FLASH FIXED

    /// <summary>
    /// 🔧 LOADING FLASH FIXED: Monitor IsLoading changes and apply smooth transitions only when appropriate
    /// </summary>
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

    /// <summary>
    /// 🔧 LOADING FLASH FIXED: Handle loading state changes with smart fade animation that respects the ViewModel's loading source decisions
    /// </summary>
    private async Task HandleLoadingStateChange()
    {
        try
        {
            if (viewModel.IsLoading)
            {
                // 🔧 LOADING FLASH FIX: Only show loading overlay when ViewModel explicitly sets IsLoading to true
                // The ViewModel now intelligently decides when to show loading based on operation type
                this.LogInfo("✨ FADE: Starting loading state (Template-controlled)");

                // Hide FAB immediately
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

                // 🔧 TEMPLATE FIX: LoadingGrid is handled by template binding IsVisible="{Binding IsLoading}"
                // No manual manipulation needed - template shows/hides automatically
            }
            else
            {
                // Finished loading: Hide loading, fade in content + FAB
                this.LogInfo("✨ FADE: Finishing loading state");

                // 🔧 TEMPLATE FIX: LoadingGrid hidden automatically by template binding
                // No manual manipulation needed

                if (contentGrid != null)
                {
                    contentGrid.IsVisible = true;
                    contentGrid.Opacity = 0;

                    // Smooth fade in content
                    await contentGrid.FadeTo(1, 300, Easing.CubicOut);

                    this.LogInfo("✨ FADE: Content fade-in completed");
                }

                // Animate FAB entrance - delayed and bouncy
                if (fabButton != null)
                {
                    await Task.Delay(200); // Small delay after content

                    fabButton.IsVisible = true;
                    fabButton.Opacity = 0;
                    fabButton.Scale = 0.7;

                    // Parallel animations for smooth entrance
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

            // Fallback to ensure content is visible
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

    #endregion

    #region PERFORMANCE OPTIMIZATION: Debounced Sorting Preservation

    /// <summary>
    /// PERFORMANCE OPTIMIZED: Hook into ViewModel's Items collection with throttled monitoring
    /// </summary>
    private void SetupSortingPreservation()
    {
        try
        {
            if (viewModel?.Items is ObservableCollection<TItemViewModel> observableItems)
            {
                observableItems.CollectionChanged += OnCollectionChanged;
                this.LogInfo("🔧 SORTING FIX: Collection change monitoring enabled");
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error setting up sorting preservation");
        }
    }

    /// <summary>
    /// PERFORMANCE OPTIMIZED: Throttled collection change handler
    /// </summary>
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        try
        {
            if (e.Action == NotifyCollectionChangedAction.Reset ||
                (e.Action == NotifyCollectionChangedAction.Add && e.NewStartingIndex == 0))
            {
                // Data was cleared and repopulated - schedule debounced sorting
                ScheduleDebouncedSorting();
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error in collection changed handler");
        }
    }

    /// <summary>
    /// PERFORMANCE OPTIMIZATION: Enhanced debounced sorting with better threading
    /// </summary>
    private void ScheduleDebouncedSorting()
    {
        lock (_sortingLock)
        {
            // Cancel existing timer if one is running
            _sortingDebounceTimer?.Dispose();

            // Only log if this is a new scheduling (not a replacement)
            if (!_isSortingScheduled)
            {
                this.LogInfo($"🔧 SORTING FIX: Scheduling debounced reapply of '{currentSortOrder}'");
                _isSortingScheduled = true;
            }

            // OPTIMIZED: Reduced debounce time for better responsiveness
            _sortingDebounceTimer = new Timer(async _ =>
            {
                await ExecuteDebouncedSorting();
            }, null, 300, Timeout.Infinite); // Reduced from 500ms to 300ms
        }
    }

    /// <summary>
    /// PERFORMANCE OPTIMIZATION: Execute sorting with optimized async handling
    /// </summary>
    private async Task ExecuteDebouncedSorting()
    {
        try
        {
            lock (_sortingLock)
            {
                _isSortingScheduled = false;
                _sortingDebounceTimer?.Dispose();
                _sortingDebounceTimer = null;
            }

            if (!string.IsNullOrEmpty(currentSortOrder) && listView != null)
            {
                this.LogInfo($"🔧 SORTING FIX: Executing debounced reapply of '{currentSortOrder}'");

                // OPTIMIZED: Use ConfigureAwait for better thread pool usage
                await Task.Run(async () =>
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ApplySyncfusionNativeSorting(currentSortOrder);
                        this.LogInfo($"✅ SORTING FIX: '{currentSortOrder}' reapplied successfully after debounce");
                    });
                }).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error executing debounced sorting");
        }
    }

    #endregion

    #region Property Change Handling

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        try
        {
            switch (e.PropertyName)
            {
                case nameof(viewModel.IsMultiSelectMode):
                    SyncSelectionMode();
                    break;
                case nameof(viewModel.SelectedItems):
                    UpdateFabVisual();
                    break;
                case nameof(viewModel.SortOrder):
                    ApplySyncfusionNativeSorting(viewModel.SortOrder);
                    break;
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, $"Error in ViewModel property changed handler for {e.PropertyName}");
        }
    }

    #endregion

    #region Multi-Select State Management - CRITICAL FIXES

    /// <summary>
    /// CRITICAL FIX: Force complete multi-select reset with enhanced visual cleanup
    /// </summary>
    private void ForceMultiSelectReset()
    {
        if (listView == null) return;

        try
        {
            this.LogInfo("🔧 MULTISELECT FIX: Starting complete reset");

            // 1. CRITICAL: Clear ListView selections FIRST and COMPLETELY
            if (listView.SelectedItems != null)
            {
                var selectedCount = listView.SelectedItems.Count;
                listView.SelectedItems.Clear();
                this.LogInfo($"🔧 Cleared {selectedCount} visual selections from ListView");
            }

            // 2. Reset all item states
            foreach (var item in viewModel.Items)
            {
                if (item.IsSelected)
                {
                    item.IsSelected = false;
                }
            }

            // 3. Clear ViewModel selection collection
            viewModel.SelectedItems.Clear();

            // 4. Exit multi-select mode
            viewModel.IsMultiSelectMode = false;

            // 5. Force ListView mode sync with delay to ensure visual reset
            listView.SelectionMode = SfSelectionMode.None;

            // 6. CRITICAL: Force visual refresh
            Task.Run(async () =>
            {
                await Task.Delay(50); // Small delay for visual state reset
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

            // 7. Update visual state
            UpdateFabVisual();

            this.LogInfo("🔧 MULTISELECT FIX: Complete reset performed successfully");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error in ForceMultiSelectReset");
        }
    }

    /// <summary>
    /// Debug method to check and auto-fix multi-select state inconsistencies
    /// UPDATED: Enhanced sync with visual validation
    /// </summary>
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

            // Auto-fix mode desync
            if (vmMode && listViewMode != SfSelectionMode.Multiple)
            {
                listView.SelectionMode = SfSelectionMode.Multiple;
                this.LogInfo("🔧 AUTO-FIX: Corrected ListView to Multiple mode");
            }
            else if (!vmMode && listViewMode != SfSelectionMode.None)
            {
                listView.SelectionMode = SfSelectionMode.None;
                listView.SelectedItems?.Clear(); // Ensure visual clear
                this.LogInfo("🔧 AUTO-FIX: Corrected ListView to None mode and cleared selections");
            }

            // ENHANCED: Better sync logic with visual validation
            if (vmMode && vmSelectedCount > 0 && listViewSelectedCount != vmSelectedCount)
            {
                // Force complete resync
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
                // Clear orphaned ListView selections
                listView.SelectedItems?.Clear();
                this.LogInfo("🔧 SYNC FIX: Cleared orphaned ListView selections");
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error in DebugAndFixMultiSelectState");
        }
    }

    #endregion

    #region Selection and Visual Sync

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

    #region Syncfusion Native Sorting - FIXED

    private void ApplySyncfusionNativeSorting(string sortOrder)
    {
        if (listView?.DataSource == null) return;

        try
        {
            this.LogInfo($"🔧 NATIVE SORT: Applying '{sortOrder}'");

            // CRITICAL FIX: Enable LiveDataUpdateMode for dynamic sorting
            listView.DataSource.LiveDataUpdateMode = LiveDataUpdateMode.AllowDataShaping;

            // Clear existing sort descriptors
            listView.DataSource.SortDescriptors.Clear();

            SortDescriptor sortDescriptor = sortOrder switch
            {
                "Name A→Z" => new SortDescriptor
                {
                    PropertyName = "Name",
                    Direction = ListSortDirection.Ascending,
                    Comparer = BaseListViewModel<T, TItemViewModel>.CaseInsensitiveComparer
                },
                "Name Z→A" => new SortDescriptor
                {
                    PropertyName = "Name",
                    Direction = ListSortDirection.Descending,
                    Comparer = BaseListViewModel<T, TItemViewModel>.CaseInsensitiveComparer
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
                "Favorites First" => new SortDescriptor { PropertyName = "IsFavorite", Direction = ListSortDirection.Descending },
                _ => new SortDescriptor
                {
                    PropertyName = "Name",
                    Direction = ListSortDirection.Ascending,
                    Comparer = BaseListViewModel<T, TItemViewModel>.CaseInsensitiveComparer
                }
            };

            listView.DataSource.SortDescriptors.Add(sortDescriptor);

            if (sortOrder == "Favorites First")
            {
                listView.DataSource.SortDescriptors.Add(new SortDescriptor
                {
                    PropertyName = "Name",
                    Direction = ListSortDirection.Ascending,
                    Comparer = BaseListViewModel<T, TItemViewModel>.CaseInsensitiveComparer
                });
            }

            // CRITICAL FIX: Force refresh to apply sorting
            listView.RefreshView();

            // CRITICAL FIX: Store current sort order for preservation
            currentSortOrder = sortOrder;

            this.LogInfo($"🔧 NATIVE SORT: Applied '{sortOrder}' successfully");
        }
        catch (Exception ex)
        {
            this.LogError(ex, $"Error applying native Syncfusion sorting for '{sortOrder}'");
        }
    }

    #endregion

    #region Lifecycle Management - 🔧 LOADING FLASH FIXED

    /// <summary>
    /// 🔧 LOADING FLASH FIXED: Enhanced BaseOnAppearing that coordinates with ViewModel's intelligent loading management
    /// No longer forces loading overlay - lets ViewModel decide based on operation type
    /// </summary>
    public async Task BaseOnAppearing()
    {
        try
        {
            _appearanceCount++;
            this.LogInfo($"🔄 BaseOnAppearing for {typeof(T).Name} (appearance #{_appearanceCount})");

            // 🔧 BUG FIX: Reset pull-to-refresh after tab navigation (known Syncfusion bug)
            if (_appearanceCount > 1 || _pullToRefreshNeedsReset)
            {
                _pullToRefreshNeedsReset = false;
                await ForceResetPullToRefresh();

                // CRITICAL FIX: Reset multi-select when returning from edit
                ForceMultiSelectReset();
                this.LogInfo("🔧 NAVIGATION FIX: Reset multi-select on return from edit");

                // Additional fix: Force a brief delay to ensure Syncfusion internal state resets
                await Task.Delay(150);
                this.LogInfo("🔧 PULL-TO-REFRESH FIX: Applied delay for internal state reset");
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

            // 🔧 LOADING FLASH FIX: Let ViewModel decide loading overlay based on operation type
            this.LogInfo($"🔧 LOADING FLASH FIX: Delegating loading decisions to ViewModel for {typeof(T).Name}");

            // Call ViewModel's OnAppearing - it will handle loading state intelligently
            await viewModel.OnAppearingAsync();

            // Apply sorting after data loads
            if (!string.IsNullOrEmpty(viewModel.SortOrder))
            {
                currentSortOrder = viewModel.SortOrder;
                ApplySyncfusionNativeSorting(viewModel.SortOrder);
            }

            this.LogInfo($"✅ LOADING FLASH FIX: BaseOnAppearing completed for {typeof(T).Name}");
        }
        catch (Exception ex)
        {
            this.LogError(ex, $"Error in BaseOnAppearing for {typeof(T).Name}");
            // Ensure loading is ALWAYS turned off on error
            viewModel.IsLoading = false;
        }
    }

    public void BaseOnDisappearing()
    {
        try
        {
            this.LogInfo($"🔄 BaseOnDisappearing for {typeof(T).Name}");

            // Set flag for pull-to-refresh reset on next appearance
            _pullToRefreshNeedsReset = true;

            if (viewModel != null)
            {
                viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            if (pullToRefresh != null)
            {
                pullToRefresh.Refreshing -= HandlePullToRefresh;
            }

            // PERFORMANCE OPTIMIZATION: Cleanup sorting timer
            lock (_sortingLock)
            {
                _sortingDebounceTimer?.Dispose();
                _sortingDebounceTimer = null;
                _isSortingScheduled = false;
            }

            this.LogInfo($"✅ BaseOnDisappearing completed for {typeof(T).Name}");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error in BaseOnDisappearing");
        }
    }

    /// <summary>
    /// ENHANCED: Entrance animation that coordinates with loading state
    /// </summary>
    private async Task PerformEntranceAnimation()
    {
        if (rootGrid == null) return;

        try
        {
            // Start invisible (loading overlay should be visible)
            rootGrid.Opacity = 0;

            // Quick delay then fade in
            await Task.Delay(50);
            await rootGrid.FadeTo(1, 250, Easing.CubicOut);

            this.LogInfo($"✨ ENHANCED: Entrance animation completed for {typeof(T).Name}");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error in entrance animation");
            // Fallback to visible
            rootGrid.Opacity = 1;
        }
    }

    #endregion

    #region Event Handlers - FIXED: No more ambiguity + Consistent Logging

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
                // FIXED: Handle multi-select with immediate visual sync
                item.IsSelected = !item.IsSelected;

                if (item.IsSelected && !viewModel.SelectedItems.Contains(item))
                {
                    viewModel.SelectedItems.Add(item);

                    // CRITICAL: Force ListView visual selection
                    if (listView?.SelectedItems != null && !listView.SelectedItems.Contains(item))
                    {
                        listView.SelectedItems.Add(item);
                    }
                }
                else if (!item.IsSelected)
                {
                    viewModel.SelectedItems.Remove(item);

                    // CRITICAL: Force ListView visual deselection
                    if (listView?.SelectedItems != null && listView.SelectedItems.Contains(item))
                    {
                        listView.SelectedItems.Remove(item);
                    }
                }

                this.LogInfo($"Multi-select tap: {item.Name} = {item.IsSelected}, ListView count: {listView?.SelectedItems?.Count ?? 0}");
            }
            else
            {
                // Normal navigation - delegate to ViewModel
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

            // Force ListView to Multiple mode immediately
            if (viewModel.IsMultiSelectMode && listView != null)
            {
                listView.SelectionMode = SfSelectionMode.Multiple;
                this.LogInfo("ListView forced to Multiple mode after long press");
            }

            UpdateFabVisual();
            // REMOVED: DebugAndFixMultiSelectState() calls to prevent interference
        }
    }

    public void HandleSelectionChanged(object? sender, ItemSelectionChangedEventArgs e)
    {
        try
        {
            if (listView?.SelectedItems != null && viewModel.SelectedItems != null)
            {
                // Sync ListView selections with ViewModel
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

            // ADDED: Debug after sync to catch issues
            DebugAndFixMultiSelectState();
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error syncing selections - forcing reset");
            ForceMultiSelectReset(); // Emergency reset on sync failure
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

                    // CRITICAL FIX: Reset multi-select after delete to prevent state corruption
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
            // FIXED: Use explicit command to avoid ambiguity
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
            // FIXED: Use explicit command to avoid ambiguity
            if (viewModel.SelectAllCommand.CanExecute(null))
            {
                viewModel.SelectAllCommand.Execute(null);
            }

            // Force ListView to show visual selections
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
            // FIXED: Use explicit command to avoid ambiguity
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

            // Check if filters are at default state
            var hasSearch = !string.IsNullOrWhiteSpace(viewModel.SearchText);
            var isDefaultFilter = viewModel.StatusFilter == "All" && !hasSearch;

            // Build main filter options
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
                isDefaultFilter ? null : "Clear All Filters",  // Destructive button only if not default
                [.. options]);

            if (result != null && result != "Cancel")
            {
                this.LogInfo($"🔧 FILTER: User selected '{result}'");

                // Apply filters with clear option
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

                // Apply the filter
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

            // Get current sort order
            var currentSort = viewModel.SortOrder;
            var isDefaultSort = currentSort == "Name A→Z";

            // Build options - add "Clear All Sorting" if not default
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
                isDefaultSort ? null : "Clear All Sorting",  // Destructive button
                [.. options.Take(5)]); // Only show main options in body

            if (result != null && result != "Cancel")
            {
                this.LogInfo($"🔧 SORT: User selected '{result}'");

                // Handle clear sorting
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

            // Get the SfPullToRefresh control
            if (sender is Syncfusion.Maui.PullToRefresh.SfPullToRefresh pullToRefreshControl)
            {
                try
                {
                    // Execute refresh command
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
                    // 🎯 CRITICAL: Always reset the refresh indicator
                    pullToRefreshControl.IsRefreshing = false;

                    // Also ensure ViewModel property is reset
                    viewModel.IsRefreshing = false;
                }
            }
        }, "Pull-to-refresh failed");
    }

    #endregion

    #region MIGRATED - Focus Handlers

    /// <summary>
    /// MIGRATED: Common empty handlers that all pages need
    /// </summary>
    public void HandleSearchFocused(object? sender, FocusEventArgs e) { /* Intentionally empty */ }
    public void HandleSearchUnfocused(object? sender, FocusEventArgs e) { /* Intentionally empty */ }
    public void HandleSwipeStarting(object? sender, Syncfusion.Maui.ListView.SwipeStartingEventArgs e) { /* Intentionally empty */ }
    public void HandleSwiping(object? sender, Syncfusion.Maui.ListView.SwipingEventArgs e) { /* Intentionally empty */ }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// PERFORMANCE OPTIMIZATION: Proper cleanup to prevent memory leaks
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Cleanup debounce timer
            lock (_sortingLock)
            {
                _sortingDebounceTimer?.Dispose();
                _sortingDebounceTimer = null;
                _isSortingScheduled = false;
            }

            // Remove event handlers
            if (viewModel != null)
            {
                viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            if (pullToRefresh != null)
            {
                pullToRefresh.Refreshing -= HandlePullToRefresh;
            }

            // Cleanup collection monitoring
            if (viewModel?.Items is ObservableCollection<TItemViewModel> observableItems)
            {
                observableItems.CollectionChanged -= OnCollectionChanged;
            }

            this.LogInfo($"BaseListPageLogic disposed for {typeof(T).Name}");
        }
    }

    #endregion
}