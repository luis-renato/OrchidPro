using OrchidPro.ViewModels.Families;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace OrchidPro.Views.Pages;

/// <summary>
/// ✅ CORRIGIDO: FamiliesListSyncfusionPage com binding e eventos resolvidos
/// </summary>
public partial class FamiliesListSyncfusionPage : ContentPage
{
    private readonly FamiliesListSyncfusionViewModel _viewModel;

    public FamiliesListSyncfusionPage(FamiliesListSyncfusionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        Debug.WriteLine("✅ [FAMILIES_SYNCFUSION_PAGE] Initialized with corrected ItemsSource binding");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            Debug.WriteLine("🚀 [FAMILIES_SYNCFUSION_PAGE] OnAppearing - Starting initialization");

            // Perform entrance animation and load data in parallel
            var animationTask = PerformEntranceAnimation();
            var dataTask = _viewModel.OnAppearingAsync();

            await Task.WhenAll(animationTask, dataTask);

            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_PAGE] OnAppearing completed - Items count: {_viewModel.Items.Count}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] OnAppearing error: {ex.Message}");
        }
    }

    #region Animation Methods

    /// <summary>
    /// ✅ Smooth entrance animation
    /// </summary>
    private async Task PerformEntranceAnimation()
    {
        try
        {
            // Initial state
            ListRootGrid.Opacity = 0;
            ListRootGrid.Scale = 0.95;
            ListRootGrid.TranslationY = 30;

            // Smooth entrance animation
            await Task.WhenAll(
                ListRootGrid.FadeTo(1, 600, Easing.CubicOut),
                ListRootGrid.ScaleTo(1, 600, Easing.SpringOut),
                ListRootGrid.TranslateTo(0, 0, 600, Easing.CubicOut)
            );

            Debug.WriteLine("✅ [FAMILIES_SYNCFUSION_PAGE] Entrance animation completed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Animation error: {ex.Message}");
            ListRootGrid.Opacity = 1;
        }
    }

    #endregion

    #region Search and Filter Event Handlers

    private void OnSearchFocused(object sender, FocusEventArgs e)
    {
        try
        {
            if (sender is Entry entry && e.IsFocused)
            {
                // Subtle focus animation
                if (entry.Parent is Border border)
                {
                    _ = border.ScaleTo(1.02, 150, Easing.CubicOut);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Search focus error: {ex.Message}");
        }
    }

    private void OnSearchUnfocused(object sender, FocusEventArgs e)
    {
        try
        {
            if (sender is Entry entry && !e.IsFocused)
            {
                // Subtle unfocus animation
                if (entry.Parent is Border border)
                {
                    _ = border.ScaleTo(1, 150, Easing.CubicOut);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Search unfocus error: {ex.Message}");
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            // Apply filter in real-time with debounce
            Debug.WriteLine($"🔍 [FAMILIES_SYNCFUSION_PAGE] Search text changed: '{e.NewTextValue}'");
            _viewModel.ApplyFilterCommand.Execute(null);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Search text change error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handler for status filter button
    /// </summary>
    private async void OnFilterTapped(object sender, EventArgs e)
    {
        try
        {
            string[] options = { "All", "Active", "Inactive" };
            string result = await DisplayActionSheet("Filter by Status", "Cancel", null, options);

            if (result != "Cancel" && result != null)
            {
                _viewModel.StatusFilter = result;
                Debug.WriteLine($"📊 [FAMILIES_SYNCFUSION_PAGE] Status filter changed to: {result}");
                _viewModel.ApplyFilterCommand.Execute(null);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Filter error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handler for Sort button
    /// </summary>
    private async void OnSortTapped(object sender, EventArgs e)
    {
        try
        {
            string[] options = { "Name A→Z", "Name Z→A", "Recent First", "Oldest First", "Favorites First" };
            string result = await DisplayActionSheet("Sort by", "Cancel", null, options);

            if (result != "Cancel" && result != null)
            {
                _viewModel.SortOrder = result;
                Debug.WriteLine($"🔄 [FAMILIES_SYNCFUSION_PAGE] Sort order changed to: {result}");
                _viewModel.ToggleSortCommand.Execute(null);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Sort error: {ex.Message}");
        }
    }

    #endregion

    #region Syncfusion ListView Event Handlers

    private void OnItemTapped(object sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
    {
        try
        {
            if (e.DataItem is FamilyItemViewModel item)
            {
                Debug.WriteLine($"👆 [FAMILIES_SYNCFUSION_PAGE] Item tapped: {item.Name} - MultiSelect: {_viewModel.IsMultiSelectMode}");

                if (!_viewModel.IsMultiSelectMode)
                {
                    Debug.WriteLine($"🔄 [FAMILIES_SYNCFUSION_PAGE] Navigating to edit: {item.Name}");
                    _viewModel.NavigateToEditCommand.Execute(item);
                }
                else
                {
                    Debug.WriteLine($"🔘 [FAMILIES_SYNCFUSION_PAGE] In multi-select mode - toggling selection");
                    item.ToggleFamilySelectionCommand.Execute(null);
                    _viewModel.UpdateFabForSelection();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Item tap error: {ex.Message}");
        }
    }

    private void OnItemLongPress(object sender, Syncfusion.Maui.ListView.ItemLongPressEventArgs e)
    {
        try
        {
            if (!_viewModel.IsMultiSelectMode)
            {
                Debug.WriteLine($"🔘 [FAMILIES_SYNCFUSION_PAGE] Long press - entering multi-select mode");
                _viewModel.ToggleMultiSelectCommand.Execute(null);

                // Activate multiple selection mode in ListView
                FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.Multiple;

                // Select the item that was pressed
                if (e.DataItem is FamilyItemViewModel item)
                {
                    item.IsSelected = true;
                    if (!_viewModel.SelectedItems.Contains(item))
                    {
                        _viewModel.SelectedItems.Add(item);
                    }
                    _viewModel.UpdateFabForSelection();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Long press error: {ex.Message}");
        }
    }

    private void OnSelectionChanged(object sender, Syncfusion.Maui.ListView.ItemSelectionChangedEventArgs e)
    {
        try
        {
            Debug.WriteLine($"🔄 [FAMILIES_SYNCFUSION_PAGE] Selection changed - Added: {e.AddedItems?.Count ?? 0}, Removed: {e.RemovedItems?.Count ?? 0}");

            // Synchronize selections with ViewModel
            if (e.AddedItems != null)
            {
                foreach (FamilyItemViewModel item in e.AddedItems)
                {
                    if (!item.IsSelected)
                    {
                        item.IsSelected = true;
                    }
                    if (!_viewModel.SelectedItems.Contains(item))
                    {
                        _viewModel.SelectedItems.Add(item);
                    }
                }
            }

            if (e.RemovedItems != null)
            {
                foreach (FamilyItemViewModel item in e.RemovedItems)
                {
                    if (item.IsSelected)
                    {
                        item.IsSelected = false;
                    }
                    if (_viewModel.SelectedItems.Contains(item))
                    {
                        _viewModel.SelectedItems.Remove(item);
                    }
                }
            }

            _viewModel.UpdateFabForSelection();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Selection change error: {ex.Message}");
        }
    }

    #endregion

    #region Swipe Event Handlers (Disabled - Using Context Menu Instead)

    private void OnSwipeStarting(object sender, Syncfusion.Maui.ListView.SwipeStartingEventArgs e)
    {
        try
        {
            Debug.WriteLine($"🔄 [FAMILIES_SYNCFUSION_PAGE] Swipe starting on item");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Swipe starting error: {ex.Message}");
        }
    }

    private void OnSwiping(object sender, Syncfusion.Maui.ListView.SwipingEventArgs e)
    {
        // Minimal logging for performance
    }

    private void OnSwipeEnded(object sender, Syncfusion.Maui.ListView.SwipeEndedEventArgs e)
    {
        try
        {
            Debug.WriteLine($"✋ [FAMILIES_SYNCFUSION_PAGE] SwipeEnded event triggered");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] SwipeEnded error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ Context menu para ações (substituindo swipe)
    /// </summary>
    private async void OnItemContextMenuRequested(object sender, EventArgs e)
    {
        try
        {
            if (sender is Element element && element.BindingContext is FamilyItemViewModel item)
            {
                var actions = new List<string> { "Edit ✏️" };

                if (item.IsFavorite)
                    actions.Add("Remove from Favorites ⭐");
                else
                    actions.Add("Add to Favorites ⭐");

                if (!item.IsSystemDefault)
                    actions.Add("Delete 🗑️");

                var result = await DisplayActionSheet(
                    $"Actions for '{item.Name}'",
                    "Cancel",
                    null,
                    actions.ToArray()
                );

                switch (result)
                {
                    case "Edit ✏️":
                        await _viewModel.NavigateToEditCommand.ExecuteAsync(item);
                        break;

                    case "Add to Favorites ⭐":
                    case "Remove from Favorites ⭐":
                        await _viewModel.ToggleFavoriteCommand.ExecuteAsync(item);
                        break;

                    case "Delete 🗑️":
                        var confirm = await DisplayAlert(
                            "Delete Family",
                            $"Are you sure you want to delete '{item.Name}'?",
                            "Delete",
                            "Cancel"
                        );

                        if (confirm)
                        {
                            await _viewModel.DeleteSingleCommand.ExecuteAsync(item);
                        }
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Context menu error: {ex.Message}");
        }
    }

    #endregion

    #region Public Methods for ViewModel Interaction

    /// <summary>
    /// Refresh with visual feedback
    /// </summary>
    public void RefreshListView()
    {
        try
        {
            ListRefresh.IsRefreshing = true;
            Debug.WriteLine($"🔄 [FAMILIES_SYNCFUSION_PAGE] ListView refresh triggered via SfPullToRefresh");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] RefreshListView error: {ex.Message}");
        }
    }

    /// <summary>
    /// Clear selections with smooth transition
    /// </summary>
    public async void ClearSelections()
    {
        try
        {
            FamilyListView.SelectedItems?.Clear();
            _viewModel.SelectedItems.Clear();

            foreach (var item in _viewModel.Items)
            {
                item.IsSelected = false;
            }

            _viewModel.IsMultiSelectMode = false;
            FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.None;
            _viewModel.UpdateFabForSelection();

            // Smooth animation out of selection mode
            await Task.Delay(100);

            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_PAGE] All selections cleared and SelectionMode = None");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] ClearSelections error: {ex.Message}");
        }
    }

    /// <summary>
    /// Select all items
    /// </summary>
    public void SelectAllItems()
    {
        try
        {
            FamilyListView.SelectAll();
            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_PAGE] Selected all items via Syncfusion native method");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] SelectAllItems error: {ex.Message}");
        }
    }

    /// <summary>
    /// Toggle selection mode with better UX
    /// </summary>
    public void ToggleSelectionMode()
    {
        try
        {
            if (FamilyListView.SelectionMode == Syncfusion.Maui.ListView.SelectionMode.None)
            {
                FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.Multiple;
                Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_PAGE] Entered multi-selection mode");
            }
            else
            {
                FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.None;
                ClearSelections();
                Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_PAGE] Exited multi-selection mode");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] ToggleSelectionMode error: {ex.Message}");
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Scroll to top method
    /// </summary>
    public void ScrollToTop()
    {
        try
        {
            if (_viewModel.Items.Any())
            {
                Debug.WriteLine($"📜 [FAMILIES_SYNCFUSION_PAGE] Scroll to top requested - {_viewModel.Items.Count} items available");
                // Note: Actual scrolling implementation depends on Syncfusion version
                // This method is available for future implementation when needed
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] ScrollToTop error: {ex.Message}");
        }
    }

    /// <summary>
    /// Get selected items count
    /// </summary>
    public int GetSelectedItemsCount()
    {
        try
        {
            return _viewModel.SelectedItems?.Count ?? 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] GetSelectedItemsCount error: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// ✅ NOVO: Force refresh da ListView
    /// </summary>
    public async Task ForceRefreshAsync()
    {
        try
        {
            Debug.WriteLine($"🔄 [FAMILIES_SYNCFUSION_PAGE] Force refresh requested");
            await _viewModel.RefreshCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] ForceRefreshAsync error: {ex.Message}");
        }
    }

    #endregion
}