using OrchidPro.ViewModels.Families;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace OrchidPro.Views.Pages;

/// <summary>
/// ✅ COMPLETO: FamiliesListSyncfusionPage com sintaxe corrigida
/// </summary>
public partial class FamiliesListSyncfusionPage : ContentPage
{
    private readonly FamiliesListSyncfusionViewModel _viewModel;

    public FamiliesListSyncfusionPage(FamiliesListSyncfusionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        Debug.WriteLine("✅ [FAMILIES_SYNCFUSION_PAGE] Initialized with corrected syntax");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            if (ListView.SelectedItems == null)
            {
                Debug.WriteLine($"🔧 [FAMILIES_SYNCFUSION_PAGE] Initializing ListView.SelectedItems");
                await Task.Delay(100);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Error checking SelectedItems: {ex.Message}");
        }

        await PerformEntranceAnimation();
        await _viewModel.OnAppearingAsync();
    }

    #region Animation Methods

    /// <summary>
    /// ✅ Animação de entrada mais suave
    /// </summary>
    private async Task PerformEntranceAnimation()
    {
        try
        {
            // Estado inicial
            RootGrid.Opacity = 0;
            RootGrid.Scale = 0.95;
            RootGrid.TranslationY = 30;

            // Animação de entrada mais suave
            await Task.WhenAll(
                RootGrid.FadeTo(1, 600, Easing.CubicOut),
                RootGrid.ScaleTo(1, 600, Easing.SpringOut),
                RootGrid.TranslateTo(0, 0, 600, Easing.CubicOut)
            );

            Debug.WriteLine("✅ [FAMILIES_SYNCFUSION_PAGE] Entrance animation completed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Animation error: {ex.Message}");
            RootGrid.Opacity = 1;
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
                // Animação sutil de foco
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
                // Animação sutil de unfocus
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
            // ✅ Aplicar filtro em tempo real
            _viewModel.ApplyFilterCommand.Execute(null);
            Debug.WriteLine($"🔍 [FAMILIES_SYNCFUSION_PAGE] Search text changed: '{e.NewTextValue}'");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Search text change error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handler para o botão de filtro de status
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
    /// Handler para o botão de Sort
    /// </summary>
    private async void OnSortTapped(object sender, EventArgs e)
    {
        try
        {
            string[] options = { "Name A→Z", "Name Z→A", "Recent First", "Oldest First" };
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

    /// <summary>
    /// Handler para toggle de multi-seleção
    /// </summary>
    private void OnMultiSelectTapped(object sender, EventArgs e)
    {
        try
        {
            Debug.WriteLine($"🔘 [FAMILIES_SYNCFUSION_PAGE] Multi-select toggle tapped");
            _viewModel.ToggleMultiSelectCommand.Execute(null);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Multi-select toggle error: {ex.Message}");
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

                if (!_viewModel.IsMultiSelectMode && ListView.SelectionMode == Syncfusion.Maui.ListView.SelectionMode.None)
                {
                    Debug.WriteLine($"🔄 [FAMILIES_SYNCFUSION_PAGE] Navigating to edit: {item.Name}");
                    _viewModel.NavigateToEditCommand.Execute(item);
                }
                else
                {
                    Debug.WriteLine($"🔘 [FAMILIES_SYNCFUSION_PAGE] In multi-select mode - toggling selection");
                    item.ToggleSelectionCommand.Execute(null);
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

                // Ativar modo de seleção múltipla no ListView
                ListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.Multiple;

                // Selecionar o item que foi pressionado
                if (e.DataItem is FamilyItemViewModel item)
                {
                    item.IsSelected = true;
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

            // Sincronizar seleções com o ViewModel
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
        try
        {
            // Log mínimo para debug
            Debug.WriteLine($"🔄 [FAMILIES_SYNCFUSION_PAGE] Swiping");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Swiping error: {ex.Message}");
        }
    }

    /// <summary>
    /// Funcionalidade de swipe original preservada
    /// </summary>
    private async void OnSwipeEnded(object sender, Syncfusion.Maui.ListView.SwipeEndedEventArgs e)
    {
        try
        {
            // Usar DataItem que existe no evento
            if (e.DataItem is not FamilyItemViewModel item)
            {
                Debug.WriteLine($"⚠️ [FAMILIES_SYNCFUSION_PAGE] SwipeEnded: DataItem is not FamilyItemViewModel");
                return;
            }

            Debug.WriteLine($"✋ [FAMILIES_SYNCFUSION_PAGE] SwipeEnded - Item: {item.Name}");

            // ActionSheet para escolher ação do swipe
            var result = await DisplayActionSheet(
                $"Action for '{item.Name}'",
                "Cancel",
                null,
                "Toggle Favorite ⭐",
                "Delete 🗑️"
            );

            switch (result)
            {
                case "Toggle Favorite ⭐":
                    Debug.WriteLine($"⭐ [FAMILIES_SYNCFUSION_PAGE] Toggle favorite for: {item.Name}");
                    await _viewModel.ToggleFavoriteCommand.ExecuteAsync(item);
                    break;

                case "Delete 🗑️":
                    if (item.IsSystemDefault)
                    {
                        var toast = Toast.Make("System families cannot be deleted", ToastDuration.Short, 14);
                        await toast.Show();
                        return;
                    }

                    Debug.WriteLine($"🗑️ [FAMILIES_SYNCFUSION_PAGE] Delete action for: {item.Name}");

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
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] SwipeEnded error: {ex.Message}");
        }
    }

    #endregion

    #region Public Methods for ViewModel Interaction

    /// <summary>
    /// Refresh com feedback visual
    /// </summary>
    public void RefreshListView()
    {
        try
        {
            PullToRefresh.IsRefreshing = true;
            Debug.WriteLine($"🔄 [FAMILIES_SYNCFUSION_PAGE] ListView refresh triggered via SfPullToRefresh");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] RefreshListView error: {ex.Message}");
        }
    }

    /// <summary>
    /// Clear selections com transição suave
    /// </summary>
    public async void ClearSelections()
    {
        try
        {
            ListView.SelectedItems?.Clear();
            _viewModel.SelectedItems.Clear();

            foreach (var item in _viewModel.Items)
            {
                item.IsSelected = false;
            }

            _viewModel.IsMultiSelectMode = false;
            ListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.None;
            _viewModel.UpdateFabForSelection();

            // Animação suave de saída do modo seleção
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
            ListView.SelectAll();
            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_PAGE] Selected all items via Syncfusion native method");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] SelectAllItems error: {ex.Message}");
        }
    }

    /// <summary>
    /// Toggle selection mode com melhor UX
    /// </summary>
    public void ToggleSelectionMode()
    {
        try
        {
            if (ListView.SelectionMode == Syncfusion.Maui.ListView.SelectionMode.None)
            {
                ListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.Multiple;
                Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_PAGE] Entered multi-selection mode");
            }
            else
            {
                ListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.None;
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
                // ✅ CORRIGIDO: ScrollTo precisa de índice, não do item
                ListView.ScrollTo(0);
                Debug.WriteLine($"📜 [FAMILIES_SYNCFUSION_PAGE] Scrolled to top");
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

    #endregion
}