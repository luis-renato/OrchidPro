using OrchidPro.ViewModels.Families;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace OrchidPro.Views.Pages;

/// <summary>
/// ✅ CORRIGIDO: FamiliesListSyncfusionPage com binding direto funcionando
/// </summary>
public partial class FamiliesListSyncfusionPage : ContentPage
{
    private readonly FamiliesListSyncfusionViewModel _viewModel;

    public FamiliesListSyncfusionPage(FamiliesListSyncfusionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        Debug.WriteLine("✅ [FAMILIES_SYNCFUSION_PAGE] Initialized with direct Items binding");

        // ✅ ADICIONADO: Hook do evento Refreshing como no exemplo oficial
        ListRefresh.Refreshing += PullToRefresh_Refreshing;
    }

    /// <summary>
    /// ✅ ADICIONADO: Handler do evento Refreshing (como no exemplo oficial)
    /// </summary>
    private async void PullToRefresh_Refreshing(object? sender, EventArgs e)
    {
        try
        {
            Debug.WriteLine("🔄 [FAMILIES_SYNCFUSION_PAGE] Pull-to-refresh triggered");

            ListRefresh.IsRefreshing = true;

            // Execute o refresh do ViewModel
            await _viewModel.RefreshCommand.ExecuteAsync(null);

            ListRefresh.IsRefreshing = false;

            Debug.WriteLine("✅ [FAMILIES_SYNCFUSION_PAGE] Pull-to-refresh completed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Pull-to-refresh error: {ex.Message}");
            ListRefresh.IsRefreshing = false;
        }
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

            // ✅ DIAGNÓSTICO: Log adicional para debug
            if (_viewModel.Items.Any())
            {
                Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_PAGE] First item: {_viewModel.Items.First().Name}");
                Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_PAGE] Last item: {_viewModel.Items.Last().Name}");
            }
            else
            {
                Debug.WriteLine("⚠️ [FAMILIES_SYNCFUSION_PAGE] No items in collection!");
                Debug.WriteLine($"⚠️ [FAMILIES_SYNCFUSION_PAGE] ViewModel HasData: {_viewModel.HasData}");
                Debug.WriteLine($"⚠️ [FAMILIES_SYNCFUSION_PAGE] ViewModel IsLoading: {_viewModel.IsLoading}");
                Debug.WriteLine($"⚠️ [FAMILIES_SYNCFUSION_PAGE] ViewModel IsConnected: {_viewModel.IsConnected}");
            }
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
            // ✅ CORREÇÃO: Remover aplicação direta de filtro - deixar o debounce do ViewModel funcionar
            Debug.WriteLine($"🔍 [FAMILIES_SYNCFUSION_PAGE] Search text changed: '{e.NewTextValue}'");
            // O ViewModel já tem debounce no OnSearchTextChanged
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
                // O StatusFilter já tem OnChanged que dispara o filtro
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
                await _viewModel.ToggleSortCommand.ExecuteAsync(null);
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

    #region Swipe Event Handlers

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

    /// <summary>
    /// ✅ DIAGNÓSTICO: Método para debugar binding
    /// </summary>
    public void DiagnoseBinding()
    {
        try
        {
            Debug.WriteLine("🔍 [FAMILIES_SYNCFUSION_PAGE] DIAGNÓSTICO DE BINDING:");
            Debug.WriteLine($"  - BindingContext: {BindingContext?.GetType().Name ?? "NULL"}");
            Debug.WriteLine($"  - ViewModel Items Count: {_viewModel?.Items?.Count ?? -1}");
            Debug.WriteLine($"  - ListView ItemsSource: {FamilyListView?.ItemsSource?.GetType().Name ?? "NULL"}");

            if (FamilyListView?.ItemsSource is System.Collections.IEnumerable enumerable)
            {
                var count = 0;
                foreach (var item in enumerable)
                {
                    count++;
                    if (count <= 3) // Log primeiros 3 items
                    {
                        Debug.WriteLine($"    Item {count}: {item?.GetType().Name} - {item}");
                    }
                }
                Debug.WriteLine($"  - Total items in ItemsSource: {count}");
            }

            Debug.WriteLine($"  - ListView Visibility: {FamilyListView?.IsVisible}");
            Debug.WriteLine($"  - ListView Height: {FamilyListView?.Height}");
            Debug.WriteLine($"  - ListView Width: {FamilyListView?.Width}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] DiagnoseBinding error: {ex.Message}");
        }
    }

    #endregion
}