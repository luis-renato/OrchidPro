using OrchidPro.ViewModels.Families;
using OrchidPro.Constants;
using OrchidPro.Extensions;

namespace OrchidPro.Views.Pages;

/// <summary>
/// OPTIMIZED Page for displaying and managing list of botanical families.
/// FIXED: Removed double refresh that was causing performance issues.
/// </summary>
public partial class FamiliesListPage : ContentPage
{
    private readonly FamiliesListViewModel _viewModel;

    /// <summary>
    /// Initialize the families list page with dependency injection and event binding
    /// </summary>
    public FamiliesListPage(FamiliesListViewModel viewModel)
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

            if (FamilyListView.SelectionMode != targetMode)
            {
                this.LogInfo($"Syncing SelectionMode: {FamilyListView.SelectionMode} → {targetMode}");
                FamilyListView.SelectionMode = targetMode;
                this.LogSuccess($"SelectionMode synced to: {FamilyListView.SelectionMode}");
            }

            if (!_viewModel.IsMultiSelectMode && FamilyListView.SelectedItems?.Count > 0)
            {
                this.LogInfo("Clearing ListView selections on multi-select exit");
                FamilyListView.SelectedItems.Clear();
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
            this.LogInfo("Pull-to-refresh triggered");

            ListRefresh.IsRefreshing = true;

            if (_viewModel.RefreshCommand?.CanExecute(null) == true)
            {
                await _viewModel.RefreshCommand.ExecuteAsync(null);
            }

            ListRefresh.IsRefreshing = false;

            this.LogSuccess("Pull-to-refresh completed");
        }, "Pull-to-refresh failed");
    }

    /// <summary>
    /// OPTIMIZED page appearing lifecycle - FIXED DOUBLE REFRESH!
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await this.SafeExecuteAsync(async () =>
        {
            if (FamilyListView.SelectedItems == null)
            {
                this.LogInfo("Initializing ListView.SelectedItems");
                await Task.Delay(100);
            }
        }, "Error checking SelectedItems");

        await PerformEntranceAnimation();

        this.LogInfo("Refreshing data on page appearing");

        // 🚀 FIXED: SINGLE REFRESH ONLY - just like Species!
        await _viewModel.OnAppearingAsync();

        // ❌ REMOVED: Double refresh that was causing performance issues
        // if (_viewModel.RefreshCommand?.CanExecute(null) == true)
        // {
        //     await _viewModel.RefreshCommand.ExecuteAsync(null);
        // }

        SyncSelectionMode();
        UpdateFabVisual();

        this.LogInfo($"Final check - ListView.SelectedItems: {(FamilyListView.SelectedItems != null ? "OK" : "NULL")}");
    }

    /// <summary>
    /// Handle page disappearing lifecycle with exit animations
    /// </summary>
    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await PerformExitAnimation();
    }

    #region Animation Management

    /// <summary>
    /// Perform entrance animation for smooth page transition
    /// </summary>
    private async Task PerformEntranceAnimation()
    {
        await this.SafeAnimationExecuteAsync(async () =>
        {
            // Complete page entrance animation with FAB delay
            await RootGrid.PerformCompletePageEntranceAsync(FabButton);
        }, "Page entrance animation");
    }

    /// <summary>
    /// Perform exit animation for smooth page transition
    /// </summary>
    private async Task PerformExitAnimation()
    {
        await this.SafeAnimationExecuteAsync(async () =>
        {
            // Complete page exit animation
            await RootGrid.PerformCompletePageExitAsync(FabButton);
        }, "Page exit animation");
    }

    #endregion

    #region FAB Visual Management

    /// <summary>
    /// Update FAB appearance based on current selection state and connectivity
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
                        : Color.FromArgb(ColorConstants.ERROR_COLOR);

                    FabButton.BackgroundColor = errorColor;
                    FabButton.Text = $"{TextConstants.DELETE_ITEM} ({selectedCount})";
                    this.LogInfo($"Set to DELETE mode: {TextConstants.DELETE_ITEM} ({selectedCount})");
                }
                else if (_viewModel?.IsMultiSelectMode == true)
                {
                    var grayColor = Application.Current?.Resources.TryGetValue("Gray500", out var gray) == true
                        ? (Color)gray
                        : Color.FromArgb(ColorConstants.GRAY_500);

                    FabButton.BackgroundColor = grayColor;
                    FabButton.Text = TextConstants.CANCEL_CHANGES;
                    this.LogInfo("Set to CANCEL mode");
                }
                else
                {
                    var primaryColor = Application.Current?.Resources.TryGetValue("Primary", out var primary) == true
                        ? (Color)primary
                        : Color.FromArgb(ColorConstants.PRIMARY_COLOR);

                    FabButton.BackgroundColor = primaryColor;
                    FabButton.Text = TextConstants.ADD_FAMILY;
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
                FabButton.Text = TextConstants.ADD_FAMILY;

                var primaryColor = Application.Current?.Resources.TryGetValue("Primary", out var primary) == true
                    ? (Color)primary
                    : Color.FromArgb(ColorConstants.PRIMARY_COLOR);
                FabButton.BackgroundColor = primaryColor;
            });
        }
    }

    #endregion

    #region Toolbar Event Handlers

    /// <summary>
    /// Handle select all toolbar item with proper SelectionMode synchronization
    /// </summary>
    private void OnSelectAllTapped(object sender, EventArgs e)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo("Select All toolbar tapped");

            if (FamilyListView.SelectionMode != Syncfusion.Maui.ListView.SelectionMode.Multiple)
            {
                this.LogInfo("Setting ListView to Multiple mode for Select All");
                FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.Multiple;
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
                    FamilyListView.SelectedItems?.Clear();

                    foreach (var item in _viewModel.Items)
                    {
                        if (item.IsSelected && FamilyListView.SelectedItems != null)
                        {
                            FamilyListView.SelectedItems.Add(item);
                            this.LogInfo($"Added {item.Name} to ListView selection");
                        }
                    }

                    for (int i = 0; i < _viewModel.Items.Count; i++)
                    {
                        FamilyListView.RefreshItem(i);
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
                    FamilyListView.SelectedItems?.Clear();
                    FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.None;

                    if (_viewModel?.Items != null)
                    {
                        for (int i = 0; i < _viewModel.Items.Count; i++)
                        {
                            FamilyListView.RefreshItem(i);
                        }
                    }

                    UpdateFabVisual();
                }, "ListView clear error");
            });

            this.LogSuccess("Clear All completed successfully");
        }, "Clear All failed");
    }

    #endregion

    #region Search Bar Event Handlers

    /// <summary>
    /// Handle search bar focus with visual feedback animation
    /// </summary>
    private async void OnSearchFocused(object sender, FocusEventArgs e)
    {
        await this.SafeAnimationExecuteAsync(async () =>
        {
            if (sender is Entry entry && entry.Parent?.Parent is Border border)
            {
                await border.ScaleTo(AnimationConstants.BORDER_FOCUS_SCALE, AnimationConstants.BORDER_FOCUS_DURATION, AnimationConstants.FEEDBACK_EASING);
                border.Stroke = Application.Current?.Resources["Primary"] as Color ?? Colors.Blue;
            }
        }, "Search focus animation");
    }

    /// <summary>
    /// Handle search bar unfocus with visual feedback animation
    /// </summary>
    private async void OnSearchUnfocused(object sender, FocusEventArgs e)
    {
        await this.SafeAnimationExecuteAsync(async () =>
        {
            if (sender is Entry entry && entry.Parent?.Parent is Border border)
            {
                await border.ScaleTo(AnimationConstants.FEEDBACK_SCALE_NORMAL, AnimationConstants.BORDER_FOCUS_DURATION, AnimationConstants.FEEDBACK_EASING);
                border.Stroke = Application.Current?.Resources["Gray300"] as Color ?? Colors.Gray;
            }
        }, "Search unfocus animation");
    }

    /// <summary>
    /// OPTIMIZED search text changes - minimal logging like Species
    /// </summary>
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"Search text changed to: '{e.NewTextValue}'");
        }, "Search text changed error");
    }

    #endregion

    #region FAB Action Handler

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
                this.LogInfo($"Executing unified delete for {selectedCount} items");

                // Unified delete flow through ViewModel only
                if (_viewModel?.DeleteSelectedCommand?.CanExecute(null) == true)
                {
                    await _viewModel.DeleteSelectedCommand.ExecuteAsync(null);
                    this.LogSuccess("Unified delete completed via ViewModel");
                }

                // UI cleanup after ViewModel handles the delete
                Device.BeginInvokeOnMainThread(() =>
                {
                    this.SafeExecute(() =>
                    {
                        FamilyListView.SelectedItems?.Clear();
                        FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.None;

                        if (_viewModel?.Items != null)
                        {
                            for (int i = 0; i < _viewModel.Items.Count; i++)
                            {
                                FamilyListView.RefreshItem(i);
                            }
                        }

                        UpdateFabVisual();
                    }, "UI cleanup error");
                });
            }
            else if (_viewModel?.IsMultiSelectMode == true)
            {
                this.LogInfo("Canceling multi-select mode");

                if (_viewModel != null)
                {
                    _viewModel.IsMultiSelectMode = false;
                }

                UpdateFabVisual();
            }
            else
            {
                this.LogInfo("Adding new family");

                if (_viewModel?.AddNewCommand?.CanExecute(null) == true)
                {
                    await _viewModel.AddNewCommand.ExecuteAsync(null);
                }
                else
                {
                    await this.SafeNavigationExecuteAsync(async () =>
                    {
                        await Shell.Current.GoToAsync("familyedit");
                    }, "familyedit");
                }
            }
        }, "FAB action failed");
    }

    #endregion

    #region Filter and Sort Handlers

    /// <summary>
    /// Handle filter selection with action sheet and apply filter command
    /// </summary>
    private async void OnFilterTapped(object sender, EventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (sender is Border border)
            {
                await this.SafeAnimationExecuteAsync(async () =>
                {
                    await border.PerformTapFeedbackAsync();
                }, "Filter tap feedback");
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
    /// Handle sort selection with action sheet and apply sort command
    /// </summary>
    private async void OnSortTapped(object sender, EventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (sender is Border border)
            {
                await this.SafeAnimationExecuteAsync(async () =>
                {
                    await border.PerformTapFeedbackAsync();
                }, "Sort tap feedback");
            }

            string[] options = { "Name A→Z", "Name Z→A", "Recent First", "Oldest First", "Favorites First" };
            string result = await DisplayActionSheet("Sort by", "Cancel", null, options);

            if (result != "Cancel" && result != null && _viewModel != null)
            {
                _viewModel.SortOrder = result;
                this.LogInfo($"Sort order changed to: {result}");

                if (_viewModel.ToggleSortCommand?.CanExecute(null) == true)
                {
                    _viewModel.ToggleSortCommand.Execute(null);
                }

                await this.ShowInfoToast($"Sorted by: {result}");
            }
        }, "Sort error");
    }

    #endregion

    #region Syncfusion ListView Event Handlers

    /// <summary>
    /// Handle item tap for selection or navigation based on current mode
    /// </summary>
    private void OnItemTapped(object sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
    {
        this.SafeExecute(() =>
        {
            if (e.DataItem is FamilyItemViewModel item)
            {
                this.LogInfo($"Item tapped: {item.Name}");
                this.LogInfo($"Current IsMultiSelectMode: {_viewModel?.IsMultiSelectMode}");
                this.LogInfo($"Current SelectionMode: {FamilyListView.SelectionMode}");

                if (_viewModel?.IsMultiSelectMode == true)
                {
                    this.LogInfo("Multi-select mode - toggling selection");

                    if (item.IsSelected)
                    {
                        item.IsSelected = false;
                        _viewModel?.SelectedItems?.Remove(item);
                        FamilyListView.SelectedItems?.Remove(item);
                        this.LogInfo($"Deselected: {item.Name}");
                    }
                    else
                    {
                        item.IsSelected = true;
                        if (_viewModel?.SelectedItems != null && !_viewModel.SelectedItems.Contains(item))
                            _viewModel.SelectedItems.Add(item);
                        if (FamilyListView.SelectedItems != null && !FamilyListView.SelectedItems.Contains(item))
                            FamilyListView.SelectedItems.Add(item);
                        this.LogInfo($"Selected: {item.Name}");
                    }

                    UpdateFabVisual();

                    if (_viewModel?.SelectedItems?.Count == 0)
                    {
                        if (_viewModel != null)
                        {
                            _viewModel.IsMultiSelectMode = false;
                        }
                    }
                }
                else
                {
                    this.LogInfo($"Normal mode - navigating to edit: {item.Name}");
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        if (_viewModel?.NavigateToEditCommand?.CanExecute(item) == true)
                        {
                            await _viewModel.NavigateToEditCommand.ExecuteAsync(item);
                        }
                    });
                }
            }
        }, "ItemTapped error");
    }

    /// <summary>
    /// Handle long press to activate multi-selection mode
    /// </summary>
    private void OnItemLongPress(object sender, Syncfusion.Maui.ListView.ItemLongPressEventArgs e)
    {
        this.SafeExecute(() =>
        {
            if (e.DataItem is FamilyItemViewModel item)
            {
                this.LogInfo($"Long press: {item.Name}");

                if (_viewModel?.IsMultiSelectMode != true)
                {
                    if (_viewModel != null)
                    {
                        _viewModel.IsMultiSelectMode = true;
                    }
                    this.LogSuccess("Multi-select mode ACTIVATED");
                }

                if (!item.IsSelected)
                {
                    item.IsSelected = true;
                    if (_viewModel?.SelectedItems != null && !_viewModel.SelectedItems.Contains(item))
                    {
                        _viewModel.SelectedItems.Add(item);
                    }
                    this.LogSuccess($"Item selected: {item.Name}");
                }

                Device.BeginInvokeOnMainThread(() =>
                {
                    var index = _viewModel?.Items?.IndexOf(item) ?? -1;
                    if (index >= 0)
                    {
                        FamilyListView.RefreshItem(index);
                        this.LogInfo($"Visual refresh for index: {index}");
                    }
                });

                UpdateFabVisual();
            }
        }, "ItemLongPress error");
    }

    /// <summary>
    /// Handle native selection changes and sync with ViewModel
    /// </summary>
    private void OnSelectionChanged(object sender, Syncfusion.Maui.ListView.ItemSelectionChangedEventArgs e)
    {
        this.SafeExecute(() =>
        {
            var selectedCount = FamilyListView.SelectedItems?.Count ?? 0;
            this.LogInfo($"NATIVE Selection changed - ListView count: {selectedCount}");

            if (FamilyListView.SelectedItems != null && _viewModel?.SelectedItems != null)
            {
                _viewModel.SelectedItems.Clear();

                foreach (FamilyItemViewModel item in FamilyListView.SelectedItems)
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

                this.LogInfo($"ViewModel updated - MultiSelect: {_viewModel.IsMultiSelectMode}, SelectionMode: {FamilyListView.SelectionMode}");
            }

            if (e.AddedItems?.Count > 0)
            {
                foreach (FamilyItemViewModel item in e.AddedItems)
                {
                    this.LogInfo($"NATIVE Selected: {item.Name}");
                }
            }

            if (e.RemovedItems?.Count > 0)
            {
                foreach (FamilyItemViewModel item in e.RemovedItems)
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
            if (e.DataItem is FamilyItemViewModel item)
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
            if (e.DataItem is FamilyItemViewModel item)
            {
                var offsetPercent = Math.Abs(e.Offset) / FamilyListView.SwipeOffset;
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
            if (e.DataItem is not FamilyItemViewModel item)
            {
                this.LogError("DataItem is not FamilyItemViewModel");
                return;
            }

            var direction = e.Direction.ToString();
            var offsetPercent = Math.Abs(e.Offset) / FamilyListView.SwipeOffset;
            var icon = direction == "Right" ? "⭐" : "🗑️";

            this.LogInfo($"{icon} Item: {item.Name}, Direction: {direction}, Progress: {offsetPercent:P1}");

            if (offsetPercent < SWIPE_THRESHOLD)
            {
                this.LogWarning($"INSUFFICIENT SWIPE - Need {SWIPE_THRESHOLD:P0}+, got {offsetPercent:P1}");
                FamilyListView.ResetSwipeItem();
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

                        var message = wasAlreadyFavorite ? "Removed from favorites" : "Added to favorites! ⭐";
                        await this.ShowSuccessToast(message);
                    }, "Failed to update favorite status");
                    break;

                case "Left":
                    this.LogInfo("UNIFIED DELETE action triggered");

                    if (_viewModel?.IsConnected != true)
                    {
                        await this.ShowWarningToast("Cannot delete while offline");
                        break;
                    }

                    await this.SafeExecuteAsync(async () =>
                    {
                        this.LogInfo($"Calling UNIFIED DeleteSingleCommand for: {item.Name}");

                        // Unified delete flow through ViewModel only
                        if (_viewModel?.DeleteSingleCommand?.CanExecute(item) == true)
                        {
                            await _viewModel.DeleteSingleCommand.ExecuteAsync(item);
                            this.LogSuccess("UNIFIED delete completed via ViewModel");
                        }
                    }, "UNIFIED Delete failed");
                    break;

                default:
                    this.LogWarning($"Unknown swipe direction: {direction}");
                    break;
            }

            FamilyListView.ResetSwipeItem();
            this.LogInfo("Auto-reset completed");
        }, "Swipe action failed");

        if (!success)
        {
            FamilyListView.ResetSwipeItem();
        }
    }

    #endregion
}