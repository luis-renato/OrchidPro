using OrchidPro.ViewModels.Families;
using OrchidPro.Constants;
using OrchidPro.Extensions;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace OrchidPro.Views.Pages;

/// <summary>
/// ✅ FIXED: FamiliesListPage with UNIFIED DELETE FLOW - No duplicate confirmations/messages
/// </summary>
public partial class FamiliesListPage : ContentPage
{
    private readonly FamiliesListViewModel _viewModel;

    public FamiliesListPage(FamiliesListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        Debug.WriteLine("✅ [FAMILIES_LIST_PAGE] Initialized with UNIFIED delete flow");

        // Hook up events
        ListRefresh.Refreshing += PullToRefresh_Refreshing;

        // Monitor ViewModel changes to sync SelectionMode
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        if (_viewModel?.SelectedItems == null)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] ViewModel.SelectedItems is NULL during initialization!");
        }
    }

    /// <summary>
    /// ✅ Sync ListView SelectionMode with ViewModel state
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_viewModel.IsMultiSelectMode))
        {
            Debug.WriteLine($"🔄 [FAMILIES_LIST_PAGE] IsMultiSelectMode changed to: {_viewModel.IsMultiSelectMode}");
            SyncSelectionMode();
        }
    }

    /// <summary>
    /// ✅ Ensure ListView SelectionMode matches ViewModel state
    /// </summary>
    private void SyncSelectionMode()
    {
        try
        {
            var targetMode = _viewModel.IsMultiSelectMode
                ? Syncfusion.Maui.ListView.SelectionMode.Multiple
                : Syncfusion.Maui.ListView.SelectionMode.None;

            if (FamilyListView.SelectionMode != targetMode)
            {
                Debug.WriteLine($"🔄 [FAMILIES_LIST_PAGE] Syncing SelectionMode: {FamilyListView.SelectionMode} → {targetMode}");
                FamilyListView.SelectionMode = targetMode;
                Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] SelectionMode synced to: {FamilyListView.SelectionMode}");
            }

            if (!_viewModel.IsMultiSelectMode && FamilyListView.SelectedItems?.Count > 0)
            {
                Debug.WriteLine($"🧹 [FAMILIES_LIST_PAGE] Clearing ListView selections on multi-select exit");
                FamilyListView.SelectedItems.Clear();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] SyncSelectionMode error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ Handler do evento Pull-to-Refresh
    /// </summary>
    private async void PullToRefresh_Refreshing(object? sender, EventArgs e)
    {
        try
        {
            Debug.WriteLine("🔄 [FAMILIES_LIST_PAGE] Pull-to-refresh triggered");

            ListRefresh.IsRefreshing = true;

            if (_viewModel.RefreshCommand?.CanExecute(null) == true)
            {
                await _viewModel.RefreshCommand.ExecuteAsync(null);
            }

            ListRefresh.IsRefreshing = false;

            Debug.WriteLine("✅ [FAMILIES_LIST_PAGE] Pull-to-refresh completed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Pull-to-refresh error: {ex.Message}");
            ListRefresh.IsRefreshing = false;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            if (FamilyListView.SelectedItems == null)
            {
                Debug.WriteLine($"🔧 [FAMILIES_LIST_PAGE] Initializing ListView.SelectedItems");
                await Task.Delay(100);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Error checking SelectedItems: {ex.Message}");
        }

        await PerformEntranceAnimation();

        Debug.WriteLine($"🔄 [FAMILIES_LIST_PAGE] Refreshing data on page appearing");
        await _viewModel.OnAppearingAsync();

        if (_viewModel.RefreshCommand?.CanExecute(null) == true)
        {
            await _viewModel.RefreshCommand.ExecuteAsync(null);
        }

        SyncSelectionMode();
        UpdateFabVisual();

        Debug.WriteLine($"🔍 [FAMILIES_LIST_PAGE] Final check - ListView.SelectedItems: {(FamilyListView.SelectedItems != null ? "OK" : "NULL")}");
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await PerformExitAnimation();
    }

    #region ✅ ANIMATIONS

    private async Task PerformEntranceAnimation()
    {
        try
        {
            // ✅ USANDO EXTENSÃO: Animação completa de página + FAB com delay
            await RootGrid.PerformCompletePageEntranceAsync(FabButton);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Entrance animation error: {ex.Message}");
            RootGrid.Opacity = 1;
            FabButton.Opacity = 1;
        }
    }

    private async Task PerformExitAnimation()
    {
        try
        {
            // ✅ USANDO EXTENSÃO: Animação completa de saída
            await RootGrid.PerformCompletePageExitAsync(FabButton);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Exit animation error: {ex.Message}");
        }
    }

    #endregion

    #region ✅ FAB VISUAL UPDATES

    /// <summary>
    /// ✅ Atualizar visual do FAB usando cores do ResourceDictionary
    /// </summary>
    private void UpdateFabVisual()
    {
        try
        {
            var selectedCount = _viewModel?.SelectedItems?.Count ?? 0;
            Debug.WriteLine($"🎨 [FAB_VISUAL] Updating FAB for {selectedCount} selected items");

            Device.BeginInvokeOnMainThread(() =>
            {
                FabButton.IsVisible = true;

                if (selectedCount > 0)
                {
                    var errorColor = Application.Current?.Resources.TryGetValue("ErrorColor", out var error) == true
                        ? (Color)error
                        : Color.FromArgb(ColorConstants.ERROR_COLOR); // ✅ USANDO CONSTANTE: era "#D32F2F"

                    FabButton.BackgroundColor = errorColor;
                    FabButton.Text = $"{TextConstants.DELETE_ITEM} ({selectedCount})"; // ✅ USANDO CONSTANTE: era "Delete ({selectedCount})"
                    Debug.WriteLine($"🔴 [FAB_VISUAL] Set to DELETE mode: {TextConstants.DELETE_ITEM} ({selectedCount})");
                }
                else if (_viewModel?.IsMultiSelectMode == true)
                {
                    var grayColor = Application.Current?.Resources.TryGetValue("Gray500", out var gray) == true
                        ? (Color)gray
                        : Color.FromArgb(ColorConstants.GRAY_500); // ✅ USANDO CONSTANTE: era "#757575"

                    FabButton.BackgroundColor = grayColor;
                    FabButton.Text = TextConstants.CANCEL_CHANGES; // ✅ USANDO CONSTANTE: era "Cancel"
                    Debug.WriteLine($"⚫ [FAB_VISUAL] Set to CANCEL mode");
                }
                else
                {
                    var primaryColor = Application.Current?.Resources.TryGetValue("Primary", out var primary) == true
                        ? (Color)primary
                        : Color.FromArgb(ColorConstants.PRIMARY_COLOR); // ✅ USANDO CONSTANTE: era "#A47764"

                    FabButton.BackgroundColor = primaryColor;
                    FabButton.Text = TextConstants.ADD_FAMILY; // ✅ USANDO CONSTANTE: era "Add Family"
                    Debug.WriteLine($"🟢 [FAB_VISUAL] Set to ADD mode");
                }

                Debug.WriteLine($"✅ [FAB_VISUAL] FAB updated successfully - Text: {FabButton.Text}");
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAB_VISUAL] Error updating FAB visual: {ex.Message}");

            Device.BeginInvokeOnMainThread(() =>
            {
                FabButton.IsVisible = true;
                FabButton.Text = TextConstants.ADD_FAMILY; // ✅ USANDO CONSTANTE: era "Add Family"

                var primaryColor = Application.Current?.Resources.TryGetValue("Primary", out var primary) == true
                    ? (Color)primary
                    : Color.FromArgb(ColorConstants.PRIMARY_COLOR); // ✅ USANDO CONSTANTE: era "#A47764"
                FabButton.BackgroundColor = primaryColor;
            });
        }
    }
    #endregion

    #region ✅ TOOLBAR ITEMS HANDLERS

    /// <summary>
    /// ✅ Select All toolbar with proper SelectionMode sync
    /// </summary>
    private void OnSelectAllTapped(object sender, EventArgs e)
    {
        try
        {
            Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] Select All toolbar tapped");

            if (FamilyListView.SelectionMode != Syncfusion.Maui.ListView.SelectionMode.Multiple)
            {
                Debug.WriteLine($"🔄 [FAMILIES_LIST_PAGE] Setting ListView to Multiple mode for Select All");
                FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.Multiple;
            }

            if (_viewModel?.SelectAllCommand?.CanExecute(null) == true)
            {
                _viewModel.SelectAllCommand.Execute(null);
                Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] SelectAllCommand executed");
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    FamilyListView.SelectedItems?.Clear();

                    foreach (var item in _viewModel.Items)
                    {
                        if (item.IsSelected && FamilyListView.SelectedItems != null)
                        {
                            FamilyListView.SelectedItems.Add(item);
                            Debug.WriteLine($"🔘 [FAMILIES_LIST_PAGE] Added {item.Name} to ListView selection");
                        }
                    }

                    for (int i = 0; i < _viewModel.Items.Count; i++)
                    {
                        FamilyListView.RefreshItem(i);
                    }

                    UpdateFabVisual();
                }
                catch (Exception syncEx)
                {
                    Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] ListView sync error: {syncEx.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Select All toolbar error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ Clear All with proper cleanup
    /// </summary>
    private async void OnDeselectAllTapped(object sender, EventArgs e)
    {
        try
        {
            Debug.WriteLine($"🧹 [FAMILIES_LIST_PAGE] Clear All toolbar tapped");

            if (_viewModel?.ClearSelectionCommand != null && _viewModel.ClearSelectionCommand.CanExecute(null))
            {
                _viewModel.ClearSelectionCommand.Execute(null);
                Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] ClearSelectionCommand executed");
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                try
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
                }
                catch (Exception clearEx)
                {
                    Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] ListView clear error: {clearEx.Message}");
                }
            });

            await this.ShowSuccessToast("🧹 Cleared selections and filters");

            Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] Clear All completed successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Clear All error: {ex.Message}");
            // ✅ USANDO EXTENSÃO: Toast de erro padronizado
            await this.ShowErrorToast($"Failed to clear: {ex.Message}");
        }
    }
    #endregion

    #region ✅ SEARCH BAR EVENTS

    private async void OnSearchFocused(object sender, FocusEventArgs e)
    {
        try
        {
            if (sender is Entry entry && entry.Parent?.Parent is Border border)
            {
                await border.ScaleTo(AnimationConstants.BORDER_FOCUS_SCALE, AnimationConstants.BORDER_FOCUS_DURATION, AnimationConstants.FEEDBACK_EASING); // ✅ USANDO CONSTANTE: era 1.02, 150, Easing.CubicOut
                border.Stroke = Application.Current?.Resources["Primary"] as Color ?? Colors.Blue;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Search focus error: {ex.Message}");
        }
    }

    private async void OnSearchUnfocused(object sender, FocusEventArgs e)
    {
        try
        {
            if (sender is Entry entry && entry.Parent?.Parent is Border border)
            {
                await border.ScaleTo(AnimationConstants.FEEDBACK_SCALE_NORMAL, AnimationConstants.BORDER_FOCUS_DURATION, AnimationConstants.FEEDBACK_EASING); // ✅ USANDO CONSTANTE: era 1, 150, Easing.CubicOut
                border.Stroke = Application.Current?.Resources["Gray300"] as Color ?? Colors.Gray;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Search unfocus error: {ex.Message}");
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            Debug.WriteLine($"🔍 [FAMILIES_LIST_PAGE] Search text changed to: '{e.NewTextValue}'");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Search text changed error: {ex.Message}");
        }
    }

    #endregion

    #region ✅ FAB HANDLER - UNIFIED FLOW

    /// <summary>
    /// ✅ FIXED: FAB handler with UNIFIED delete flow via ViewModel only
    /// </summary>
    private async void OnFabPressed(object sender, EventArgs e)
    {
        try
        {
            // ✅ USANDO EXTENSÃO: Tap feedback padronizado
            await FabButton.PerformTapFeedbackAsync();

            var selectedCount = _viewModel?.SelectedItems?.Count ?? 0;
            Debug.WriteLine($"🎯 [FAMILIES_LIST_PAGE] FAB pressed - Selected: {selectedCount}, MultiSelect: {_viewModel?.IsMultiSelectMode}");

            if (selectedCount > 0)
            {
                Debug.WriteLine($"🗑️ [FAMILIES_LIST_PAGE] Executing UNIFIED delete for {selectedCount} items");

                try
                {
                    // ✅ UNIFIED: Call ViewModel command only - it handles confirmation + success message
                    if (_viewModel?.DeleteSelectedCommand?.CanExecute(null) == true)
                    {
                        await _viewModel.DeleteSelectedCommand.ExecuteAsync(null);
                        Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] UNIFIED delete completed via ViewModel");
                    }

                    // ✅ UI cleanup after ViewModel handles the delete
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        try
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
                        }
                        catch (Exception uiEx)
                        {
                            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] UI cleanup error: {uiEx.Message}");
                        }
                    });
                }
                catch (Exception deleteEx)
                {
                    Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] UNIFIED delete failed: {deleteEx.Message}");
                }
            }
            else if (_viewModel?.IsMultiSelectMode == true)
            {
                Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Canceling multi-select mode");

                if (_viewModel != null)
                {
                    _viewModel.IsMultiSelectMode = false;
                }

                UpdateFabVisual();
            }
            else
            {
                Debug.WriteLine($"➕ [FAMILIES_LIST_PAGE] Adding new family");

                if (_viewModel?.AddNewCommand?.CanExecute(null) == true)
                {
                    await _viewModel.AddNewCommand.ExecuteAsync(null);
                }
                else
                {
                    await Shell.Current.GoToAsync("familyedit");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] FAB action error: {ex.Message}");
            // ✅ USANDO EXTENSÃO: Toast padronizado
            await this.ShowErrorToast("Action failed. Please try again.");
        }
    }

    #endregion

    #region ✅ FILTER AND SORT HANDLERS

    private async void OnFilterTapped(object sender, EventArgs e)
    {
        try
        {
            if (sender is Border border)
            {
                // ✅ USANDO EXTENSÃO: Tap feedback padronizado
                await border.PerformTapFeedbackAsync();
            }

            string[] options = { "All", "Active", "Inactive" };
            string result = await DisplayActionSheet("Filter by Status", "Cancel", null, options);

            if (result != "Cancel" && result != null && _viewModel != null)
            {
                _viewModel.StatusFilter = result;
                Debug.WriteLine($"🏷️ [FAMILIES_LIST_PAGE] Status filter changed to: {result}");

                if (_viewModel.ApplyFilterCommand?.CanExecute(null) == true)
                {
                    await _viewModel.ApplyFilterCommand.ExecuteAsync(null);
                }

                // ✅ USANDO EXTENSÃO: Toast de informação padronizado
                await this.ShowInfoToast($"Filter: {result}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Filter error: {ex.Message}");
        }
    }

    private async void OnSortTapped(object sender, EventArgs e)
    {
        try
        {
            if (sender is Border border)
            {
                // ✅ USANDO EXTENSÃO: Tap feedback padronizado
                await border.PerformTapFeedbackAsync();
            }

            string[] options = { "Name A→Z", "Name Z→A", "Recent First", "Oldest First", "Favorites First" };
            string result = await DisplayActionSheet("Sort by", "Cancel", null, options);

            if (result != "Cancel" && result != null && _viewModel != null)
            {
                _viewModel.SortOrder = result;
                Debug.WriteLine($"🔄 [FAMILIES_LIST_PAGE] Sort order changed to: {result}");

                if (_viewModel.ToggleSortCommand?.CanExecute(null) == true)
                {
                    _viewModel.ToggleSortCommand.Execute(null);
                }

                // ✅ USANDO EXTENSÃO: Toast de informação padronizado
                await this.ShowInfoToast($"Sorted by: {result}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Sort error: {ex.Message}");
        }
    }

    #endregion

    #region ✅ SYNCFUSION LISTVIEW EVENT HANDLERS

    private void OnItemTapped(object sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
    {
        try
        {
            if (e.DataItem is FamilyItemViewModel item)
            {
                Debug.WriteLine($"👆 [FAMILIES_LIST_PAGE] Item tapped: {item.Name}");
                Debug.WriteLine($"👆 [FAMILIES_LIST_PAGE] Current IsMultiSelectMode: {_viewModel?.IsMultiSelectMode}");
                Debug.WriteLine($"👆 [FAMILIES_LIST_PAGE] Current SelectionMode: {FamilyListView.SelectionMode}");

                if (_viewModel?.IsMultiSelectMode == true)
                {
                    Debug.WriteLine($"🔘 [FAMILIES_LIST_PAGE] Multi-select mode - toggling selection");

                    if (item.IsSelected)
                    {
                        item.IsSelected = false;
                        _viewModel?.SelectedItems?.Remove(item);
                        FamilyListView.SelectedItems?.Remove(item);
                        Debug.WriteLine($"➖ [FAMILIES_LIST_PAGE] Deselected: {item.Name}");
                    }
                    else
                    {
                        item.IsSelected = true;
                        if (_viewModel?.SelectedItems != null && !_viewModel.SelectedItems.Contains(item))
                            _viewModel.SelectedItems.Add(item);
                        if (FamilyListView.SelectedItems != null && !FamilyListView.SelectedItems.Contains(item))
                            FamilyListView.SelectedItems.Add(item);
                        Debug.WriteLine($"➕ [FAMILIES_LIST_PAGE] Selected: {item.Name}");
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
                    Debug.WriteLine($"🔄 [FAMILIES_LIST_PAGE] Normal mode - navigating to edit: {item.Name}");
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        if (_viewModel?.NavigateToEditCommand?.CanExecute(item) == true)
                        {
                            await _viewModel.NavigateToEditCommand.ExecuteAsync(item);
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] ItemTapped error: {ex.Message}");
        }
    }

    private void OnItemLongPress(object sender, Syncfusion.Maui.ListView.ItemLongPressEventArgs e)
    {
        try
        {
            if (e.DataItem is FamilyItemViewModel item)
            {
                Debug.WriteLine($"🔘 [FAMILIES_LIST_PAGE] Long press: {item.Name}");

                if (_viewModel?.IsMultiSelectMode != true)
                {
                    if (_viewModel != null)
                    {
                        _viewModel.IsMultiSelectMode = true;
                    }
                    Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] Multi-select mode ACTIVATED");
                }

                if (!item.IsSelected)
                {
                    item.IsSelected = true;
                    if (_viewModel?.SelectedItems != null && !_viewModel.SelectedItems.Contains(item))
                    {
                        _viewModel.SelectedItems.Add(item);
                    }
                    Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] Item selected: {item.Name}");
                }

                Device.BeginInvokeOnMainThread(() =>
                {
                    var index = _viewModel?.Items?.IndexOf(item) ?? -1;
                    if (index >= 0)
                    {
                        FamilyListView.RefreshItem(index);
                        Debug.WriteLine($"🔄 [FAMILIES_LIST_PAGE] Visual refresh for index: {index}");
                    }
                });

                UpdateFabVisual();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] ItemLongPress error: {ex.Message}");
        }
    }

    private void OnSelectionChanged(object sender, Syncfusion.Maui.ListView.ItemSelectionChangedEventArgs e)
    {
        try
        {
            var selectedCount = FamilyListView.SelectedItems?.Count ?? 0;
            Debug.WriteLine($"🔘 [FAMILIES_LIST_PAGE] NATIVE Selection changed - ListView count: {selectedCount}");

            if (FamilyListView.SelectedItems != null && _viewModel?.SelectedItems != null)
            {
                _viewModel.SelectedItems.Clear();

                foreach (FamilyItemViewModel item in FamilyListView.SelectedItems)
                {
                    _viewModel.SelectedItems.Add(item);
                    item.IsSelected = true;
                }

                Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] Synced to ViewModel: {_viewModel.SelectedItems.Count} items");
            }

            if (_viewModel != null)
            {
                var vmSelectedCount = _viewModel.SelectedItems?.Count ?? 0;

                if (vmSelectedCount == 0 && _viewModel.IsMultiSelectMode)
                {
                    Debug.WriteLine($"🔄 [FAMILIES_LIST_PAGE] Auto-exiting multi-select mode");
                    _viewModel.IsMultiSelectMode = false;
                }
                else if (vmSelectedCount > 0 && !_viewModel.IsMultiSelectMode)
                {
                    _viewModel.IsMultiSelectMode = true;
                }

                UpdateFabVisual();

                Debug.WriteLine($"🎯 [FAMILIES_LIST_PAGE] ViewModel updated - MultiSelect: {_viewModel.IsMultiSelectMode}, SelectionMode: {FamilyListView.SelectionMode}");
            }

            if (e.AddedItems?.Count > 0)
            {
                foreach (FamilyItemViewModel item in e.AddedItems)
                {
                    Debug.WriteLine($"➕ [FAMILIES_LIST_PAGE] NATIVE Selected: {item.Name}");
                }
            }

            if (e.RemovedItems?.Count > 0)
            {
                foreach (FamilyItemViewModel item in e.RemovedItems)
                {
                    item.IsSelected = false;
                    Debug.WriteLine($"➖ [FAMILIES_LIST_PAGE] NATIVE Deselected: {item.Name}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] SelectionChanged error: {ex.Message}");
        }
    }

    #endregion

    #region ✅ SYNCFUSION SWIPE HANDLERS - UNIFIED DELETE FLOW

    private const double SWIPE_THRESHOLD = 0.8;

    private void OnSwipeStarting(object sender, Syncfusion.Maui.ListView.SwipeStartingEventArgs e)
    {
        try
        {
            if (e.DataItem is FamilyItemViewModel item)
            {
                Debug.WriteLine($"🚀 [SWIPE_STARTING] Item: {item.Name}");
                Debug.WriteLine($"🚀 [SWIPE_STARTING] Direction: {e.Direction}");
                Debug.WriteLine($"✅ [SWIPE_STARTING] Swipe allowed for {item.Name} - direction: {e.Direction}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [SWIPE_STARTING] Error: {ex.Message}");
        }
    }

    private void OnSwiping(object sender, Syncfusion.Maui.ListView.SwipingEventArgs e)
    {
        try
        {
            if (e.DataItem is FamilyItemViewModel item)
            {
                var offsetPercent = Math.Abs(e.Offset) / FamilyListView.SwipeOffset;
                var direction = e.Direction.ToString();
                var icon = direction == "Right" ? "⭐" : "🗑️";

                if (offsetPercent >= 0.8)
                {
                    Debug.WriteLine($"📱 [SWIPING] {icon} {item.Name} | {direction} | {offsetPercent:P0} - READY!");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [SWIPING] Error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ FIXED: UNIFIED SWIPE DELETE FLOW - Uses ViewModel command only
    /// </summary>
    private async void OnSwipeEnded(object sender, Syncfusion.Maui.ListView.SwipeEndedEventArgs e)
    {
        try
        {
            if (e.DataItem is not FamilyItemViewModel item)
            {
                Debug.WriteLine($"❌ [SWIPE_ENDED] DataItem is not FamilyItemViewModel");
                return;
            }

            var direction = e.Direction.ToString();
            var offsetPercent = Math.Abs(e.Offset) / FamilyListView.SwipeOffset;
            var icon = direction == "Right" ? "⭐" : "🗑️";

            Debug.WriteLine($"🏁 [SWIPE_ENDED] {icon} Item: {item.Name}, Direction: {direction}, Progress: {offsetPercent:P1}");

            if (offsetPercent < SWIPE_THRESHOLD)
            {
                Debug.WriteLine($"❌ [SWIPE_ENDED] INSUFFICIENT SWIPE - Need {SWIPE_THRESHOLD:P0}+, got {offsetPercent:P1}");
                FamilyListView.ResetSwipeItem();
                return;
            }

            Debug.WriteLine($"🎯 [SWIPE_ENDED] SWIPE APPROVED! Executing {icon} {direction} action");

            switch (direction)
            {
                case "Right":
                    Debug.WriteLine($"⭐ [SWIPE_ENDED] FAVORITE action triggered");

                    if (_viewModel?.IsConnected != true)
                    {
                        // ✅ USANDO EXTENSÃO: Toast de aviso padronizado
                        await this.ShowWarningToast("Cannot favorite while offline");
                        break;
                    }

                    try
                    {
                        var wasAlreadyFavorite = item.IsFavorite;

                        if (_viewModel?.ToggleFavoriteCommand?.CanExecute(item) == true)
                        {
                            await _viewModel.ToggleFavoriteCommand.ExecuteAsync(item);
                        }

                        var message = wasAlreadyFavorite ? "Removed from favorites" : "Added to favorites! ⭐";
                        // ✅ USANDO EXTENSÃO: Toast de sucesso padronizado
                        await this.ShowSuccessToast(message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ [SWIPE_ENDED] ToggleFavorite failed: {ex.Message}");
                        // ✅ USANDO EXTENSÃO: Toast de erro padronizado
                        await this.ShowErrorToast("Failed to update favorite status");
                    }
                    break;

                case "Left":
                    Debug.WriteLine($"🗑️ [SWIPE_ENDED] UNIFIED DELETE action triggered");

                    if (_viewModel?.IsConnected != true)
                    {
                        // ✅ USANDO EXTENSÃO: Toast de aviso padronizado
                        await this.ShowWarningToast("Cannot delete while offline");
                        break;
                    }

                    try
                    {
                        Debug.WriteLine($"🗑️ [SWIPE_ENDED] Calling UNIFIED DeleteSingleCommand for: {item.Name}");

                        // ✅ UNIFIED: Use ViewModel command only - it handles confirmation + success message
                        if (_viewModel?.DeleteSingleCommand?.CanExecute(item) == true)
                        {
                            await _viewModel.DeleteSingleCommand.ExecuteAsync(item);
                            Debug.WriteLine($"✅ [SWIPE_ENDED] UNIFIED delete completed via ViewModel");
                        }

                        // ✅ NO additional messages here - ViewModel already showed success toast
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ [SWIPE_ENDED] UNIFIED Delete failed: {ex.Message}");
                        // ViewModel already showed error message
                    }
                    break;

                default:
                    Debug.WriteLine($"⚠️ [SWIPE_ENDED] Unknown direction: {direction}");
                    break;
            }

            FamilyListView.ResetSwipeItem();
            Debug.WriteLine($"🔄 [SWIPE_ENDED] Auto-reset completed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [SWIPE_ENDED] ERROR: {ex.Message}");
            FamilyListView.ResetSwipeItem();
            await DisplayAlert("Error", $"Swipe action failed: {ex.Message}", "OK");
        }
    }
    #endregion

    // ✅ REMOVIDO: Método ShowToast antigo - agora usando ToastExtensions padronizado
}