using OrchidPro.Extensions;
using OrchidPro.ViewModels.Species;
using OrchidPro.Views.Pages.Base;

namespace OrchidPro.Views.Pages;

/// <summary>
/// Species List Page - ULTRA CLEAN using BaseListPageLogic composition.
/// Now even cleaner with migrated handlers from base logic.
/// </summary>
public partial class SpeciesListPage : ContentPage
{
    private readonly BaseListPageLogic<Models.Species, SpeciesItemViewModel> _base;

    public SpeciesListPage(SpeciesListViewModel viewModel)
    {
        InitializeComponent();

        // TRUE composition - use the base logic directly
        _base = new BaseListPageLogic<Models.Species, SpeciesItemViewModel>(viewModel);
        _base.SetupPage(this, SpeciesListView, RootGrid, FabButton, ListRefresh);

        this.LogInfo("Species List Page initialized with base logic");
    }

    #region Lifecycle Events

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

    #endregion

    #region Event Handlers - All Delegated to Base

    // Core interaction handlers
    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e) => _base.HandleSearchTextChanged(sender, e);
    private void OnItemTapped(object? sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e) => _base.HandleItemTapped(sender, e);
    private void OnItemLongPress(object? sender, Syncfusion.Maui.ListView.ItemLongPressEventArgs e) => _base.HandleItemLongPress(sender, e);
    private void OnSelectionChanged(object? sender, Syncfusion.Maui.ListView.ItemSelectionChangedEventArgs e) => _base.HandleSelectionChanged(sender, e);

    // Swipe handlers
    private void OnSwipeEnded(object? sender, Syncfusion.Maui.ListView.SwipeEndedEventArgs e) => _base.HandleSwipeEnded(sender, e);
    private void OnSwipeStarting(object? sender, Syncfusion.Maui.ListView.SwipeStartingEventArgs e) => _base.HandleSwipeStarting(sender, e);
    private void OnSwiping(object? sender, Syncfusion.Maui.ListView.SwipingEventArgs e) => _base.HandleSwiping(sender, e);

    // Button handlers
    private void OnFabTapped(object? sender, EventArgs e) => _base.HandleFabTapped(sender, e);
    private void OnSelectAllTapped(object? sender, EventArgs e) => _base.HandleSelectAllTapped(sender, e);
    private void OnDeselectAllTapped(object? sender, EventArgs e) => _base.HandleDeselectAllTapped(sender, e);
    private void OnFilterTapped(object? sender, EventArgs e) => _base.HandleFilterTapped(sender, e);
    private void OnSortTapped(object? sender, EventArgs e) => _base.HandleSortTapped(sender, e);

    // Focus handlers - migrated to base as empty handlers
    private void OnSearchFocused(object? sender, FocusEventArgs e) => _base.HandleSearchFocused(sender, e);
    private void OnSearchUnfocused(object? sender, FocusEventArgs e) => _base.HandleSearchUnfocused(sender, e);

    #endregion
}