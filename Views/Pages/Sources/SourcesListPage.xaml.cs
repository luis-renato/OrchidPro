using OrchidPro.Extensions;
using OrchidPro.ViewModels.Sources;
using OrchidPro.Views.Base;

namespace OrchidPro.Views.Pages.Sources;

/// <summary>
/// SourcesListPage - NOMES CORRETOS dos controles XAML
/// </summary>
public partial class SourcesListPage : ContentPage
{
    private readonly BaseListPageLogic<Models.Source, SourceItemViewModel> _base;

    public SourcesListPage(SourcesListViewModel viewModel)
    {
        InitializeComponent();
        _base = new BaseListPageLogic<Models.Source, SourceItemViewModel>(viewModel);

        // CORRECTED: Usar nomes REAIS dos controles do XAML que criei
        _base.SetupPage(this, SourceListView, RootGrid, FabButton, ListRefresh);
        //                   ^^^^^^^^^^^^^  ^^^^^^^^  ^^^^^^^^^  ^^^^^^^^^^^
        //                   XAML real     XAML real XAML real  XAML real

        this.LogInfo("🚀 REFACTORED SourcesListPage - using BaseListPageLogic composition");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _base.BaseOnAppearing();
    }

    protected override void OnDisappearing()
    {
        _base.BaseOnDisappearing();
        base.OnDisappearing();
    }

    // Event Handlers - All Delegated to Base (seguindo padrão FamiliesListPage)
    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e) => _base.HandleSearchTextChanged(sender, e);
    private void OnItemTapped(object? sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e) => _base.HandleItemTapped(sender, e);
    private void OnItemLongPress(object? sender, Syncfusion.Maui.ListView.ItemLongPressEventArgs e) => _base.HandleItemLongPress(sender, e);
    private void OnSelectionChanged(object? sender, Syncfusion.Maui.ListView.ItemSelectionChangedEventArgs e) => _base.HandleSelectionChanged(sender, e);
    private void OnSwipeEnded(object? sender, Syncfusion.Maui.ListView.SwipeEndedEventArgs e) => _base.HandleSwipeEnded(sender, e);
    private void OnSwipeStarting(object? sender, Syncfusion.Maui.ListView.SwipeStartingEventArgs e) => _base.HandleSwipeStarting(sender, e);
    private void OnSwiping(object? sender, Syncfusion.Maui.ListView.SwipingEventArgs e) => _base.HandleSwiping(sender, e);
    private void OnFabTapped(object? sender, EventArgs e) => _base.HandleFabTapped(sender, e);
    private void OnSelectAllTapped(object? sender, EventArgs e) => _base.HandleSelectAllTapped(sender, e);
    private void OnDeselectAllTapped(object? sender, EventArgs e) => _base.HandleDeselectAllTapped(sender, e);
    private void OnFilterTapped(object? sender, EventArgs e) => _base.HandleFilterTapped(sender, e);
    private void OnSortTapped(object? sender, EventArgs e) => _base.HandleSortTapped(sender, e);
    private void OnSearchFocused(object? sender, FocusEventArgs e) => _base.HandleSearchFocused(sender, e);
    private void OnSearchUnfocused(object? sender, FocusEventArgs e) => _base.HandleSearchUnfocused(sender, e);
}
