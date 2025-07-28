using OrchidPro.ViewModels.Families;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace OrchidPro.Views.Pages;

/// <summary>
/// ✅ FIXED: FamiliesListPage with corrected SelectionMode synchronization
/// </summary>
public partial class FamiliesListPage : ContentPage
{
    private readonly FamiliesListViewModel _viewModel;

    public FamiliesListPage(FamiliesListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        Debug.WriteLine("✅ [FAMILIES_LIST_PAGE] Initialized with corrected synchronization");

        // Hook up events
        ListRefresh.Refreshing += PullToRefresh_Refreshing;

        // ✅ CRITICAL: Monitor ViewModel changes to sync SelectionMode
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        if (_viewModel?.SelectedItems == null)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] ViewModel.SelectedItems is NULL during initialization!");
        }
    }

    /// <summary>
    /// ✅ CRITICAL: Sync ListView SelectionMode with ViewModel state
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
    /// ✅ CRITICAL: Ensure ListView SelectionMode matches ViewModel state
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

            // If we're exiting multi-select, clear ListView selections
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

        // ✅ CRITICAL: Ensure SelectionMode is synced on appearing
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
            RootGrid.Opacity = 0;
            RootGrid.Scale = 0.95;
            RootGrid.TranslationY = 30;

            FabButton.Opacity = 0;
            FabButton.Scale = 0.8;
            FabButton.TranslationY = 50;

            await Task.WhenAll(
                RootGrid.FadeTo(1, 600, Easing.CubicOut),
                RootGrid.ScaleTo(1, 600, Easing.SpringOut),
                RootGrid.TranslateTo(0, 0, 600, Easing.CubicOut)
            );

            await Task.Delay(200);
            await Task.WhenAll(
                FabButton.FadeTo(1, 400, Easing.CubicOut),
                FabButton.ScaleTo(1, 400, Easing.SpringOut),
                FabButton.TranslateTo(0, 0, 400, Easing.CubicOut)
            );
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
            await Task.WhenAll(
                RootGrid.FadeTo(0.8, 300, Easing.CubicIn),
                RootGrid.ScaleTo(0.98, 300, Easing.CubicIn),
                FabButton.FadeTo(0, 200, Easing.CubicIn),
                FabButton.ScaleTo(0.9, 200, Easing.CubicIn)
            );
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Exit animation error: {ex.Message}");
        }
    }

    #endregion

    #region ✅ FAB VISUAL UPDATES - CORRIGIDO PARA BUTTON

    /// <summary>
    /// ✅ CORRIGIDO: Atualizar visual do FAB usando cores do ResourceDictionary
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
                        : Color.FromArgb("#D32F2F");

                    FabButton.BackgroundColor = errorColor;
                    FabButton.Text = $"Delete ({selectedCount})";
                    Debug.WriteLine($"🔴 [FAB_VISUAL] Set to DELETE mode: Delete ({selectedCount})");
                }
                else if (_viewModel?.IsMultiSelectMode == true)
                {
                    var grayColor = Application.Current?.Resources.TryGetValue("Gray500", out var gray) == true
                        ? (Color)gray
                        : Color.FromArgb("#757575");

                    FabButton.BackgroundColor = grayColor;
                    FabButton.Text = "Cancel";
                    Debug.WriteLine($"⚫ [FAB_VISUAL] Set to CANCEL mode");
                }
                else
                {
                    var primaryColor = Application.Current?.Resources.TryGetValue("Primary", out var primary) == true
                        ? (Color)primary
                        : Color.FromArgb("#A47764");

                    FabButton.BackgroundColor = primaryColor;
                    FabButton.Text = "Add Family";
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
                FabButton.Text = "Add Family";

                var primaryColor = Application.Current?.Resources.TryGetValue("Primary", out var primary) == true
                    ? (Color)primary
                    : Color.FromArgb("#A47764");
                FabButton.BackgroundColor = primaryColor;
            });
        }
    }
    #endregion

    #region ✅ TOOLBAR ITEMS HANDLERS - FIXED

    /// <summary>
    /// ✅ FIXED: Select All toolbar with proper SelectionMode sync
    /// </summary>
    private void OnSelectAllTapped(object sender, EventArgs e)
    {
        try
        {
            Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] Select All toolbar tapped");

            // ✅ CRITICAL: Ensure ListView is in Multiple mode FIRST
            if (FamilyListView.SelectionMode != Syncfusion.Maui.ListView.SelectionMode.Multiple)
            {
                Debug.WriteLine($"🔄 [FAMILIES_LIST_PAGE] Setting ListView to Multiple mode for Select All");
                FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.Multiple;
            }

            // Execute ViewModel command
            if (_viewModel?.SelectAllCommand?.CanExecute(null) == true)
            {
                _viewModel.SelectAllCommand.Execute(null);
                Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] SelectAllCommand executed");
            }

            // ✅ CRITICAL: Sync ListView selections manually
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

                    // Force refresh visuals
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
    /// ✅ FIXED: Clear All with proper cleanup
    /// </summary>
    private async void OnDeselectAllTapped(object sender, EventArgs e)
    {
        try
        {
            Debug.WriteLine($"🧹 [FAMILIES_LIST_PAGE] Clear All toolbar tapped");

            // Execute ViewModel command
            if (_viewModel?.ClearSelectionCommand != null && _viewModel.ClearSelectionCommand.CanExecute(null))
            {
                _viewModel.ClearSelectionCommand.Execute(null);
                Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] ClearSelectionCommand executed");
            }

            // ✅ CRITICAL: Ensure ListView is properly cleared
            Device.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    // Clear ListView selections
                    FamilyListView.SelectedItems?.Clear();

                    // Set SelectionMode to None
                    FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.None;

                    // Force refresh all items
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

            await ShowToast("🧹 Cleared selections and filters", CommunityToolkit.Maui.Core.ToastDuration.Short);

            Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] Clear All completed successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Clear All error: {ex.Message}");
            await ShowToast($"❌ Failed to clear: {ex.Message}", CommunityToolkit.Maui.Core.ToastDuration.Short);
        }
    }
    #endregion

    #region ✅ SEARCH BAR EVENTS - ATUALIZADO

    private async void OnSearchFocused(object sender, FocusEventArgs e)
    {
        try
        {
            if (sender is Entry entry && entry.Parent?.Parent is Border border)
            {
                await border.ScaleTo(1.02, 150, Easing.CubicOut);
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
                await border.ScaleTo(1, 150, Easing.CubicOut);
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

    #region ✅ FAB HANDLER - FIXED

    /// <summary>
    /// ✅ FIXED: FAB handler with proper state management
    /// </summary>
    private async void OnFabPressed(object sender, EventArgs e)
    {
        try
        {
            await FabButton.ScaleTo(0.9, 100, Easing.CubicIn);
            await FabButton.ScaleTo(1, 100, Easing.CubicOut);

            var selectedCount = _viewModel?.SelectedItems?.Count ?? 0;
            Debug.WriteLine($"🎯 [FAMILIES_LIST_PAGE] FAB pressed - Selected: {selectedCount}, MultiSelect: {_viewModel?.IsMultiSelectMode}");

            if (selectedCount > 0)
            {
                Debug.WriteLine($"🗑️ [FAMILIES_LIST_PAGE] Executing delete for {selectedCount} items");

                try
                {
                    if (_viewModel?.DeleteSelectedCommand?.CanExecute(null) == true)
                    {
                        await _viewModel.DeleteSelectedCommand.ExecuteAsync(null);
                    }

                    // ✅ CRITICAL: Force complete UI cleanup after delete
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        try
                        {
                            // Clear ListView selections
                            FamilyListView.SelectedItems?.Clear();

                            // Set SelectionMode to None
                            FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.None;

                            // Refresh all remaining items
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

                    await ShowToast($"✅ {selectedCount} families deleted successfully", CommunityToolkit.Maui.Core.ToastDuration.Short);
                }
                catch (Exception deleteEx)
                {
                    Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Delete failed: {deleteEx.Message}");
                    await ShowToast($"❌ Failed to delete families: {deleteEx.Message}", CommunityToolkit.Maui.Core.ToastDuration.Long);
                }
            }
            else if (_viewModel?.IsMultiSelectMode == true)
            {
                Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Canceling multi-select mode");

                if (_viewModel != null)
                {
                    _viewModel.IsMultiSelectMode = false;
                }

                // UI will be synced via OnViewModelPropertyChanged
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
            await ShowToast($"❌ Action failed: {ex.Message}", CommunityToolkit.Maui.Core.ToastDuration.Short);
        }
    }

    #endregion

    #region ✅ FILTER AND SORT HANDLERS - ATUALIZADO

    private async void OnFilterTapped(object sender, EventArgs e)
    {
        try
        {
            if (sender is Border border)
            {
                await border.ScaleTo(0.9, 100, Easing.CubicOut);
                await border.ScaleTo(1, 100, Easing.CubicOut);
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

                var toast = Toast.Make($"Filter: {result}", CommunityToolkit.Maui.Core.ToastDuration.Short, 14);
                await toast.Show();
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
                await border.ScaleTo(0.9, 100, Easing.CubicOut);
                await border.ScaleTo(1, 100, Easing.CubicOut);
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

                var toast = Toast.Make($"Sorted by: {result}", CommunityToolkit.Maui.Core.ToastDuration.Short, 14);
                await toast.Show();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Sort error: {ex.Message}");
        }
    }

    #endregion

    #region ✅ SYNCFUSION LISTVIEW EVENT HANDLERS - FIXED

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

                    // Toggle selection
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

                    // If no more items selected, exit multi-select mode
                    if (_viewModel?.SelectedItems?.Count == 0)
                    {
                        if (_viewModel != null)
                        {
                            _viewModel.IsMultiSelectMode = false;
                        }
                        // SelectionMode will be synced via OnViewModelPropertyChanged
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

                // Activate multi-select mode
                if (_viewModel?.IsMultiSelectMode != true)
                {
                    if (_viewModel != null)
                    {
                        _viewModel.IsMultiSelectMode = true;
                    }
                    // SelectionMode will be synced via OnViewModelPropertyChanged
                    Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] Multi-select mode ACTIVATED");
                }

                // Select the item
                if (!item.IsSelected)
                {
                    item.IsSelected = true;
                    if (_viewModel?.SelectedItems != null && !_viewModel.SelectedItems.Contains(item))
                    {
                        _viewModel.SelectedItems.Add(item);
                    }
                    Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] Item selected: {item.Name}");
                }

                // Force visual refresh
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
                    // SelectionMode will be synced via OnViewModelPropertyChanged
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

    #region ✅ SYNCFUSION SWIPE HANDLERS - OTIMIZADO

    private const double SWIPE_THRESHOLD = 0.8;

    private void OnSwipeStarting(object sender, Syncfusion.Maui.ListView.SwipeStartingEventArgs e)
    {
        try
        {
            if (e.DataItem is FamilyItemViewModel item)
            {
                Debug.WriteLine($"🚀 [SWIPE_STARTING] Item: {item.Name}");
                Debug.WriteLine($"🚀 [SWIPE_STARTING] Direction: {e.Direction}");
                Debug.WriteLine($"🚀 [SWIPE_STARTING] IsSystemDefault: {item.IsSystemDefault}");

                // ✅ REMOVED: No longer blocking system defaults since you don't use this feature
                // if (item.IsSystemDefault && e.Direction.ToString() == "Left")
                // {
                //     e.Cancel = true;
                //     Debug.WriteLine($"❌ [SWIPE_STARTING] Cancelled left swipe for system default: {item.Name}");
                //     return;
                // }

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
                        await ShowToast("Cannot favorite while offline", CommunityToolkit.Maui.Core.ToastDuration.Short);
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
                        await ShowToast(message, CommunityToolkit.Maui.Core.ToastDuration.Short);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ [SWIPE_ENDED] ToggleFavorite failed: {ex.Message}");
                        await ShowToast("Failed to update favorite status", CommunityToolkit.Maui.Core.ToastDuration.Short);
                    }
                    break;

                case "Left":
                    Debug.WriteLine($"🗑️ [SWIPE_ENDED] DELETE action triggered");

                    if (_viewModel?.IsConnected != true)
                    {
                        await ShowToast("Cannot delete while offline", CommunityToolkit.Maui.Core.ToastDuration.Short);
                        break;
                    }

                    // ✅ REMOVED: No longer blocking system defaults since you don't use this feature
                    // if (item.IsSystemDefault)
                    // {
                    //     await ShowToast("Cannot delete system default families", CommunityToolkit.Maui.Core.ToastDuration.Short);
                    //     break;
                    // }

                    try
                    {
                        Debug.WriteLine($"🗑️ [SWIPE_ENDED] Calling DeleteSingleCommand for: {item.Name}");

                        // ✅ FIXED: Use the correct command
                        if (_viewModel?.DeleteSingleCommand?.CanExecute(item) == true)
                        {
                            await _viewModel.DeleteSingleCommand.ExecuteAsync(item);
                        }

                        await ShowToast($"✅ '{item.Name}' deleted successfully", CommunityToolkit.Maui.Core.ToastDuration.Short);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ [SWIPE_ENDED] Delete failed: {ex.Message}");
                        await ShowToast("❌ Failed to delete family", CommunityToolkit.Maui.Core.ToastDuration.Short);
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

    #region ✅ UTILITY METHODS

    private async Task ShowToast(string message, CommunityToolkit.Maui.Core.ToastDuration duration)
    {
        try
        {
            var toast = Toast.Make(message, duration);
            await toast.Show();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Toast error: {ex.Message}");
            await DisplayAlert("Info", message, "OK");
        }
    }

    #endregion
}