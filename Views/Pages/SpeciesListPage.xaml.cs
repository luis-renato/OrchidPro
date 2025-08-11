using OrchidPro.Constants;
using OrchidPro.Extensions;
using OrchidPro.ViewModels.Species;
using Syncfusion.Maui.ListView;
using SfSelectionMode = Syncfusion.Maui.ListView.SelectionMode;
using SfSwipeEndedEventArgs = Syncfusion.Maui.ListView.SwipeEndedEventArgs;

namespace OrchidPro.Views.Pages;

/// <summary>
/// OPTIMIZED Page for displaying and managing list of botanical species.
/// Reduced logging, efficient event handling, minimal SafeExecute usage.
/// </summary>
public partial class SpeciesListPage : ContentPage
{
    private readonly SpeciesListViewModel _viewModel;

    public SpeciesListPage(SpeciesListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        this.LogInfo("Initialized Species List Page");

        // Hook up events
        ListRefresh.Refreshing += PullToRefresh_Refreshing;

        // Monitor ViewModel changes to sync SelectionMode
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    #region ViewModel Synchronization (OPTIMIZED)

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_viewModel.IsMultiSelectMode))
        {
            SyncSelectionMode();
        }
    }

    /// <summary>
    /// OPTIMIZED SelectionMode sync - removed excessive logging
    /// </summary>
    private void SyncSelectionMode()
    {
        var targetMode = _viewModel.IsMultiSelectMode
            ? SfSelectionMode.Multiple
            : SfSelectionMode.None;

        if (SpeciesListView.SelectionMode != targetMode)
        {
            SpeciesListView.SelectionMode = targetMode;
        }
    }

    #endregion

    #region Page Lifecycle (OPTIMIZED)

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // OPTIMIZED - Removed SafeExecute wrapper for performance
        if (_viewModel?.SelectedItems == null)
        {
            this.LogError("ViewModel.SelectedItems is NULL during OnAppearing");
            return;
        }

        // Clear any existing selections
        SpeciesListView.SelectedItems?.Clear();

        UpdateFabVisual();

        this.LogInfo("Refreshing data on page appearing");
        await _viewModel.OnAppearingAsync();

        // Perform entrance animation
        await PerformEntranceAnimation();
    }

    /// <summary>
    /// OPTIMIZED entrance animation with reduced logging
    /// </summary>
    private async Task PerformEntranceAnimation()
    {
        RootGrid.Opacity = 0;
        await RootGrid.FadeTo(1, 300, Easing.CubicOut);
    }

    #endregion

    #region Search Events (OPTIMIZED)

    /// <summary>
    /// OPTIMIZED search text change handler
    /// </summary>
    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        // REMOVED SafeExecute wrapper and excessive logging for performance
        _viewModel.SearchText = e.NewTextValue ?? string.Empty;
    }

    private void OnSearchFocused(object? sender, FocusEventArgs e)
    {
        // Minimal implementation - no logging needed
    }

    private void OnSearchUnfocused(object? sender, FocusEventArgs e)
    {
        // Minimal implementation - no logging needed
    }

    #endregion

    #region ListView Events (OPTIMIZED)

    /// <summary>
    /// OPTIMIZED item tap handler
    /// </summary>
    private async void OnItemTapped(object? sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
    {
        if (e.DataItem is SpeciesItemViewModel item)
        {
            if (_viewModel.IsMultiSelectMode)
            {
                // Use the public command (synchronous)
                _viewModel.ItemLongPressCommand.Execute(item);
                UpdateFabVisual();
            }
            else
            {
                // Use the public command (asynchronous)
                await _viewModel.ItemTappedCommand.ExecuteAsync(item);
            }
        }
    }

    /// <summary>
    /// OPTIMIZED long press handler
    /// </summary>
    private void OnItemLongPress(object? sender, ItemLongPressEventArgs e)
    {
        if (e.DataItem is SpeciesItemViewModel item)
        {
            // Use the public command
            _viewModel.ItemLongPressCommand.Execute(item);
            UpdateFabVisual();
        }
    }

    /// <summary>
    /// OPTIMIZED selection change handler
    /// </summary>
    private void OnSelectionChanged(object? sender, ItemSelectionChangedEventArgs e)
    {
        UpdateFabVisual();
        SyncSelectionMode();
    }

    #endregion

    #region Swipe Events (OPTIMIZED)

    private void OnSwipeStarting(object? sender, SwipeStartingEventArgs e)
    {
        // Minimal implementation for swipe start
    }

    private void OnSwiping(object? sender, SwipingEventArgs e)
    {
        // Minimal implementation for swiping
    }

    private async void OnSwipeEnded(object? sender, SfSwipeEndedEventArgs e)
    {
        if (e.DataItem is not SpeciesItemViewModel item) return;

        if (e.Direction == SwipeDirection.Right)
        {
            // Favorite toggle - use public command
            await _viewModel.ToggleFavoriteCommand.ExecuteAsync(item);
        }
        else if (e.Direction == SwipeDirection.Left)
        {
            // Delete operation
            await _viewModel.DeleteSingleCommand.ExecuteAsync(item);
        }
    }

    #endregion

    #region Action Events (OPTIMIZED)

    private async void OnFabTapped(object? sender, EventArgs e)
    {
        if (_viewModel.SelectedItems.Any())
        {
            // Use the public command
            await _viewModel.DeleteSelectedCommand.ExecuteAsync(null);
        }
        else
        {
            // Navigate to add new species - construct the route manually
            await Shell.Current.GoToAsync(_viewModel.EditRoute);
        }
    }

    private void OnSelectAllTapped(object? sender, EventArgs e)
    {
        _viewModel.SelectAllCommand.Execute(null);
        UpdateFabVisual();
    }

    private void OnDeselectAllTapped(object? sender, EventArgs e)
    {
        _viewModel.DeselectAllCommand.Execute(null);
        UpdateFabVisual();
    }

    private async void OnFilterTapped(object? sender, EventArgs e)
    {
        // Show simple filter options
        var result = await DisplayActionSheet("Filter Options", "Cancel", null,
            "All Species", "Active Only", "Inactive Only", "Favorites Only");

        if (result != null && result != "Cancel")
        {
            switch (result)
            {
                case "All Species":
                    _viewModel.StatusFilter = "All";
                    break;
                case "Active Only":
                    _viewModel.StatusFilter = "Active";
                    break;
                case "Inactive Only":
                    _viewModel.StatusFilter = "Inactive";
                    break;
                case "Favorites Only":
                    // Use specific species method if available
                    break;
            }
            await _viewModel.ApplyFilterCommand.ExecuteAsync(null);
        }
    }

    private async void OnSortTapped(object? sender, EventArgs e)
    {
        // Show simple sort options
        var result = await DisplayActionSheet("Sort Options", "Cancel", null,
            "Name A→Z", "Name Z→A", "Recent First", "Oldest First", "Favorites First");

        if (result != null && result != "Cancel")
        {
            _viewModel.SortOrder = result;
            await _viewModel.ApplyFilterCommand.ExecuteAsync(null);
        }
    }

    #endregion

    #region Pull to Refresh (OPTIMIZED)

    private async void PullToRefresh_Refreshing(object? sender, EventArgs e)
    {
        // Use the public command
        await _viewModel.RefreshCommand.ExecuteAsync(null);
        ListRefresh.IsRefreshing = false;
    }

    #endregion

    #region FAB Visual Updates (OPTIMIZED)

    /// <summary>
    /// OPTIMIZED FAB visual update with minimal logging
    /// </summary>
    private void UpdateFabVisual()
    {
        var selectedCount = _viewModel.SelectedItems?.Count ?? 0;

        if (selectedCount > 0)
        {
            FabButton.Text = $"Delete ({selectedCount})";
            // Keep existing style for delete mode
        }
        else
        {
            FabButton.Text = "Add Species";
            // Keep existing style for add mode
        }
    }

    #endregion
}