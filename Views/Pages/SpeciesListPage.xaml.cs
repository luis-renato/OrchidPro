using OrchidPro.Constants;
using OrchidPro.Extensions;
using OrchidPro.ViewModels.Species;
using Syncfusion.Maui.ListView;
using SfSelectionMode = Syncfusion.Maui.ListView.SelectionMode;
using SfSwipeEndedEventArgs = Syncfusion.Maui.ListView.SwipeEndedEventArgs;

namespace OrchidPro.Views.Pages;

/// <summary>
/// FIXED Species List Page with proper EmptyState and loading sequence
/// </summary>
public partial class SpeciesListPage : ContentPage
{
    private readonly SpeciesListViewModel _viewModel;
    private bool _hasAppearedOnce = false;

    public SpeciesListPage(SpeciesListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        this.LogInfo("Initialized Species List Page");

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
        else if (e.PropertyName == nameof(_viewModel.IsRefreshing))
        {
            // Ensure pull-to-refresh UI is in sync
            if (ListRefresh.IsRefreshing != _viewModel.IsRefreshing)
            {
                ListRefresh.IsRefreshing = _viewModel.IsRefreshing;
            }
        }
        // CRITICAL FIX: Sync when SelectedItems collection changes
        else if (e.PropertyName == nameof(_viewModel.SelectedItems))
        {
            SyncAllSelectionsWithListView();
            UpdateFabVisual();
        }
    }

    /// <summary>
    /// OPTIMIZED SelectionMode sync
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

    #region Page Lifecycle (CRITICAL FIX)

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_viewModel?.SelectedItems == null)
        {
            this.LogError("ViewModel.SelectedItems is NULL during OnAppearing");
            return;
        }

        // Clear any existing selections
        SpeciesListView.SelectedItems?.Clear();
        UpdateFabVisual();

        this.LogInfo("Refreshing data on page appearing");

        // CRITICAL FIX: Show entrance animation FIRST (before loading)
        if (!_hasAppearedOnce)
        {
            await PerformEntranceAnimation();
            _hasAppearedOnce = true;
        }

        // Then load data (this will show loading overlay if needed)
        await _viewModel.OnAppearingAsync();
    }

    /// <summary>
    /// FIXED entrance animation - happens BEFORE loading
    /// </summary>
    private async Task PerformEntranceAnimation()
    {
        // Start invisible
        RootGrid.Opacity = 0;

        // Small delay to ensure layout is ready
        await Task.Delay(50);

        // Smooth fade in
        await RootGrid.FadeTo(1, 250, Easing.CubicOut);
    }

    #endregion

    #region Search Events (OPTIMIZED)

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _viewModel.SearchText = e.NewTextValue ?? string.Empty;
    }

    private void OnSearchFocused(object? sender, FocusEventArgs e)
    {
        // Minimal implementation
    }

    private void OnSearchUnfocused(object? sender, FocusEventArgs e)
    {
        // Minimal implementation
    }

    #endregion

    #region ListView Events (OPTIMIZED)

    private async void OnItemTapped(object? sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
    {
        if (e.DataItem is SpeciesItemViewModel item)
        {
            if (_viewModel.IsMultiSelectMode)
            {
                // In multiselect mode: manually toggle selection and sync with ListView
                item.IsSelected = !item.IsSelected;

                if (item.IsSelected)
                {
                    if (!_viewModel.SelectedItems.Contains(item))
                    {
                        _viewModel.SelectedItems.Add(item);
                    }
                    if (!SpeciesListView.SelectedItems.Contains(item))
                    {
                        SpeciesListView.SelectedItems.Add(item);
                    }
                }
                else
                {
                    _viewModel.SelectedItems.Remove(item);
                    SpeciesListView.SelectedItems.Remove(item);
                }

                UpdateFabVisual();
            }
            else
            {
                // Normal mode: use ViewModel command for navigation
                await _viewModel.ItemTappedCommand.ExecuteAsync(item);
            }
        }
    }

    private void OnItemLongPress(object? sender, ItemLongPressEventArgs e)
    {
        if (e.DataItem is SpeciesItemViewModel item)
        {
            _viewModel.ItemLongPressCommand.Execute(item);
            UpdateFabVisual();
        }
    }

    private void OnSelectionChanged(object? sender, ItemSelectionChangedEventArgs e)
    {
        UpdateFabVisual();
        SyncSelectionMode();
    }

    #endregion

    #region Swipe Events (OPTIMIZED)

    private void OnSwipeStarting(object? sender, SwipeStartingEventArgs e)
    {
        // Minimal implementation
    }

    private void OnSwiping(object? sender, SwipingEventArgs e)
    {
        // Minimal implementation
    }

    private async void OnSwipeEnded(object? sender, SfSwipeEndedEventArgs e)
    {
        if (e.DataItem is not SpeciesItemViewModel item) return;

        if (e.Direction == SwipeDirection.Right)
        {
            await _viewModel.ToggleFavoriteCommand.ExecuteAsync(item);
        }
        else if (e.Direction == SwipeDirection.Left)
        {
            await _viewModel.DeleteSingleCommand.ExecuteAsync(item);
        }
    }

    #endregion

    #region Action Events (OPTIMIZED)

    private async void OnFabTapped(object? sender, EventArgs e)
    {
        if (_viewModel.SelectedItems.Any())
        {
            await _viewModel.DeleteSelectedCommand.ExecuteAsync(null);
        }
        else
        {
            await Shell.Current.GoToAsync(_viewModel.EditRoute);
        }
    }

    private void OnSelectAllTapped(object? sender, EventArgs e)
    {
        _viewModel.SelectAllCommand.Execute(null);

        // CRITICAL FIX: Sync Syncfusion ListView with ViewModel selections
        SyncAllSelectionsWithListView();

        UpdateFabVisual();
    }

    private void OnDeselectAllTapped(object? sender, EventArgs e)
    {
        _viewModel.DeselectAllCommand.Execute(null);

        // CRITICAL FIX: Clear Syncfusion ListView selections
        SpeciesListView.SelectedItems?.Clear();

        UpdateFabVisual();
    }

    private async void OnFilterTapped(object? sender, EventArgs e)
    {
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
                    _viewModel.StatusFilter = "All";
                    _viewModel.SortOrder = "Favorites First";
                    break;
            }
            await _viewModel.ApplyFilterCommand.ExecuteAsync(null);
        }
    }

    private async void OnSortTapped(object? sender, EventArgs e)
    {
        var result = await DisplayActionSheet("Sort Options", "Cancel", null,
            "Name A→Z", "Name Z→A", "Recent First", "Oldest First", "Favorites First");

        if (result != null && result != "Cancel")
        {
            _viewModel.SortOrder = result;
            await _viewModel.ApplyFilterCommand.ExecuteAsync(null);
        }
    }

    #endregion

    #region Pull to Refresh (CRITICAL FIX)

    /// <summary>
    /// CRITICAL FIX: Proper pull-to-refresh handling with guaranteed reset
    /// </summary>
    private async void PullToRefresh_Refreshing(object? sender, EventArgs e)
    {
        try
        {
            // Call the custom refresh method
            await _viewModel.RefreshSpeciesAsync();
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error during pull-to-refresh");
        }
        finally
        {
            // CRITICAL FIX: Always ensure pull-to-refresh is reset
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                try
                {
                    if (ListRefresh.IsRefreshing)
                    {
                        ListRefresh.IsRefreshing = false;
                    }

                    // Extra safety: Reset ViewModel refresh state too
                    if (_viewModel.IsRefreshing)
                    {
                        // Force reset if needed
                        _viewModel.GetType()
                            .GetProperty("IsRefreshing")?
                            .SetValue(_viewModel, false);
                    }
                }
                catch (Exception resetEx)
                {
                    this.LogError(resetEx, "Error resetting refresh state");
                }
            });
        }
    }

    #endregion

    #region Selection Synchronization (CRITICAL FIX)

    /// <summary>
    /// CRITICAL FIX: Sync all ViewModel selections with Syncfusion ListView
    /// </summary>
    private void SyncAllSelectionsWithListView()
    {
        try
        {
            // Clear current ListView selections
            SpeciesListView.SelectedItems?.Clear();

            // Add all ViewModel selected items to ListView
            if (_viewModel.SelectedItems?.Any() == true)
            {
                foreach (var selectedItem in _viewModel.SelectedItems)
                {
                    if (!SpeciesListView.SelectedItems.Contains(selectedItem))
                    {
                        SpeciesListView.SelectedItems.Add(selectedItem);
                    }
                }
            }

            this.LogInfo($"Synced {_viewModel.SelectedItems?.Count ?? 0} selections with ListView");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error syncing selections with ListView");
        }
    }

    #endregion

    private void UpdateFabVisual()
    {
        var selectedCount = _viewModel.SelectedItems?.Count ?? 0;

        if (selectedCount > 0)
        {
            FabButton.Text = $"Delete ({selectedCount})";
        }
        else
        {
            FabButton.Text = "Add Species";
        }
    }
}