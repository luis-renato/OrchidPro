using OrchidPro.ViewModels.Families;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace OrchidPro.Views.Pages;

/// <summary>
/// ✅ CORRIGIDO: FamiliesListPage com todos os erros de compilação corrigidos
/// </summary>
public partial class FamiliesListPage : ContentPage
{
    private readonly FamiliesListSyncfusionViewModel _viewModel;

    public FamiliesListPage(FamiliesListSyncfusionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        Debug.WriteLine("✅ [FAMILIES_LIST_PAGE] Initialized with NATIVE SYNCFUSION selection");

        // Hook do evento Refreshing
        ListRefresh.Refreshing += PullToRefresh_Refreshing;

        if (_viewModel?.SelectedItems == null)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] ViewModel.SelectedItems is NULL during initialization!");
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

        // ✅ REFRESH SEMPRE QUE VOLTAR PARA A TELA
        Debug.WriteLine($"🔄 [FAMILIES_LIST_PAGE] Refreshing data on page appearing");
        await _viewModel.OnAppearingAsync();

        if (_viewModel.RefreshCommand?.CanExecute(null) == true)
        {
            await _viewModel.RefreshCommand.ExecuteAsync(null);
        }

        // ✅ GARANTIR QUE FAB SEJA ATUALIZADO APÓS CARREGAMENTO
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
                // ✅ SEMPRE GARANTIR QUE FAB ESTEJA VISÍVEL
                FabButton.IsVisible = true;

                if (selectedCount > 0)
                {
                    // ✅ CORREÇÃO: Modo Delete - usar ErrorColor do ResourceDictionary
                    var errorColor = Application.Current?.Resources.TryGetValue("ErrorColor", out var error) == true
                        ? (Color)error
                        : Color.FromArgb("#D32F2F"); // Fallback

                    FabButton.BackgroundColor = errorColor;
                    FabButton.Text = $"Delete ({selectedCount})";
                    Debug.WriteLine($"🔴 [FAB_VISUAL] Set to DELETE mode: Delete ({selectedCount})");
                }
                else if (_viewModel?.IsMultiSelectMode == true)
                {
                    // ✅ CORREÇÃO: Modo Cancel - usar Gray500 do ResourceDictionary
                    var grayColor = Application.Current?.Resources.TryGetValue("Gray500", out var gray) == true
                        ? (Color)gray
                        : Color.FromArgb("#757575"); // Fallback

                    FabButton.BackgroundColor = grayColor;
                    FabButton.Text = "Cancel";
                    Debug.WriteLine($"⚫ [FAB_VISUAL] Set to CANCEL mode");
                }
                else
                {
                    // ✅ CORREÇÃO: Modo Add - usar Primary do ResourceDictionary
                    var primaryColor = Application.Current?.Resources.TryGetValue("Primary", out var primary) == true
                        ? (Color)primary
                        : Color.FromArgb("#A47764"); // Fallback

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

            // ✅ CORREÇÃO: Fallback usando Primary do ResourceDictionary
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

    #region ✅ TOOLBAR ITEMS HANDLERS

    /// <summary>
    /// ✅ Select All toolbar item handler
    /// </summary>
    private void OnSelectAllTapped(object sender, EventArgs e)
    {
        try
        {
            Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] Select All toolbar tapped");

            foreach (var item in _viewModel.Items)
            {
                if (!item.IsSelected)
                {
                    item.IsSelected = true;
                    if (!_viewModel.SelectedItems.Contains(item))
                    {
                        _viewModel.SelectedItems.Add(item);
                    }
                }
            }

            // Force ListView visual update
            Device.BeginInvokeOnMainThread(() =>
            {
                FamilyListView.SelectedItems?.Clear();
                foreach (var item in _viewModel.Items)
                {
                    FamilyListView.SelectedItems?.Add(item);
                }

                // Force refresh all items visually
                for (int i = 0; i < _viewModel.Items.Count; i++)
                {
                    FamilyListView.RefreshItem(i);
                }
            });

            _viewModel.UpdateFabForSelection();
            UpdateFabVisual();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Select All toolbar error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ CORRIGIDO: Clear All - verificar se comando existe antes de usar
    /// </summary>
    private async void OnDeselectAllTapped(object sender, EventArgs e)
    {
        try
        {
            Debug.WriteLine($"🧹 [FAMILIES_LIST_PAGE] Clear All toolbar tapped - will clear selections AND filters");

            // ✅ CORREÇÃO: Verificar se comando existe antes de usar
            if (_viewModel?.ClearSelectionCommand != null && _viewModel.ClearSelectionCommand.CanExecute(null))
            {
                _viewModel.ClearSelectionCommand.Execute(null);
                Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] ClearSelectionCommand executed - filters and selection cleared");
            }
            else
            {
                // ✅ FALLBACK: Limpar manualmente se comando não estiver disponível
                Debug.WriteLine($"⚠️ [FAMILIES_LIST_PAGE] ClearSelectionCommand not available - doing manual clear");

                // Limpar seleções
                if (_viewModel?.Items != null)
                {
                    foreach (var item in _viewModel.Items)
                    {
                        item.IsSelected = false;
                    }
                }

                if (_viewModel?.SelectedItems != null)
                {
                    _viewModel.SelectedItems.Clear();
                }

                FamilyListView.SelectedItems?.Clear();

                // Limpar filtros
                if (_viewModel != null)
                {
                    _viewModel.SearchText = string.Empty;
                    _viewModel.StatusFilter = "All";
                    _viewModel.SortOrder = "Name A→Z";
                    _viewModel.IsMultiSelectMode = false;
                }

                FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.None;
            }

            // ✅ FORÇAR REFRESH VISUAL
            Device.BeginInvokeOnMainThread(() =>
            {
                if (_viewModel?.Items != null)
                {
                    for (int i = 0; i < _viewModel.Items.Count; i++)
                    {
                        FamilyListView.RefreshItem(i);
                    }
                }
            });

            _viewModel?.UpdateFabForSelection();
            UpdateFabVisual();

            // ✅ TOAST DE FEEDBACK - CORRIGIDO
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

    /// <summary>
    /// ✅ Search focus with animation
    /// </summary>
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

    /// <summary>
    /// ✅ Search unfocus with animation
    /// </summary>
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

    /// <summary>
    /// ✅ Search text changed handler
    /// </summary>
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            Debug.WriteLine($"🔍 [FAMILIES_LIST_PAGE] Search text changed to: '{e.NewTextValue}'");
            // O binding automático já chama o ApplyFilter via OnSearchTextChanged no ViewModel
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Search text changed error: {ex.Message}");
        }
    }

    #endregion

    #region ✅ FAB HANDLER - SEM DUPLA CONFIRMAÇÃO

    /// <summary>
    /// ✅ CORRIGIDO: FAB handler com toast de sucesso garantido
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
                    // ✅ USAR COMANDO EXISTENTE
                    if (_viewModel?.DeleteSelectedCommand?.CanExecute(null) == true)
                    {
                        await _viewModel.DeleteSelectedCommand.ExecuteAsync(null);
                    }

                    // ✅ LIMPAR SELEÇÕES
                    _viewModel?.SelectedItems.Clear();
                    FamilyListView.SelectedItems?.Clear();
                    if (_viewModel != null)
                    {
                        _viewModel.IsMultiSelectMode = false;
                    }
                    FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.None;

                    UpdateFabVisual();

                    // ✅ GARANTIR TOAST DE SUCESSO - CORRIGIDO
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
                FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.None;
                FamilyListView.SelectedItems?.Clear();
                _viewModel?.SelectedItems?.Clear();

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
                    // ✅ FALLBACK - navegação manual se command falhar
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

    /// <summary>
    /// ✅ Método local para toggle de multiseleção
    /// </summary>
    private void ToggleMultiSelect()
    {
        try
        {
            if (_viewModel?.ToggleMultiSelectCommand?.CanExecute(null) == true)
            {
                _viewModel.ToggleMultiSelectCommand.Execute(null);
            }
            else
            {
                // Fallback manual
                if (_viewModel != null)
                {
                    _viewModel.IsMultiSelectMode = !_viewModel.IsMultiSelectMode;
                    _viewModel.UpdateFabForSelection();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] ToggleMultiSelect error: {ex.Message}");
        }
    }
    #endregion

    #region ✅ FILTER AND SORT HANDLERS - ATUALIZADO

    /// <summary>
    /// ✅ Filter handler with icon button animation
    /// </summary>
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

                // Show feedback toast - CORRIGIDO
                var toast = Toast.Make($"Filter: {result}", CommunityToolkit.Maui.Core.ToastDuration.Short, 14);
                await toast.Show();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Filter error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ Sort handler with icon button animation
    /// </summary>
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

                // Show feedback toast - CORRIGIDO
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

    #region ✅ SYNCFUSION LISTVIEW EVENT HANDLERS

    private void OnItemTapped(object sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
    {
        try
        {
            if (e.DataItem is FamilyItemViewModel item)
            {
                Debug.WriteLine($"👆 [FAMILIES_LIST_PAGE] Item tapped: {item.Name} - MultiSelect: {_viewModel?.IsMultiSelectMode}");

                if (_viewModel?.IsMultiSelectMode != true)
                {
                    Debug.WriteLine($"🔄 [FAMILIES_LIST_PAGE] Navigating to edit: {item.Name}");
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        if (_viewModel?.NavigateToEditCommand?.CanExecute(item) == true)
                        {
                            await _viewModel.NavigateToEditCommand.ExecuteAsync(item);
                        }
                    });
                }
                else
                {
                    Debug.WriteLine($"🔘 [FAMILIES_LIST_PAGE] Multi-select mode - toggling selection");
                    // Toggle selection manualmente
                    if (item.IsSelected)
                    {
                        item.IsSelected = false;
                        _viewModel?.SelectedItems?.Remove(item);
                        FamilyListView.SelectedItems?.Remove(item);
                    }
                    else
                    {
                        item.IsSelected = true;
                        if (_viewModel?.SelectedItems != null && !_viewModel.SelectedItems.Contains(item))
                            _viewModel.SelectedItems.Add(item);
                        if (FamilyListView.SelectedItems != null && !FamilyListView.SelectedItems.Contains(item))
                            FamilyListView.SelectedItems.Add(item);
                    }

                    _viewModel?.UpdateFabForSelection();

                    // ✅ FORÇAR REFRESH VISUAL DO FAB
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        UpdateFabVisual();
                    });

                    // Se não há mais itens selecionados, sair do modo multisseleção
                    if (_viewModel?.SelectedItems?.Count == 0)
                    {
                        if (_viewModel != null)
                        {
                            _viewModel.IsMultiSelectMode = false;
                        }
                        FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.None;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] ItemTapped error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ LongPress handler
    /// </summary>
    private void OnItemLongPress(object sender, Syncfusion.Maui.ListView.ItemLongPressEventArgs e)
    {
        try
        {
            if (e.DataItem is FamilyItemViewModel item)
            {
                Debug.WriteLine($"🔘 [FAMILIES_LIST_PAGE] Long press: {item.Name}");

                // Ativar modo multisseleção
                if (_viewModel?.IsMultiSelectMode != true)
                {
                    if (_viewModel != null)
                    {
                        _viewModel.IsMultiSelectMode = true;
                    }
                    FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.Multiple;
                    Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] Multi-select mode ACTIVATED");
                }

                // Selecionar o item
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

                _viewModel?.UpdateFabForSelection();
                UpdateFabVisual();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] ItemLongPress error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ SelectionChanged nativo do Syncfusion - CORRIGIDO para atualizar FAB
    /// </summary>
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
                    Debug.WriteLine($"🔄 [FAMILIES_LIST_PAGE] Auto-exiting multi-select mode and setting SelectionMode = None");
                    _viewModel.IsMultiSelectMode = false;
                    FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.None;
                }
                else if (vmSelectedCount > 0)
                {
                    _viewModel.IsMultiSelectMode = true;
                }

                // ✅ FORÇAR ATUALIZAÇÃO DO FAB
                _viewModel.UpdateFabForSelection();

                // ✅ FORÇAR REFRESH DA UI DO FAB COM BACKGROUND COLOR
                Device.BeginInvokeOnMainThread(() =>
                {
                    UpdateFabVisual();
                });

                Debug.WriteLine($"🎯 [FAMILIES_LIST_PAGE] ViewModel updated - MultiSelect: {_viewModel.IsMultiSelectMode}, SelectionMode: {FamilyListView.SelectionMode}, FAB: {_viewModel.FabText}");
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
    /// ✅ SwipeEnded - CORRIGIDO: executar ação com comando do ViewModel
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

                    if (_viewModel?.IsConnected != true)
                    {
                        await ShowToast("Cannot favorite while offline", CommunityToolkit.Maui.Core.ToastDuration.Short);
                        break;
                    }

                    try
                    {
                        var wasAlreadyFavorite = item.IsFavorite;

                        Debug.WriteLine($"⭐ [SWIPE_ENDED] Calling ToggleFavoriteAsync for: {item.Name} (Currently: {wasAlreadyFavorite})");

                        if (_viewModel?.ToggleFavoriteCommand?.CanExecute(item) == true)
                        {
                            await _viewModel.ToggleFavoriteCommand.ExecuteAsync(item);
                        }

                        Debug.WriteLine($"✅ [SWIPE_ENDED] ToggleFavorite completed for: {item.Name}");

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

                    if (item.IsSystemDefault)
                    {
                        await ShowToast("Cannot delete system default families", CommunityToolkit.Maui.Core.ToastDuration.Short);
                        break;
                    }

                    try
                    {
                        Debug.WriteLine($"🗑️ [SWIPE_ENDED] Calling DeleteSingleItemAsync for: {item.Name}");

                        if (_viewModel?.DeleteSingleItemCommand?.CanExecute(item) == true)
                        {
                            await _viewModel.DeleteSingleItemCommand.ExecuteAsync(item);
                        }

                        Debug.WriteLine($"✅ [SWIPE_ENDED] Delete completed for: {item.Name}");

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
    /// ✅ Show toast notification - CORRIGIDO
    /// </summary>
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
            // Fallback para DisplayAlert se toast falhar
            await DisplayAlert("Info", message, "OK");
        }
    }

    public void ScrollToItem(FamilyItemViewModel item)
    {
        try
        {
            if (item != null && _viewModel?.Items?.Contains(item) == true)
            {
                int index = _viewModel.Items.IndexOf(item);
                if (index >= 0)
                {
                    FamilyListView.ItemsLayout.ScrollToRowIndex(index, Microsoft.Maui.Controls.ScrollToPosition.Center, true);
                    Debug.WriteLine($"📜 [FAMILIES_LIST_PAGE] Scrolled to item by index: {item.Name} (index: {index})");
                }
                else
                {
                    FamilyListView.ScrollTo(item, Microsoft.Maui.Controls.ScrollToPosition.Center, true);
                    Debug.WriteLine($"📜 [FAMILIES_LIST_PAGE] Scrolled to item by object: {item.Name}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] ScrollToItem error: {ex.Message}");
        }
    }

    public void RefreshListView()
    {
        try
        {
            ListRefresh.IsRefreshing = true;
            Debug.WriteLine($"🔄 [FAMILIES_LIST_PAGE] ListView refresh triggered via SfPullToRefresh");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] RefreshListView error: {ex.Message}");
        }
    }

    public void ClearSelections()
    {
        try
        {
            FamilyListView.SelectedItems?.Clear();
            _viewModel?.SelectedItems?.Clear();

            if (_viewModel?.Items != null)
            {
                foreach (var item in _viewModel.Items)
                {
                    item.IsSelected = false;
                }
            }

            if (_viewModel != null)
            {
                _viewModel.IsMultiSelectMode = false;
            }
            FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.None;
            _viewModel?.UpdateFabForSelection();

            Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] All selections cleared and SelectionMode = None");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] ClearSelections error: {ex.Message}");
        }
    }

    public void SelectAllItems()
    {
        try
        {
            FamilyListView.SelectAll();
            Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] Selected all items via Syncfusion native method");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] SelectAllItems error: {ex.Message}");
        }
    }

    public void ToggleSelectionMode()
    {
        try
        {
            if (FamilyListView.SelectionMode == Syncfusion.Maui.ListView.SelectionMode.None)
            {
                FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.Multiple;
                Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] Enabled multi-selection mode");
            }
            else
            {
                FamilyListView.SelectionMode = Syncfusion.Maui.ListView.SelectionMode.None;
                ClearSelections();
                Debug.WriteLine($"✅ [FAMILIES_LIST_PAGE] Disabled multi-selection mode");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] ToggleSelectionMode error: {ex.Message}");
        }
    }

    #endregion

    #region Public Methods for External Access

    public void ScrollToTop()
    {
        try
        {
            if (_viewModel?.Items?.Any() == true)
            {
                ScrollToItem(_viewModel.Items.First());
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] ScrollToTop error: {ex.Message}");
        }
    }

    public void ScrollToBottom()
    {
        try
        {
            if (_viewModel?.Items?.Any() == true)
            {
                ScrollToItem(_viewModel.Items.Last());
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] ScrollToBottom error: {ex.Message}");
        }
    }

    public string GetListStatistics()
    {
        try
        {
            var total = _viewModel?.Items?.Count ?? 0;
            var selected = FamilyListView.SelectedItems?.Count ?? 0;
            var filtered = _viewModel?.Items?.Count ?? 0; // Sem DataSource, usamos Items direto

            return $"Total: {total}, Filtered: {filtered}, Selected: {selected}";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] GetListStatistics error: {ex.Message}");
            return "Error getting statistics";
        }
    }

    #endregion
}