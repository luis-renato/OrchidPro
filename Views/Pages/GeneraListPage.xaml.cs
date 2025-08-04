using OrchidPro.ViewModels.Genera;
using OrchidPro.Constants;
using OrchidPro.Extensions;

namespace OrchidPro.Views.Pages;

/// <summary>
/// Page for displaying and managing list of botanical genera with advanced UI features.
/// Provides CRUD operations, multi-selection, filtering, sorting, and swipe actions.
/// </summary>
public partial class GeneraListPage : ContentPage
{
    private readonly GeneraListViewModel _viewModel;

    /// <summary>
    /// Initialize the genera list page with dependency injection and event binding
    /// </summary>
    public GeneraListPage(GeneraListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        this.LogInfo("Initialized with unified delete flow");

        // Hook up events
        ListRefresh.Refreshing += PullToRefresh_Refreshing;

        // Monitor ViewModel changes to sync SelectionMode
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        if (_viewModel?.SelectedItems == null)
        {
            this.LogError("ViewModel.SelectedItems is NULL during initialization");
        }
    }

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

            if (GeneraListView.SelectionMode != targetMode)
            {
                this.LogInfo($"Syncing SelectionMode: {GeneraListView.SelectionMode} → {targetMode}");
                GeneraListView.SelectionMode = targetMode;
                this.LogSuccess($"SelectionMode synced to: {GeneraListView.SelectionMode}");
            }

            if (!_viewModel.IsMultiSelectMode && GeneraListView.SelectedItems?.Count > 0)
            {
                this.LogInfo("Clearing ListView selections on multi-select exit");
                GeneraListView.SelectedItems.Clear();
            }
        }, "SyncSelectionMode");
    }

    /// <summary>
    /// Handle pull-to-refresh gesture for data refreshing
    /// </summary>
    private async void PullToRefresh_Refreshing(object? sender, EventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Pull to refresh triggered");

            if (_viewModel?.RefreshCommand?.CanExecute(null) == true)
            {
                await _viewModel.RefreshCommand.ExecuteAsync(null);
            }

            ListRefresh.IsRefreshing = false;
            this.LogSuccess("Pull to refresh completed");
        }, "Pull to refresh error");
    }

    /// <summary>
    /// Handle page appearing - load data and setup
    /// </summary>
    protected override async void OnAppearing()
    {
        using (this.LogPerformance("GeneraListPage OnAppearing"))
        {
            try
            {
                base.OnAppearing();

                this.LogInfo("GeneraListPage appearing");

                // Ensure ViewModel is available
                if (_viewModel == null)
                {
                    this.LogError("ViewModel is null in OnAppearing");
                    return;
                }

                // Fade in animation like FamiliesListPage
                await RootGrid.FadeTo(1, 500);

                // Load data
                await _viewModel.OnAppearingAsync();

                this.LogInfo("GeneraListPage appeared successfully");
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Error in GeneraListPage OnAppearing");
            }
        }
    }

    /// <summary>
    /// Handle page disappearing - cleanup if needed
    /// </summary>
    protected override void OnDisappearing()
    {
        try
        {
            base.OnDisappearing();
            this.LogInfo("GeneraListPage disappearing");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error in GeneraListPage OnDisappearing");
        }
    }

    /// <summary>
    /// Handle select all toolbar item with proper SelectionMode synchronization
    /// </summary>
    private void OnSelectAllTapped(object sender, EventArgs e)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo("Select All toolbar tapped");

            if (GeneraListView.SelectionMode != Syncfusion.Maui.ListView.SelectionMode.Multiple)
            {
                this.LogInfo("Setting ListView to Multiple mode for Select All");
                GeneraListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.Multiple;
            }

            if (_viewModel?.SelectAllCommand?.CanExecute(null) == true)
            {
                _viewModel.SelectAllCommand.Execute(null);
                this.LogSuccess("SelectAllCommand executed");
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                this.SafeExecute(() =>
                {
                    GeneraListView.SelectedItems?.Clear();

                    foreach (var item in _viewModel.Items)
                    {
                        if (item.IsSelected && GeneraListView.SelectedItems != null)
                        {
                            GeneraListView.SelectedItems.Add(item);
                            this.LogInfo($"Added {item.Name} to ListView selection");
                        }
                    }

                    for (int i = 0; i < _viewModel.Items.Count; i++)
                    {
                        GeneraListView.RefreshItem(i);
                    }

                    UpdateFabVisual();
                }, "ListView sync error");
            });
        }, "Select All toolbar error");
    }

    /// <summary>
    /// Handle clear all selections with proper cleanup and user feedback
    /// </summary>
    private async void OnDeselectAllTapped(object sender, EventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Clear All toolbar tapped");

            if (_viewModel?.ClearSelectionCommand != null && _viewModel.ClearSelectionCommand.CanExecute(null))
            {
                _viewModel.ClearSelectionCommand.Execute(null);
                this.LogSuccess("ClearSelectionCommand executed");
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                this.SafeExecute(() =>
                {
                    GeneraListView.SelectedItems?.Clear();
                    GeneraListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.None;

                    if (_viewModel?.Items != null)
                    {
                        for (int i = 0; i < _viewModel.Items.Count; i++)
                        {
                            GeneraListView.RefreshItem(i);
                        }
                    }

                    UpdateFabVisual();
                }, "ListView clear error");
            });

            await this.ShowSuccessToast("🧹 Cleared selections and filters");

            this.LogSuccess("Clear All completed successfully");
        }, "Clear All failed");
    }

    /// <summary>
    /// Update FAB appearance based on selection state with proper error handling
    /// </summary>
    private void UpdateFabVisual()
    {
        var success = this.SafeExecute(() =>
        {
            var selectedCount = _viewModel?.SelectedItems?.Count ?? 0;
            this.LogInfo($"Updating FAB for {selectedCount} selected items");

            Device.BeginInvokeOnMainThread(() =>
            {
                FabButton.IsVisible = true;

                if (selectedCount > 0)
                {
                    var errorColor = Application.Current?.Resources.TryGetValue("ErrorColor", out var error) == true
                        ? (Color)error
                        : Color.FromArgb("#F44336");

                    FabButton.BackgroundColor = errorColor;
                    FabButton.Text = $"Delete ({selectedCount})";
                    this.LogInfo($"Set to DELETE mode: Delete ({selectedCount})");
                }
                else if (_viewModel?.IsMultiSelectMode == true)
                {
                    var grayColor = Application.Current?.Resources.TryGetValue("Gray500", out var gray) == true
                        ? (Color)gray
                        : Color.FromArgb("#9E9E9E");

                    FabButton.BackgroundColor = grayColor;
                    FabButton.Text = "Cancel";
                    this.LogInfo("Set to CANCEL mode");
                }
                else
                {
                    var primaryColor = Application.Current?.Resources.TryGetValue("Primary", out var primary) == true
                        ? (Color)primary
                        : Color.FromArgb("#A47764");

                    FabButton.BackgroundColor = primaryColor;
                    FabButton.Text = "Add Genus";
                    this.LogInfo("Set to ADD mode");
                }

                this.LogSuccess($"FAB updated successfully - Text: {FabButton.Text}");
            });
        }, "UpdateFabVisual");

        if (!success)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                FabButton.IsVisible = true;
                FabButton.Text = "Add Genus";
                FabButton.BackgroundColor = Color.FromArgb("#A47764");
            });
        }
    }

    /// <summary>
    /// Handle search text changes for real-time filtering
    /// </summary>
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"Search text changed to: '{e.NewTextValue}'");
        }, "Search text changed error");
    }

    /// <summary>
    /// Handle search focused event
    /// </summary>
    private void OnSearchFocused(object sender, FocusEventArgs e)
    {
        try
        {
            this.LogInfo("Search focused");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error in search focused");
        }
    }

    /// <summary>
    /// Handle search unfocused event
    /// </summary>
    private void OnSearchUnfocused(object sender, FocusEventArgs e)
    {
        try
        {
            this.LogInfo("Search unfocused");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error in search unfocused");
        }
    }

    /// <summary>
    /// Handle filter button tapped
    /// </summary>
    private async void OnFilterTapped(object sender, EventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (sender is Border border)
            {
                await border.PerformTapFeedbackAsync();
            }

            string[] options = { "All", "Active", "Inactive" };
            string result = await DisplayActionSheet("Filter by Status", "Cancel", null, options);

            if (result != "Cancel" && result != null && _viewModel != null)
            {
                _viewModel.StatusFilter = result;
                this.LogInfo($"Status filter changed to: {result}");

                if (_viewModel.ApplyFilterCommand?.CanExecute(null) == true)
                {
                    await _viewModel.ApplyFilterCommand.ExecuteAsync(null);
                }

                await this.ShowInfoToast($"Filter: {result}");
            }
        }, "Filter error");
    }

    /// <summary>
    /// Handle sort button tapped
    /// </summary>
    private async void OnSortTapped(object sender, EventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (sender is Border border)
            {
                await border.PerformTapFeedbackAsync();
            }

            string[] options = { "Name A→Z", "Name Z→A", "Recent First", "Oldest First", "Favorites First" };
            string result = await DisplayActionSheet("Sort by", "Cancel", null, options);

            if (result != "Cancel" && result != null && _viewModel != null)
            {
                _viewModel.SortOrder = result;
                this.LogInfo($"Sort order changed to: {result}");
            }
        }, "Sort error");
    }

    /// <summary>
    /// Handle item tap for selection or navigation based on current mode
    /// </summary>
    private void OnItemTapped(object sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
    {
        this.SafeExecute(() =>
        {
            if (e.DataItem is GenusItemViewModel item)
            {
                this.LogInfo($"Item tapped: {item.Name}");

                if (_viewModel?.IsMultiSelectMode == true)
                {
                    this.LogInfo("Multi-select mode - toggling selection");

                    if (item.IsSelected)
                    {
                        item.IsSelected = false;
                        _viewModel?.SelectedItems?.Remove(item);
                        GeneraListView.SelectedItems?.Remove(item);
                        this.LogInfo($"Deselected: {item.Name}");
                    }
                    else
                    {
                        item.IsSelected = true;
                        if (_viewModel?.SelectedItems != null && !_viewModel.SelectedItems.Contains(item))
                            _viewModel.SelectedItems.Add(item);
                        if (GeneraListView.SelectedItems != null && !GeneraListView.SelectedItems.Contains(item))
                            GeneraListView.SelectedItems.Add(item);
                        this.LogInfo($"Selected: {item.Name}");
                    }

                    UpdateFabVisual();
                }
                else
                {
                    this.LogInfo("Single-select mode - navigating to edit");
                    if (_viewModel?.NavigateToEditCommand?.CanExecute(item) == true)
                    {
                        _viewModel.NavigateToEditCommand.Execute(item);
                    }
                }
            }
        }, "ItemTapped error");
    }

    /// <summary>
    /// Handle item long press to enter multi-select mode
    /// </summary>
    private void OnItemLongPress(object sender, Syncfusion.Maui.ListView.ItemLongPressEventArgs e)
    {
        this.SafeExecute(() =>
        {
            if (e.DataItem is GenusItemViewModel item)
            {
                this.LogInfo($"Long press on: {item.Name}");

                if (_viewModel?.IsMultiSelectMode != true)
                {
                    this.LogInfo("Entering multi-select mode");
                    _viewModel.IsMultiSelectMode = true;
                }

                if (!item.IsSelected)
                {
                    item.IsSelected = true;
                    _viewModel?.SelectedItems?.Add(item);
                    GeneraListView.SelectedItems?.Add(item);
                    this.LogInfo($"Selected via long press: {item.Name}");
                }

                UpdateFabVisual();
            }
        }, "ItemLongPress error");
    }

    /// <summary>
    /// Handle selection changed event with proper synchronization
    /// </summary>
    private void OnSelectionChanged(object sender, Syncfusion.Maui.ListView.ItemSelectionChangedEventArgs e)
    {
        this.SafeExecute(() =>
        {
            var selectedCount = GeneraListView.SelectedItems?.Count ?? 0;
            this.LogInfo($"NATIVE Selection changed - ListView count: {selectedCount}");

            if (GeneraListView.SelectedItems != null && _viewModel?.SelectedItems != null)
            {
                _viewModel.SelectedItems.Clear();

                foreach (GenusItemViewModel item in GeneraListView.SelectedItems)
                {
                    _viewModel.SelectedItems.Add(item);
                    item.IsSelected = true;
                }

                this.LogSuccess($"Synced to ViewModel: {_viewModel.SelectedItems.Count} items");
            }

            UpdateFabVisual();

            if (e.AddedItems?.Count > 0)
            {
                foreach (GenusItemViewModel item in e.AddedItems)
                {
                    this.LogInfo($"NATIVE Selected: {item.Name}");
                }
            }

            if (e.RemovedItems?.Count > 0)
            {
                foreach (GenusItemViewModel item in e.RemovedItems)
                {
                    item.IsSelected = false;
                    this.LogInfo($"NATIVE Deselected: {item.Name}");
                }
            }
        }, "SelectionChanged error");
    }

    private const double SWIPE_THRESHOLD = 0.8;

    /// <summary>
    /// Handle swipe gesture start with logging
    /// </summary>
    private void OnSwipeStarting(object sender, Syncfusion.Maui.ListView.SwipeStartingEventArgs e)
    {
        this.SafeExecute(() =>
        {
            if (e.DataItem is GenusItemViewModel item)
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
            if (e.DataItem is GenusItemViewModel item)
            {
                var offsetPercent = Math.Abs(e.Offset) / GeneraListView.SwipeOffset;
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
            if (e.DataItem is not GenusItemViewModel item)
            {
                this.LogError("DataItem is not GenusItemViewModel");
                return;
            }

            var direction = e.Direction.ToString();
            var offsetPercent = Math.Abs(e.Offset) / GeneraListView.SwipeOffset;
            var icon = direction == "Right" ? "⭐" : "🗑️";

            this.LogInfo($"{icon} Item: {item.Name}, Direction: {direction}, Progress: {offsetPercent:P1}");

            if (offsetPercent < SWIPE_THRESHOLD)
            {
                this.LogWarning($"INSUFFICIENT SWIPE - Need {SWIPE_THRESHOLD:P0}+, got {offsetPercent:P1}");
                GeneraListView.ResetSwipeItem();
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

                        var message = wasAlreadyFavorite ? "Removed from favorites" : "Added to favorites!";
                        await this.ShowSuccessToast($"⭐ {message}");
                    }, "Favorite toggle error");
                    break;

                case "Left":
                    this.LogInfo("DELETE action triggered");

                    if (_viewModel?.DeleteSingleCommand?.CanExecute(item) == true)
                    {
                        await _viewModel.DeleteSingleCommand.ExecuteAsync(item);
                    }
                    break;
            }

            GeneraListView.ResetSwipeItem();
        }, "Swipe ended error");

        if (!success)
        {
            GeneraListView.ResetSwipeItem();
        }
    }

    /// <summary>
    /// Handle FAB press with context-aware actions (Add/Delete/Cancel)
    /// </summary>
    private async void OnFabPressed(object sender, EventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            // Perform tap feedback animation
            await this.SafeAnimationExecuteAsync(async () =>
            {
                await FabButton.PerformTapFeedbackAsync();
            }, "FAB tap feedback");

            var selectedCount = _viewModel?.SelectedItems?.Count ?? 0;
            this.LogInfo($"FAB pressed - Selected: {selectedCount}, MultiSelect: {_viewModel?.IsMultiSelectMode}");

            if (selectedCount > 0)
            {
                this.LogInfo("FAB DELETE action");
                if (_viewModel?.DeleteSelectedCommand?.CanExecute(null) == true)
                {
                    await _viewModel.DeleteSelectedCommand.ExecuteAsync(null);
                }
            }
            else if (_viewModel?.IsMultiSelectMode == true)
            {
                this.LogInfo("FAB CANCEL action");
                if (_viewModel?.ToggleMultiSelectCommand?.CanExecute(null) == true)
                {
                    _viewModel.ToggleMultiSelectCommand.Execute(null);
                }
            }
            else
            {
                this.LogInfo("FAB ADD action");
                // Navigate to add new genus - seguindo padrão do FamiliesListPage
                await Shell.Current.GoToAsync("genusedit");
            }
        }, "FAB action failed");
    }
}