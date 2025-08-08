using OrchidPro.ViewModels.Species;
using OrchidPro.Constants;
using OrchidPro.Extensions;

namespace OrchidPro.Views.Pages;

/// <summary>
/// Page for displaying and managing list of botanical species with advanced UI features.
/// Provides CRUD operations, multi-selection, filtering, sorting, and swipe actions.
/// EXATAMENTE o mesmo padrão de FamiliesListPage que FUNCIONA.
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

        this.LogInfo("Initialized Species List Page");

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

    #region Search and Filter Event Handlers

    /// <summary>
    /// Handle search text changes with debounce pattern
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
        }, "OnSearchUnfocused");
    }

    /// <summary>
    /// Handle filter button tap - EXATAMENTE igual FamiliesListPage
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

                if (_viewModel.ApplyFilterCommand != null && _viewModel.ApplyFilterCommand.CanExecute(null))
                {
                    await _viewModel.ApplyFilterCommand.ExecuteAsync(null);
                }

                await this.ShowInfoToast($"Filter: {result}");
            }
        }, "Filter error");
    }

    /// <summary>
    /// Handle sort button tap - EXATAMENTE igual FamiliesListPage
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

                if (_viewModel.ToggleSortCommand != null && _viewModel.ToggleSortCommand.CanExecute(null))
                {
                    _viewModel.ToggleSortCommand.Execute(null);
                }

                await this.ShowInfoToast($"Sorted by: {result}");
            }
        }, "Sort error");
    }

    #endregion

    #region Syncfusion ListView Event Handlers - CÓPIA EXATA DO FamiliesListPage

    /// <summary>
    /// Handle item tap for selection or navigation based on current mode
    /// </summary>
    private void OnItemTapped(object sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
    {
        this.SafeExecute(() =>
        {
            if (e.DataItem is SpeciesItemViewModel item)
            {
                this.LogInfo($"Item tapped: {item.Name} (ID: {item.Id})");
                this.LogInfo($"Current IsMultiSelectMode: {_viewModel?.IsMultiSelectMode}");
                this.LogInfo($"Current SelectionMode: {SpeciesListView.SelectionMode}");

                if (_viewModel?.IsMultiSelectMode == true)
                {
                    this.LogInfo("Multi-select mode - toggling selection");

                    if (item.IsSelected)
                    {
                        item.IsSelected = false;
                        _viewModel?.SelectedItems?.Remove(item);
                        SpeciesListView.SelectedItems?.Remove(item);
                        this.LogInfo($"Deselected: {item.Name}");
                    }
                    else
                    {
                        item.IsSelected = true;
                        if (_viewModel?.SelectedItems != null && !_viewModel.SelectedItems.Contains(item))
                            _viewModel.SelectedItems.Add(item);
                        if (SpeciesListView.SelectedItems != null && !SpeciesListView.SelectedItems.Contains(item))
                            SpeciesListView.SelectedItems.Add(item);
                        this.LogInfo($"Selected: {item.Name}");
                    }

                    // ✅ CRÍTICO: Atualizar FAB após mudança de seleção
                    UpdateFabVisual();

                    // ✅ CRÍTICO: Verificar se deve sair do modo multi-select
                    if (_viewModel?.SelectedItems?.Count == 0)
                    {
                        this.LogInfo("No items selected - exiting multi-select mode");
                        if (_viewModel != null)
                        {
                            _viewModel.IsMultiSelectMode = false;
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                SpeciesListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.None;
                                UpdateFabVisual(); // ✅ Atualizar FAB novamente após sair do modo
                            });
                        }
                    }
                }
                else
                {
                    this.LogInfo($"Normal mode - navigating to edit: {item.Name} with ID: {item.Id}");

                    // ✅ CORRIGIDO: Verificar se o item tem ID válido antes de navegar
                    if (item.Id == Guid.Empty)
                    {
                        this.LogError($"Invalid item ID (empty Guid) for item: {item.Name}");
                        return;
                    }

                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        if (_viewModel?.NavigateToEditCommand != null && _viewModel.NavigateToEditCommand.CanExecute(item))
                        {
                            this.LogInfo($"Executing NavigateToEditCommand for species ID: {item.Id}");
                            await _viewModel.NavigateToEditCommand.ExecuteAsync(item);
                        }
                        else
                        {
                            this.LogError("NavigateToEditCommand is null or cannot execute");
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
            if (e.DataItem is SpeciesItemViewModel item)
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
                        SpeciesListView.RefreshItem(index);
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
            var selectedCount = SpeciesListView.SelectedItems?.Count ?? 0;
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

                // ✅ CORRIGIDO: Auto-exit multi-select quando seleção fica vazia
                if (vmSelectedCount == 0 && _viewModel.IsMultiSelectMode)
                {
                    this.LogInfo("Auto-exiting multi-select mode - no items selected");
                    _viewModel.IsMultiSelectMode = false;

                    // ✅ CRÍTICO: Reset SelectionMode do ListView
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        SpeciesListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.None;
                        this.LogInfo("ListView SelectionMode reset to None");
                    });
                }
                else if (vmSelectedCount > 0 && !_viewModel.IsMultiSelectMode)
                {
                    _viewModel.IsMultiSelectMode = true;
                }

                // ✅ CRÍTICO: Sempre atualizar FAB após mudanças de seleção
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

    #region Swipe Action Handlers - CÓPIA EXATA DO FamiliesListPage

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

                        if (_viewModel?.ToggleFavoriteCommand != null && _viewModel.ToggleFavoriteCommand.CanExecute(item))
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

                    if (_viewModel?.DeleteSingleItemCommand != null && _viewModel.DeleteSingleItemCommand.CanExecute(item))
                    {
                        await _viewModel.DeleteSingleItemCommand.ExecuteAsync(item);
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

            if (_viewModel?.SelectAllCommand != null && _viewModel.SelectAllCommand.CanExecute(null))
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

            if (_viewModel?.DeselectAllCommand != null && _viewModel.DeselectAllCommand.CanExecute(null))
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
                if (_viewModel.SelectedItems?.Count > 0)
                {
                    if (_viewModel?.DeleteSelectedCommand != null && _viewModel.DeleteSelectedCommand.CanExecute(null))
                    {
                        await _viewModel.DeleteSelectedCommand.ExecuteAsync(null);
                        this.LogSuccess("Bulk delete executed");
                    }
                }
                else
                {
                    // Cancel multi-select mode
                    this.LogInfo("No items selected - exiting multi-select mode");
                    _viewModel.IsMultiSelectMode = false;
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        SpeciesListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.None;
                        UpdateFabVisual();
                    });
                }
            }
            else
            {
                // Add new species
                if (_viewModel?.NavigateToAddCommand != null && _viewModel.NavigateToAddCommand.CanExecute(null))
                {
                    await _viewModel.NavigateToAddCommand.ExecuteAsync(null);
                    this.LogSuccess("Navigate to add executed");
                }
            }
        }, "OnFabTapped");
    }

    /// <summary>
    /// Update FAB visual state based on selection mode - IGUAL FamiliesListPage
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
                        : Color.FromArgb("#D32F2F");

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
                    FabButton.Text = "Add Species";
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
                FabButton.Text = "Add Species";

                var primaryColor = Application.Current?.Resources.TryGetValue("Primary", out var primary) == true
                    ? (Color)primary
                    : Color.FromArgb("#A47764");
                FabButton.BackgroundColor = primaryColor;
            });
        }
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
            if (_viewModel != null && _viewModel.OnAppearingAsync != null)
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
            if (_viewModel != null && _viewModel.OnDisappearingAsync != null)
            {
                await _viewModel.OnDisappearingAsync();
            }

            this.LogSuccess("Page disappeared successfully");
        }, "OnDisappearing");
    }

    #endregion
}