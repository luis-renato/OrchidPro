using OrchidPro.Extensions;
using OrchidPro.ViewModels.Botanical.Families;
using OrchidPro.Views.Base;

namespace OrchidPro.Views.Pages.Botanical;

/// <summary>
/// Family List Page - ULTRA CLEAN using BaseListPageLogic composition.
/// Reduced from 400+ lines to ~30 lines using the base list page logic.
/// Follows exact same pattern as SpeciesListPage with BaseListPageLogic.
/// </summary>
public partial class FamiliesListPage : ContentPage
{
    private readonly BaseListPageLogic<Models.Family, FamilyItemViewModel> _base;

    /// <summary>
    /// Initialize the families list page with dependency injection and composition
    /// </summary>
    public FamiliesListPage(FamiliesListViewModel viewModel)
    {
        InitializeComponent();

        // TRUE composition - use the base logic directly
        _base = new BaseListPageLogic<Models.Family, FamilyItemViewModel>(viewModel);
        _base.SetupPage(this, FamilyListView, RootGrid, FabButton, ListRefresh);

        this.LogInfo("🚀 REFACTORED FamiliesListPage - using BaseListPageLogic composition");
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