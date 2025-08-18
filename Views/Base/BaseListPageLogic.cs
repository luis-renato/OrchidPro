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

namespace OrchidPro.Views.Pages.Base;

/// <summary>
/// PATCHED Logic class with FIXED IsLoading management for all navigation scenarios.
/// Ensures loading overlay appears consistently across all tabs and navigations.
/// </summary>
public class BaseListPageLogic<T, TItemViewModel>
    where T : class, IBaseEntity, new()
    where TItemViewModel : BaseItemViewModel<T>
{
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

    public BaseListPageLogic(BaseListViewModel<T, TItemViewModel> vm)
    {
        viewModel = vm;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        this.LogInfo($"Initialized BaseListPageLogic for {typeof(T).Name}");
    }

    /// <summary>
    /// ENHANCED: Setup page with fade effect support
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

        // Find the content and loading grids
        contentGrid = rootGrid?.FindByName<Grid>("ContentGrid");
        loadingGrid = rootGrid?.FindByName<Grid>("LoadingGrid");

        // Connect the Refreshing event
        if (pullToRefresh != null)
        {
            pullToRefresh.Refreshing += HandlePullToRefresh;
        }

        // Setup sorting preservation
        SetupSortingPreservation();

        // Monitor IsLoading for fade effects
        SetupFadeEffects();

        this.LogInfo($"🔧 Page setup completed for {typeof(T).Name} with fade effects");
    }

    /// <summary>
    /// FADE EFFECT: Monitor IsLoading changes and apply smooth transitions
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
    /// SMOOTH TRANSITION: Handle loading state changes with fade animation
    /// </summary>
    private async Task HandleLoadingStateChange()
    {
        try
        {
            if (viewModel.IsLoading)
            {
                // Starting to load: Hide everything, show loading
                this.LogInfo("✨ FADE: Starting loading state");

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

                if (loadingGrid != null)
                {
                    loadingGrid.IsVisible = true;
                    loadingGrid.Opacity = 1;
                }
            }
            else
            {
                // Finished loading: Hide loading, fade in content + FAB
                this.LogInfo("✨ FADE: Finishing loading state");

                if (loadingGrid != null)
                {
                    loadingGrid.IsVisible = false;
                }

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

    #region CRITICAL FIX: Sorting Preservation

    /// <summary>
    /// CRITICAL FIX: Hook into ViewModel's Items collection to detect changes
    /// This allows us to reapply sorting whenever data is refreshed
    /// </summary>
    private void SetupSortingPreservation()
    {
        try
        {
            if (viewModel?.Items is ObservableCollection<TItemViewModel> observableItems)
            {
                observableItems.CollectionChanged += (sender, e) =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Reset ||
                        (e.Action == NotifyCollectionChangedAction.Add && e.NewStartingIndex == 0))
                    {
                        // Data was cleared and repopulated - reapply sorting
                        PreserveSortingAfterDataChange();
                    }
                };

                this.LogInfo("🔧 SORTING FIX: Collection change monitoring enabled");
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error setting up sorting preservation");
        }
    }

    /// <summary>
    /// CRITICAL FIX: Preserve and reapply sorting after ItemsSource changes
    /// Syncfusion clears SortDescriptors when ItemsSource changes - we need to reapply
    /// </summary>
    private void PreserveSortingAfterDataChange()
    {
        try
        {
            // Get current sort order before it gets cleared
            var sortOrderToReapply = currentSortOrder;

            if (!string.IsNullOrEmpty(sortOrderToReapply) && listView != null)
            {
                this.LogInfo($"🔧 SORTING FIX: Reapplying '{sortOrderToReapply}' after data change");

                // Small delay to allow ItemsSource to settle
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(100); // Allow UI to settle
                    ApplySyncfusionNativeSorting(sortOrderToReapply);
                    this.LogInfo($"✅ SORTING FIX: '{sortOrderToReapply}' reapplied successfully");
                });
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error preserving sorting after data change");
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

    #region Lifecycle Management - CRITICAL LOADING FIX

    /// <summary>
    /// BALANCED FIX: Enhanced BaseOnAppearing with proper IsLoading management
    /// Shows loading briefly but ensures it's always turned off
    /// </summary>
    public async Task BaseOnAppearing()
    {
        try
        {
            this.LogInfo($"🔄 BALANCED FIX: BaseOnAppearing for {typeof(T).Name}");

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

            // BALANCED FIX: Only show loading if we expect actual loading time
            var shouldShowLoading = ShouldShowLoadingOverlay();

            if (shouldShowLoading)
            {
                this.LogInfo($"🔄 BALANCED: Showing loading overlay for {typeof(T).Name}");
                viewModel.IsLoading = true;

                // Small delay to show loading state
                await Task.Delay(150);
            }

            // Call ViewModel's OnAppearing
            await viewModel.OnAppearingAsync();

            // CRITICAL: Always ensure loading is turned off
            if (viewModel.IsLoading)
            {
                this.LogInfo($"🔄 BALANCED: Turning off loading overlay for {typeof(T).Name}");
                viewModel.IsLoading = false;
            }

            // Apply sorting after data loads
            if (!string.IsNullOrEmpty(viewModel.SortOrder))
            {
                currentSortOrder = viewModel.SortOrder;
                ApplySyncfusionNativeSorting(viewModel.SortOrder);
            }

            this.LogInfo($"✅ BALANCED: BaseOnAppearing completed for {typeof(T).Name}");
        }
        catch (Exception ex)
        {
            this.LogError(ex, $"Error in BaseOnAppearing for {typeof(T).Name}");
            // Ensure loading is ALWAYS turned off on error
            viewModel.IsLoading = false;
        }
    }

    /// <summary>
    /// SMART: Determine if we should show loading overlay based on data state
    /// </summary>
    private bool ShouldShowLoadingOverlay()
    {
        try
        {
            // Show loading if:
            // 1. No items yet (first load)
            // 2. Items exist but we're refreshing and it's been a while since last load

            var hasNoItems = viewModel.Items?.Count == 0;
            var isFirstAppearance = !hasAppearedOnce;

            // For subsequent visits, only show loading if no data or explicitly refreshing
            if (!isFirstAppearance && !hasNoItems)
            {
                this.LogInfo($"🤖 SMART: Skipping loading overlay - data already present");
                return false;
            }

            this.LogInfo($"🤖 SMART: Will show loading overlay - first load or no data");
            return true;
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error determining loading overlay state");
            return false; // Default to no loading on error
        }
    }

    public void BaseOnDisappearing()
    {
        if (viewModel != null)
        {
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (pullToRefresh != null)
        {
            pullToRefresh.Refreshing -= HandlePullToRefresh;
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
                // Immediate multi-select response
                item.IsSelected = !item.IsSelected;

                if (item.IsSelected && !viewModel.SelectedItems.Contains(item))
                {
                    viewModel.SelectedItems.Add(item);
                }
                else if (!item.IsSelected && viewModel.SelectedItems.Contains(item))
                {
                    viewModel.SelectedItems.Remove(item);
                }

                // Force ListView sync
                if (listView != null)
                {
                    if (item.IsSelected && !listView.SelectedItems.Contains(item))
                    {
                        listView.SelectedItems.Add(item);
                    }
                    else if (!item.IsSelected && listView.SelectedItems.Contains(item))
                    {
                        listView.SelectedItems.Remove(item);
                    }
                }

                this.LogInfo($"Multi-select tap: {item.Name} = {item.IsSelected}");
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
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error syncing selections");
        }

        UpdateFabVisual();
        SyncSelectionMode();
    }

    public async void HandleSwipeEnded(object? sender, SfSwipeEndedEventArgs e)
    {
        if (e.DataItem is not TItemViewModel item) return;

        try
        {
            if (e.Direction == SwipeDirection.Right)
            {
                // Favorite toggle - always executes
                if (viewModel.ToggleFavoriteCommand.CanExecute(item))
                {
                    await viewModel.ToggleFavoriteCommand.ExecuteAsync(item);
                }
            }
            else if (e.Direction == SwipeDirection.Left)
            {
                // Delete action - might be cancelled
                if (viewModel.DeleteSingleItemCommand.CanExecute(item))
                {
                    await viewModel.DeleteSingleItemCommand.ExecuteAsync(item);
                }
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, $"Error handling swipe action");
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
                options.ToArray());

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
                options.Take(5).ToArray()); // Only show main options in body

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
            this.LogInfo("🔄 CRITICAL FIX: Pull-to-refresh triggered with loading state");

            // CRITICAL FIX: Set loading state during refresh
            viewModel.IsLoading = true;

            if (pullToRefresh != null)
            {
                pullToRefresh.IsRefreshing = true;
            }

            // FIXED: Use explicit command to avoid ambiguity
            if (viewModel.RefreshCommand.CanExecute(null))
            {
                await viewModel.RefreshCommand.ExecuteAsync(null);
            }

            // Ensure loading is turned off
            viewModel.IsLoading = false;

            if (pullToRefresh != null)
            {
                pullToRefresh.IsRefreshing = false;
            }

            this.LogInfo("✅ CRITICAL FIX: Pull-to-refresh completed");
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
}