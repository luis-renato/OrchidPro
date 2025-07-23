using OrchidPro.ViewModels.Families;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace OrchidPro.Views.Pages;

/// <summary>
/// ✅ CORRIGIDO: FamiliesListSyncfusionPage compatível com estrutura existente
/// - Remove referências ao DataSource do Syncfusion
/// - Usa BaseItemViewModel do projeto existente
/// - Mantém todas as funcionalidades avançadas
/// - Compatível com sistema de navegação atual
/// </summary>
public partial class FamiliesListSyncfusionPage : ContentPage
{
    private readonly FamiliesListSyncfusionViewModel _viewModel;

    public FamiliesListSyncfusionPage(FamiliesListSyncfusionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        Debug.WriteLine("✅ [FAMILIES_SYNCFUSION_PAGE] Initialized with CORRECTED structure compatibility");
        Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_PAGE] ViewModel: {(_viewModel != null ? "OK" : "NULL")}");
        Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_PAGE] ListView: {(FamilyListView != null ? "OK" : "NULL")}");

        // Hook do evento Refreshing
        ListRefresh.Refreshing += PullToRefresh_Refreshing;

        if (_viewModel?.SelectedItems == null)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] ViewModel.SelectedItems is NULL during initialization!");
        }
    }

    /// <summary>
    /// ✅ Handler do evento Pull-to-Refresh
    /// </summary>
    private async void PullToRefresh_Refreshing(object? sender, EventArgs e)
    {
        try
        {
            Debug.WriteLine("🔄 [FAMILIES_SYNCFUSION_PAGE] Pull-to-refresh triggered");

            ListRefresh.IsRefreshing = true;
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
            if (FamilyListView.SelectedItems == null)
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

        Debug.WriteLine($"🔍 [FAMILIES_SYNCFUSION_PAGE] Final check - ListView.SelectedItems: {(FamilyListView.SelectedItems != null ? "OK" : "NULL")}");
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
            ListRootGrid.Opacity = 0;
            ListRootGrid.Scale = 0.95;
            ListRootGrid.TranslationY = 30;

            FabButton.Opacity = 0;
            FabButton.Scale = 0.8;
            FabButton.TranslationY = 50;

            await Task.WhenAll(
                ListRootGrid.FadeTo(1, 600, Easing.CubicOut),
                ListRootGrid.ScaleTo(1, 600, Easing.SpringOut),
                ListRootGrid.TranslateTo(0, 0, 600, Easing.CubicOut)
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
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Entrance animation error: {ex.Message}");
            ListRootGrid.Opacity = 1;
            FabButton.Opacity = 1;
        }
    }

    private async Task PerformExitAnimation()
    {
        try
        {
            await Task.WhenAll(
                ListRootGrid.FadeTo(0.8, 300, Easing.CubicIn),
                ListRootGrid.ScaleTo(0.98, 300, Easing.CubicIn),
                FabButton.FadeTo(0, 200, Easing.CubicIn),
                FabButton.ScaleTo(0.9, 200, Easing.CubicIn)
            );
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Exit animation error: {ex.Message}");
        }
    }

    #endregion

    #region ✅ FAB HANDLER - DINÂMICO

    private async void OnFabPressed(object sender, EventArgs e)
    {
        try
        {
            await FabButton.ScaleTo(0.9, 100, Easing.CubicIn);
            await FabButton.ScaleTo(1, 100, Easing.CubicOut);

            var selectedCount = _viewModel.SelectedItems.Count;
            Debug.WriteLine($"🎯 [FAMILIES_SYNCFUSION_PAGE] FAB pressed - Selected: {selectedCount}");

            if (selectedCount > 0)
            {
                Debug.WriteLine($"🗑️ [FAMILIES_SYNCFUSION_PAGE] Executing delete for {selectedCount} items");
                await _viewModel.DeleteSelectedCommand.ExecuteAsync(null);
                FamilyListView.SelectedItems?.Clear();
            }
            else if (_viewModel.IsMultiSelectMode)
            {
                Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Canceling multi-select mode");
                _viewModel.ToggleMultiSelectCommand.Execute(null);
            }
            else
            {
                Debug.WriteLine($"➕ [FAMILIES_SYNCFUSION_PAGE] Adding new family");
                await _viewModel.AddNewCommand.ExecuteAsync(null);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] FAB action error: {ex.Message}");
        }
    }

    #endregion

    #region ✅ FILTER AND SORT HANDLERS

    private async void OnStatusFilterTapped(object sender, EventArgs e)
    {
        try
        {
            string[] options = { "All", "Active", "Inactive" };
            string result = await DisplayActionSheet("Filter by Status", "Cancel", null, options);

            if (result != "Cancel" && result != null)
            {
                _viewModel.StatusFilter = result;
                Debug.WriteLine($"🏷️ [FAMILIES_SYNCFUSION_PAGE] Status filter changed to: {result}");
                _viewModel.ApplyFilterCommand.Execute(null);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Filter error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ Handler para o botão de Sort com ActionSheet
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

    #region ✅ SYNCFUSION LISTVIEW EVENT HANDLERS

    private void OnItemTapped(object sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
    {
        try
        {
            if (e.DataItem is FamilyItemViewModel item)
            {
                Debug.WriteLine($"👆 [FAMILIES_SYNCFUSION_PAGE] Item tapped: {item.Name} - MultiSelect: {_viewModel.IsMultiSelectMode}, SelectionMode: {FamilyListView.SelectionMode}");

                if (!_viewModel.IsMultiSelectMode && FamilyListView.SelectionMode == Syncfusion.Maui.ListView.SelectionMode.None)
                {
                    Debug.WriteLine($"🔄 [FAMILIES_SYNCFUSION_PAGE] Navigating to edit: {item.Name}");
                    _viewModel.NavigateToEditCommand.Execute(item);
                }
                else
                {
                    Debug.WriteLine($"🔘 [FAMILIES_SYNCFUSION_PAGE] In multi-select mode - tap will select/deselect via SelectionChanged");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] ItemTapped error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ LongPress para ativar modo multisseleção
    /// </summary>
    private void OnItemLongPress(object sender, Syncfusion.Maui.ListView.ItemLongPressEventArgs e)
    {
        try
        {
            if (e.DataItem is FamilyItemViewModel item)
            {
                Debug.WriteLine($"🔘 [FAMILIES_SYNCFUSION_PAGE] *** LONG PRESS DETECTED ***: {item.Name}");

                FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.Multiple;
                Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_PAGE] ListView.SelectionMode = Multiple");

                _viewModel.IsMultiSelectMode = true;
                Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_PAGE] Multi-select mode activated");

                Device.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await Task.Delay(50);

                        if (FamilyListView.SelectedItems != null && !FamilyListView.SelectedItems.Contains(item))
                        {
                            FamilyListView.SelectedItems.Add(item);
                            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_PAGE] FORCED visual selection in ListView: {item.Name}");
                        }
                    }
                    catch (Exception visualEx)
                    {
                        Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Visual selection error: {visualEx.Message}");

                        try
                        {
                            var index = _viewModel.Items.IndexOf(item);
                            if (index >= 0)
                            {
                                FamilyListView.RefreshItem(index);
                                Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_PAGE] Refreshed item at index: {index}");
                            }
                        }
                        catch (Exception refreshEx)
                        {
                            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Refresh error: {refreshEx.Message}");
                        }
                    }
                });

                if (!_viewModel.SelectedItems.Contains(item))
                {
                    _viewModel.SelectedItems.Add(item);
                    Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_PAGE] Added to ViewModel.SelectedItems: {item.Name}");
                }

                item.IsSelected = true;
                Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_PAGE] Item marked as selected");

                _viewModel.UpdateFabForSelection();
                Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_PAGE] FAB updated");

                Debug.WriteLine($"🎉 [FAMILIES_SYNCFUSION_PAGE] LONG PRESS SUCCESS: {item.Name} - Total selected: {_viewModel.SelectedItems.Count}");
                Debug.WriteLine($"🎯 [FAMILIES_SYNCFUSION_PAGE] FAB Text: {_viewModel.FabText}");
            }
            else
            {
                Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] e.DataItem is not FamilyItemViewModel");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] ItemLongPress error: {ex.Message}");
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// ✅ SelectionChanged nativo do Syncfusion
    /// </summary>
    private void OnSelectionChanged(object sender, Syncfusion.Maui.ListView.ItemSelectionChangedEventArgs e)
    {
        try
        {
            var selectedCount = FamilyListView.SelectedItems?.Count ?? 0;
            Debug.WriteLine($"🔘 [FAMILIES_SYNCFUSION_PAGE] NATIVE Selection changed - ListView count: {selectedCount}");

            if (FamilyListView.SelectedItems != null && _viewModel?.SelectedItems != null)
            {
                _viewModel.SelectedItems.Clear();

                foreach (FamilyItemViewModel item in FamilyListView.SelectedItems)
                {
                    _viewModel.SelectedItems.Add(item);
                    item.IsSelected = true;
                }

                Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_PAGE] Synced to ViewModel: {_viewModel.SelectedItems.Count} items");
            }

            if (_viewModel != null)
            {
                var vmSelectedCount = _viewModel.SelectedItems?.Count ?? 0;

                if (vmSelectedCount == 0 && _viewModel.IsMultiSelectMode)
                {
                    Debug.WriteLine($"🔄 [FAMILIES_SYNCFUSION_PAGE] Auto-exiting multi-select mode and setting SelectionMode = None");
                    _viewModel.IsMultiSelectMode = false;
                    FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.None;
                }
                else if (vmSelectedCount > 0)
                {
                    _viewModel.IsMultiSelectMode = true;
                }

                _viewModel.UpdateFabForSelection();

                Debug.WriteLine($"🎯 [FAMILIES_SYNCFUSION_PAGE] ViewModel updated - MultiSelect: {_viewModel.IsMultiSelectMode}, SelectionMode: {FamilyListView.SelectionMode}, FAB: {_viewModel.FabText}");
            }

            if (e.AddedItems?.Count > 0)
            {
                foreach (FamilyItemViewModel item in e.AddedItems)
                {
                    Debug.WriteLine($"➕ [FAMILIES_SYNCFUSION_PAGE] NATIVE Selected: {item.Name}");
                }
            }

            if (e.RemovedItems?.Count > 0)
            {
                foreach (FamilyItemViewModel item in e.RemovedItems)
                {
                    item.IsSelected = false;
                    Debug.WriteLine($"➖ [FAMILIES_SYNCFUSION_PAGE] NATIVE Deselected: {item.Name}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] SelectionChanged error: {ex.Message}");
        }
    }

    #endregion

    #region ✅ SYNCFUSION SWIPE HANDLERS - OTIMIZADO

    private const double SWIPE_THRESHOLD = 0.8; // ✅ 80% - bom equilíbrio entre usabilidade e precisão

    /// <summary>
    /// ✅ SwipeStarting - detecta direção inicial e pode cancelar
    /// </summary>
    private void OnSwipeStarting(object sender, Syncfusion.Maui.ListView.SwipeStartingEventArgs e)
    {
        try
        {
            if (e.DataItem is FamilyItemViewModel item)
            {
                Debug.WriteLine($"🚀 [SWIPE_STARTING] Item: {item.Name}");
                Debug.WriteLine($"🚀 [SWIPE_STARTING] Direction: {e.Direction}");
                Debug.WriteLine($"🚀 [SWIPE_STARTING] SwipeOffset: {FamilyListView.SwipeOffset}px - threshold: {SWIPE_THRESHOLD:P0}");

                // Cancelar swipe para itens do sistema se for delete
                if (item.IsSystemDefault && e.Direction.ToString() == "Left")
                {
                    e.Cancel = true;
                    Debug.WriteLine($"❌ [SWIPE_STARTING] Cancelled left swipe for system default: {item.Name}");
                    return;
                }

                Debug.WriteLine($"✅ [SWIPE_STARTING] Swipe allowed for {item.Name} - direction: {e.Direction}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [SWIPE_STARTING] Error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ Swiping - monitorar progresso do swipe
    /// </summary>
    private void OnSwiping(object sender, Syncfusion.Maui.ListView.SwipingEventArgs e)
    {
        try
        {
            if (e.DataItem is FamilyItemViewModel item)
            {
                var offsetPercent = Math.Abs(e.Offset) / FamilyListView.SwipeOffset;
                var direction = e.Direction.ToString();
                var icon = direction == "Right" ? "⭐" : "🗑️";

                // Log apenas marcos importantes para não poluir o console
                if (offsetPercent >= 0.8)
                {
                    Debug.WriteLine($"📱 [SWIPING] {icon} {item.Name} | {direction} | {offsetPercent:P0} - READY!");
                }
                else if (offsetPercent >= 0.5)
                {
                    Debug.WriteLine($"📱 [SWIPING] {icon} {item.Name} | {direction} | {offsetPercent:P0}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [SWIPING] Error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ SwipeEnded - executar ação baseada no threshold
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

            Debug.WriteLine($"🏁 [SWIPE_ENDED] === SWIPE ACTION ===");
            Debug.WriteLine($"🏁 [SWIPE_ENDED] {icon} Item: {item.Name}");
            Debug.WriteLine($"🏁 [SWIPE_ENDED] Direction: {direction}");
            Debug.WriteLine($"🏁 [SWIPE_ENDED] Final Progress: {offsetPercent:P1}");

            // ✅ Verificar threshold
            if (offsetPercent < SWIPE_THRESHOLD)
            {
                Debug.WriteLine($"❌ [SWIPE_ENDED] INSUFFICIENT SWIPE - Need {SWIPE_THRESHOLD:P0}+, got {offsetPercent:P1}");
                FamilyListView.ResetSwipeItem();
                return;
            }

            // ✅ AÇÃO APROVADA
            Debug.WriteLine($"🎯 [SWIPE_ENDED] SWIPE APPROVED! Executing {icon} {direction} action");

            switch (direction)
            {
                case "Right":
                    Debug.WriteLine($"⭐ [SWIPE_ENDED] FAVORITE action triggered");

                    if (!_viewModel.IsConnected)
                    {
                        await ShowToast("Cannot favorite while offline", ToastDuration.Short);
                        break;
                    }

                    // Toggle favorite status (simulado - você implementaria a lógica real)
                    var newFavoriteStatus = !item.IsFavorite;
                    var message = newFavoriteStatus ? "Added to favorites!" : "Removed from favorites!";

                    await ShowToast($"⭐ {message}", ToastDuration.Short);

                    Debug.WriteLine($"✅ [SWIPE_ENDED] FAVORITE completed for: {item.Name}");
                    break;

                case "Left":
                    Debug.WriteLine($"🗑️ [SWIPE_ENDED] DELETE action triggered");

                    if (!_viewModel.IsConnected)
                    {
                        await ShowToast("Cannot delete while offline", ToastDuration.Short);
                        break;
                    }

                    if (item.IsSystemDefault)
                    {
                        await ShowToast("Cannot delete system default family", ToastDuration.Short);
                        break;
                    }

                    var confirmed = await DisplayAlert(
                        "🗑️ Delete Family",
                        $"Are you sure you want to delete '{item.Name}'?",
                        "Delete",
                        "Cancel");

                    if (confirmed)
                    {
                        Debug.WriteLine($"🗑️ [SWIPE_ENDED] User confirmed delete: {item.Name}");
                        await _viewModel.DeleteSingleItemCommand.ExecuteAsync(item);
                    }
                    else
                    {
                        Debug.WriteLine($"❌ [SWIPE_ENDED] User cancelled delete: {item.Name}");
                    }
                    break;

                default:
                    Debug.WriteLine($"⚠️ [SWIPE_ENDED] Unknown direction: {direction}");
                    break;
            }

            // ✅ SEMPRE RESETAR após ação
            FamilyListView.ResetSwipeItem();
            Debug.WriteLine($"🔄 [SWIPE_ENDED] Auto-reset completed");

            Debug.WriteLine($"🏁 [SWIPE_ENDED] === END ===");
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

    /// <summary>
    /// ✅ Show toast notification
    /// </summary>
    private async Task ShowToast(string message, ToastDuration duration)
    {
        try
        {
            var toast = Toast.Make(message, duration);
            await toast.Show();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] Toast error: {ex.Message}");
            // Fallback para DisplayAlert se toast falhar
            await DisplayAlert("Info", message, "OK");
        }
    }

    public void ScrollToItem(FamilyItemViewModel item)
    {
        try
        {
            if (item != null && _viewModel.Items.Contains(item))
            {
                int index = _viewModel.Items.IndexOf(item);
                if (index >= 0)
                {
                    FamilyListView.ItemsLayout.ScrollToRowIndex(index, Microsoft.Maui.Controls.ScrollToPosition.Center, true);
                    Debug.WriteLine($"📜 [FAMILIES_SYNCFUSION_PAGE] Scrolled to item by index: {item.Name} (index: {index})");
                }
                else
                {
                    FamilyListView.ScrollTo(item, Microsoft.Maui.Controls.ScrollToPosition.Center, true);
                    Debug.WriteLine($"📜 [FAMILIES_SYNCFUSION_PAGE] Scrolled to item by object: {item.Name}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] ScrollToItem error: {ex.Message}");
        }
    }

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

    public void ClearSelections()
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

            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_PAGE] All selections cleared and SelectionMode = None");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] ClearSelections error: {ex.Message}");
        }
    }

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

    public void ToggleSelectionMode()
    {
        try
        {
            if (FamilyListView.SelectionMode == Syncfusion.Maui.ListView.SelectionMode.None)
            {
                FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.Multiple;
                Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_PAGE] Enabled multi-selection mode");
            }
            else
            {
                FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.None;
                ClearSelections();
                Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_PAGE] Disabled multi-selection mode");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] ToggleSelectionMode error: {ex.Message}");
        }
    }

    #endregion

    #region Public Methods for External Access

    public void ScrollToTop()
    {
        try
        {
            if (_viewModel.Items.Any())
            {
                ScrollToItem(_viewModel.Items.First());
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] ScrollToTop error: {ex.Message}");
        }
    }

    public void ScrollToBottom()
    {
        try
        {
            if (_viewModel.Items.Any())
            {
                ScrollToItem(_viewModel.Items.Last());
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] ScrollToBottom error: {ex.Message}");
        }
    }

    public string GetListStatistics()
    {
        try
        {
            var total = _viewModel.Items.Count;
            var selected = FamilyListView.SelectedItems?.Count ?? 0;
            var filtered = _viewModel.Items.Count; // Sem DataSource, usamos Items direto

            return $"Total: {total}, Filtered: {filtered}, Selected: {selected}";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_PAGE] GetListStatistics error: {ex.Message}");
            return "Error getting statistics";
        }
    }

    #endregion
}