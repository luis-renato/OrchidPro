using OrchidPro.ViewModels.Species;
using OrchidPro.Constants;
using OrchidPro.Extensions;

namespace OrchidPro.Views.Pages;

/// <summary>
/// Page for displaying and managing list of botanical species with advanced UI features.
/// Provides CRUD operations, multi-selection, filtering, sorting, and swipe actions.
/// EXACT pattern from FamiliesListPage and GeneraListPage.
/// </summary>
public partial class SpeciesListPage : ContentPage
{
    private readonly SpeciesListViewModel _viewModel;

    /// <summary>
    /// Initialize the species list page with dependency injection and event binding
    /// </summary>
    public SpeciesListPage(SpeciesListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        this.LogInfo("Initialized with unified delete flow");

        // Hook up events - using correct names from XAML
        // ListRefresh.Refreshing += PullToRefresh_Refreshing; // Will add when XAML is fixed

        // Monitor ViewModel changes to sync SelectionMode
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        if (_viewModel?.SelectedItems == null)
        {
            this.LogError("ViewModel.SelectedItems is NULL during initialization");
        }
    }

    #region ViewModel Synchronization

    /// <summary>
    /// Synchronize ListView SelectionMode with ViewModel state changes
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_viewModel.IsMultiSelectMode))
        {
            this.LogInfo($"IsMultiSelectMode changed to: {_viewModel.IsMultiSelectMode}");
            SyncSelectionMode();
        }
    }

    /// <summary>
    /// Ensure ListView SelectionMode matches ViewModel state for consistency
    /// </summary>
    private void SyncSelectionMode()
    {
        this.SafeExecute(() =>
        {
            var targetMode = _viewModel.IsMultiSelectMode
                ? Syncfusion.Maui.ListView.SelectionMode.Multiple
                : Syncfusion.Maui.ListView.SelectionMode.None;

            if (SpeciesListView.SelectionMode != targetMode)
            {
                this.LogInfo($"Syncing SelectionMode: {SpeciesListView.SelectionMode} → {targetMode}");
                SpeciesListView.SelectionMode = targetMode;
                this.LogSuccess($"SelectionMode synced to: {SpeciesListView.SelectionMode}");
            }

            if (!_viewModel.IsMultiSelectMode && SpeciesListView.SelectedItems?.Count > 0)
            {
                this.LogInfo("Clearing ListView selections on multi-select exit");
                SpeciesListView.SelectedItems.Clear();
            }
        }, "SyncSelectionMode");
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handle pull-to-refresh gesture for data refreshing
    /// </summary>
    private async void PullToRefresh_Refreshing(object? sender, EventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Pull-to-refresh triggered");

            if (_viewModel?.RefreshCommand?.CanExecute(null) == true)
            {
                await _viewModel.RefreshCommand.ExecuteAsync(null);
                this.LogSuccess("Pull-to-refresh completed via ViewModel");
            }
            else
            {
                this.LogWarning("RefreshCommand not available or not executable");
            }
        }, "Pull-to-refresh failed");
    }

    /// <summary>
    /// Handle item taps for navigation to edit page
    /// </summary>
    private async void OnItemTapped(object sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (e.DataItem is not SpeciesItemViewModel item)
            {
                this.LogError("Tapped item is not SpeciesItemViewModel");
                return;
            }

            this.LogInfo($"Item tapped: {item.Name}");

            if (_viewModel?.IsMultiSelectMode == true)
            {
                this.LogInfo("Multi-select mode active, ignoring tap");
                return;
            }

            if (_viewModel?.NavigateToEditCommand?.CanExecute(item) == true)
            {
                await _viewModel.NavigateToEditCommand.ExecuteAsync(item);
                this.LogSuccess($"Navigated to edit for: {item.Name}");
            }
            else
            {
                this.LogWarning("NavigateToEditCommand not available");
            }
        }, "ItemTapped");
    }

    /// <summary>
    /// Handle long press to enter multi-select mode
    /// </summary>
    private void OnItemLongPress(object sender, Syncfusion.Maui.ListView.ItemLongPressEventArgs e)
    {
        this.SafeExecute(() =>
        {
            if (e.DataItem is SpeciesItemViewModel item)
            {
                this.LogInfo($"Long press on: {item.Name}");

                if (_viewModel != null && !_viewModel.IsMultiSelectMode)
                {
                    _viewModel.IsMultiSelectMode = true;
                    this.LogSuccess("Multi-select mode activated via long press");
                }
            }
        }, "ItemLongPress");
    }

    /// <summary>
    /// Handle selection changes for multi-select operations
    /// </summary>
    private void OnSelectionChanged(object sender, Syncfusion.Maui.ListView.ItemSelectionChangedEventArgs e)
    {
        this.SafeExecute(() =>
        {
            var selectedCount = SpeciesListView?.SelectedItems?.Count ?? 0;
            this.LogInfo($"NATIVE Selection changed - ListView count: {selectedCount}");

            if (SpeciesListView.SelectedItems != null && _viewModel?.SelectedItems != null)
            {
                _viewModel.SelectedItems.Clear();

                foreach (SpeciesItemViewModel item in SpeciesListView.SelectedItems)
                {
                    _viewModel.SelectedItems.Add(item);
                    item.IsSelected = true;
                }

                this.LogSuccess($"Synced to ViewModel: {_viewModel.SelectedItems.Count} items");
            }

            if (_viewModel != null)
            {
                var vmSelectedCount = _viewModel.SelectedItems?.Count ?? 0;

                if (vmSelectedCount == 0 && _viewModel.IsMultiSelectMode)
                {
                    this.LogInfo("Auto-exiting multi-select mode");
                    _viewModel.IsMultiSelectMode = false;
                }
                else if (vmSelectedCount > 0 && !_viewModel.IsMultiSelectMode)
                {
                    _viewModel.IsMultiSelectMode = true;
                }

                UpdateFabVisual();

                this.LogInfo($"ViewModel updated - MultiSelect: {_viewModel.IsMultiSelectMode}, SelectionMode: {SpeciesListView.SelectionMode}");
            }

            if (e.AddedItems?.Count > 0)
            {
                foreach (SpeciesItemViewModel item in e.AddedItems)
                {
                    this.LogInfo($"NATIVE Selected: {item.Name}");
                }
            }

            if (e.RemovedItems?.Count > 0)
            {
                foreach (SpeciesItemViewModel item in e.RemovedItems)
                {
                    item.IsSelected = false;
                    this.LogInfo($"NATIVE Deselected: {item.Name}");
                }
            }
        }, "SelectionChanged error");
    }

    #endregion

    #region Swipe Action Handlers

    private const double SWIPE_THRESHOLD = 0.8;

    /// <summary>
    /// Handle swipe gesture start with logging
    /// </summary>
    private void OnSwipeStarting(object sender, Syncfusion.Maui.ListView.SwipeStartingEventArgs e)
    {
        this.SafeExecute(() =>
        {
            if (e.DataItem is SpeciesItemViewModel item)
            {
                this.LogInfo($"Swipe starting for {item.Name} - direction: {e.Direction}");
            }
        }, "SwipeStarting error");
    }

    /// <summary>
    /// Handle swipe progress with real-time feedback
    /// </summary>
    private void OnSwiping(object sender, Syncfusion.Maui.ListView.SwipingEventArgs e)
    {
        this.SafeExecute(() =>
        {
            if (e.DataItem is SpeciesItemViewModel item)
            {
                var offsetPercent = Math.Abs(e.Offset) / SpeciesListView.SwipeOffset;
                var direction = e.Direction.ToString();
                var icon = direction == "Right" ? "⭐" : "🗑️";

                if (offsetPercent >= 0.8)
                {
                    this.LogInfo($"{icon} {item.Name} | {direction} | {offsetPercent:P0} - READY!");
                }
            }
        }, "Swiping error");
    }

    /// <summary>
    /// Handle swipe completion with unified action flow
    /// </summary>
    private async void OnSwipeEnded(object sender, Syncfusion.Maui.ListView.SwipeEndedEventArgs e)
    {
        var success = await this.SafeExecuteAsync(async () =>
        {
            if (e.DataItem is not SpeciesItemViewModel item)
            {
                this.LogError("DataItem is not SpeciesItemViewModel");
                return;
            }

            var direction = e.Direction.ToString();
            var offsetPercent = Math.Abs(e.Offset) / SpeciesListView.SwipeOffset;
            var icon = direction == "Right" ? "⭐" : "🗑️";

            this.LogInfo($"{icon} Item: {item.Name}, Direction: {direction}, Progress: {offsetPercent:P1}");

            if (offsetPercent < SWIPE_THRESHOLD)
            {
                this.LogWarning($"INSUFFICIENT SWIPE - Need {SWIPE_THRESHOLD:P0}+, got {offsetPercent:P1}");
                SpeciesListView.ResetSwipeItem();
                return;
            }

            this.LogSuccess($"SWIPE APPROVED! Executing {icon} {direction} action");

            switch (direction)
            {
                case "Right":
                    this.LogInfo("FAVORITE action triggered");

                    if (_viewModel?.IsConnected != true)
                    {
                        await this.ShowWarningToast("Cannot favorite while offline");
                        break;
                    }

                    await this.SafeExecuteAsync(async () =>
                    {
                        var wasAlreadyFavorite = item.IsFavorite;

                        if (_viewModel?.ToggleFavoriteCommand?.CanExecute(item) == true)
                        {
                            await _viewModel.ToggleFavoriteCommand.ExecuteAsync(item);
                        }

                        var message = wasAlreadyFavorite ? "Removed from favorites" : "Added to favorites";
                        await this.ShowSuccessToast($"{item.Name}: {message}");

                    }, "Toggle Favorite");
                    break;

                case "Left":
                    this.LogInfo("DELETE action triggered");

                    if (_viewModel?.IsConnected != true)
                    {
                        await this.ShowWarningToast("Cannot delete while offline");
                        break;
                    }

                    if (_viewModel?.DeleteSingleCommand?.CanExecute(item) == true)
                    {
                        await _viewModel.DeleteSingleCommand.ExecuteAsync(item);
                    }
                    break;

                default:
                    this.LogWarning($"Unknown swipe direction: {direction}");
                    break;
            }

            SpeciesListView.ResetSwipeItem();

        }, "SwipeEnded");

        if (!success)
        {
            SpeciesListView.ResetSwipeItem();
        }
    }

    #endregion

    #region Toolbar and FAB Handlers

    /// <summary>
    /// Handle select all toolbar action
    /// </summary>
    private void OnSelectAllTapped(object sender, EventArgs e)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo("Select All tapped");

            if (_viewModel?.SelectAllCommand?.CanExecute(null) == true)
            {
                _viewModel.SelectAllCommand.Execute(null);
                this.LogSuccess("Select All executed");
            }
        }, "OnSelectAllTapped");
    }

    /// <summary>
    /// Handle deselect all toolbar action
    /// </summary>
    private void OnDeselectAllTapped(object sender, EventArgs e)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo("Deselect All tapped");

            if (_viewModel?.DeselectAllCommand?.CanExecute(null) == true)
            {
                _viewModel.DeselectAllCommand.Execute(null);
                this.LogSuccess("Deselect All executed");
            }
        }, "OnDeselectAllTapped");
    }

    /// <summary>
    /// Handle FAB click for adding new species or bulk operations
    /// </summary>
    private async void OnFabTapped(object sender, EventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("FAB clicked");

            if (_viewModel?.IsMultiSelectMode == true)
            {
                // Bulk delete mode
                if (_viewModel?.DeleteSelectedCommand?.CanExecute(null) == true)
                {
                    await _viewModel.DeleteSelectedCommand.ExecuteAsync(null);
                    this.LogSuccess("Bulk delete executed");
                }
            }
            else
            {
                // Add new species
                if (_viewModel?.NavigateToAddCommand?.CanExecute(null) == true)
                {
                    await _viewModel.NavigateToAddCommand.ExecuteAsync(null);
                    this.LogSuccess("Navigate to add executed");
                }
            }
        }, "OnFabTapped");
    }

    /// <summary>
    /// Update FAB visual state based on selection mode
    /// </summary>
    private void UpdateFabVisual()
    {
        this.SafeExecute(() =>
        {
            if (_viewModel?.IsMultiSelectMode == true)
            {
                FabButton.Text = "🗑️ Delete Selected";
                FabButton.BackgroundColor = Colors.Red;
            }
            else
            {
                FabButton.Text = "Add Species";
                // Reset to default primary color
            }
        }, "UpdateFabVisual");
    }

    #endregion

    #region Search and Filter Handlers

    /// <summary>
    /// Handle search text changes with debounce
    /// </summary>
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        this.SafeExecute(() =>
        {
            if (_viewModel != null)
            {
                _viewModel.SearchText = e.NewTextValue ?? string.Empty;
                this.LogInfo($"Search text changed: '{e.NewTextValue}'");
            }
        }, "OnSearchTextChanged");
    }

    /// <summary>
    /// Handle search focus for UI feedback
    /// </summary>
    private void OnSearchFocused(object sender, FocusEventArgs e)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo("Search focused");
            // Add visual feedback if needed
        }, "OnSearchFocused");
    }

    /// <summary>
    /// Handle search unfocus for UI cleanup
    /// </summary>
    private void OnSearchUnfocused(object sender, FocusEventArgs e)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo("Search unfocused");
            // Cleanup if needed
        }, "OnSearchUnfocused");
    }

    /// <summary>
    /// Handle filter button tap
    /// </summary>
    private void OnFilterTapped(object sender, EventArgs e)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo("Filter tapped");
            // Implement filter UI if needed
        }, "OnFilterTapped");
    }

    /// <summary>
    /// Handle sort button tap
    /// </summary>
    private void OnSortTapped(object sender, EventArgs e)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo("Sort tapped");

            // Use NextSortOrderCommand if it exists, or implement sort cycling
            if (_viewModel?.NextSortOrderCommand?.CanExecute(null) == true)
            {
                _viewModel.NextSortOrderCommand.Execute(null);
                this.LogSuccess("Sort order changed");
            }
        }, "OnSortTapped");
    }

    #endregion

    #region Page Lifecycle

    /// <summary>
    /// Handle page appearing with data refresh and animations
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Page appearing");

            // Animate entrance
            await RootGrid.PerformStandardEntranceAsync();

            // Refresh data
            if (_viewModel?.OnAppearingAsync != null)
            {
                await _viewModel.OnAppearingAsync();
            }

            this.LogSuccess("Page appeared successfully");
        }, "OnAppearing");
    }

    /// <summary>
    /// Handle page disappearing with cleanup
    /// </summary>
    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Page disappearing");

            // Cleanup
            if (_viewModel?.OnDisappearingAsync != null)
            {
                await _viewModel.OnDisappearingAsync();
            }

            this.LogSuccess("Page disappeared successfully");
        }, "OnDisappearing");
    }

    #endregion
}