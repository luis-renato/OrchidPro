using OrchidPro.Extensions;
using OrchidPro.ViewModels.Species;
using Syncfusion.Maui.ListView;

namespace OrchidPro.Views.Pages;

/// <summary>
/// Species list page following the exact pattern of FamiliesListPage and GeneraListPage.
/// Provides species management with hierarchical genus relationships and botanical features.
/// </summary>
public partial class SpeciesListPage : ContentPage
{
    private readonly SpeciesListViewModel _viewModel;

    /// <summary>
    /// Initialize species list page with dependency injection
    /// </summary>
    public SpeciesListPage(SpeciesListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        this.LogInfo("SpeciesListPage initialized with ViewModel");
    }

    #region Lifecycle Events

    /// <summary>
    /// Handle page appearing with animations and data loading
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("SpeciesListPage appearing - starting animations and data load");

            // Perform entrance animation
            await RootGrid.PerformStandardEntranceAsync();

            // Initialize ViewModel
            await _viewModel.OnAppearingAsync();

            this.LogSuccess("SpeciesListPage appeared successfully");

        }, "SpeciesListPage OnAppearing");
    }

    /// <summary>
    /// Handle page disappearing with cleanup
    /// </summary>
    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("SpeciesListPage disappearing");
            await _viewModel.OnDisappearingAsync();
        }, "SpeciesListPage OnDisappearing");
    }

    #endregion

    #region Search Events

    /// <summary>
    /// Handle search entry focused event
    /// </summary>
    private void OnSearchFocused(object sender, FocusEventArgs e)
    {
        this.SafeExecute(() =>
        {
            this.LogDebug("Search entry focused");
            // Could add focused state animations here
        }, "OnSearchFocused");
    }

    /// <summary>
    /// Handle search entry unfocused event
    /// </summary>
    private void OnSearchUnfocused(object sender, FocusEventArgs e)
    {
        this.SafeExecute(() =>
        {
            this.LogDebug("Search entry unfocused");
            // Could add unfocused state animations here
        }, "OnSearchUnfocused");
    }

    /// <summary>
    /// Handle search text changed with debounced filtering
    /// </summary>
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        this.SafeExecute(() =>
        {
            this.LogDebug($"Search text changed: {e.NewTextValue}");
            // Automatic filtering happens through ViewModel binding
        }, "OnSearchTextChanged");
    }

    #endregion

    #region Toolbar Events

    /// <summary>
    /// Handle select all toolbar item
    /// </summary>
    private void OnSelectAllTapped(object sender, EventArgs e)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo("Select All tapped");
            _viewModel.SelectAllCommand?.Execute(null);
        }, "OnSelectAllTapped");
    }

    /// <summary>
    /// Handle deselect all toolbar item
    /// </summary>
    private void OnDeselectAllTapped(object sender, EventArgs e)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo("Deselect All tapped");
            _viewModel.DeselectAllCommand?.Execute(null);
        }, "OnDeselectAllTapped");
    }

    #endregion

    #region Action Events

    /// <summary>
    /// Handle filter button tap
    /// </summary>
    private async void OnFilterTapped(object sender, TappedEventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Filter tapped - showing filter options");

            var filterOptions = new[] { "All", "Active", "Inactive", "Favorites", "By Rarity", "Fragrant", "By Genus" };
            var selectedFilter = await DisplayActionSheet("Filter Species", "Cancel", null, filterOptions);

            if (!string.IsNullOrEmpty(selectedFilter) && selectedFilter != "Cancel")
            {
                this.LogInfo($"Filter selected: {selectedFilter}");

                switch (selectedFilter)
                {
                    case "All":
                        _viewModel.StatusFilter = "All";
                        break;
                    case "Active":
                        _viewModel.StatusFilter = "Active";
                        break;
                    case "Inactive":
                        _viewModel.StatusFilter = "Inactive";
                        break;
                    case "Favorites":
                        _viewModel.StatusFilter = "Favorites";
                        break;
                    case "Fragrant":
                        await _viewModel.ShowFragrantSpeciesAsync();
                        break;
                    case "By Rarity":
                        await ShowRarityFilterAsync();
                        break;
                    case "By Genus":
                        await ShowGenusFilterAsync();
                        break;
                }

                // Apply the filter
                _viewModel.ApplyFilterCommand?.Execute(null);
            }

        }, "OnFilterTapped");
    }

    /// <summary>
    /// Show rarity filter options
    /// </summary>
    private async Task ShowRarityFilterAsync()
    {
        var rarityOptions = new[] { "Common", "Uncommon", "Rare", "Very Rare", "Extinct" };
        var selectedRarity = await DisplayActionSheet("Filter by Rarity", "Cancel", null, rarityOptions);

        if (!string.IsNullOrEmpty(selectedRarity) && selectedRarity != "Cancel")
        {
            await _viewModel.FilterByRarityAsync(selectedRarity);
        }
    }

    /// <summary>
    /// Show genus filter options (placeholder - would need genus list)
    /// </summary>
    private async Task ShowGenusFilterAsync()
    {
        await DisplayAlert("Genus Filter", "Genus filtering will be implemented when genus selection is available.", "OK");
    }

    /// <summary>
    /// Handle sort button tap
    /// </summary>
    private async void OnSortTapped(object sender, TappedEventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Sort tapped - showing sort options");

            var sortOptions = new[] { "Name A→Z", "Name Z→A", "Recent", "Oldest", "Favorites", "Rarity", "Scientific Name" };
            var selectedSort = await DisplayActionSheet("Sort Species", "Cancel", null, sortOptions);

            if (!string.IsNullOrEmpty(selectedSort) && selectedSort != "Cancel")
            {
                this.LogInfo($"Sort selected: {selectedSort}");
                _viewModel.SortOrder = selectedSort;
                _viewModel.ApplyFilterCommand?.Execute(null);
            }

        }, "OnSortTapped");
    }

    /// <summary>
    /// Handle FAB click for adding new species
    /// </summary>
    private async void OnFabTapped(object sender, EventArgs e)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("FAB clicked - adding new species");

            // FAB animation
            if (sender is Button button)
            {
                await button.ScaleTo(0.9, 100, Easing.CubicOut);
                await button.ScaleTo(1.0, 100, Easing.CubicOut);
            }

            // Execute add command
            _viewModel.NavigateToAddCommand?.Execute(null);

        }, "OnFabTapped");
    }

    #endregion

    #region ListView Events

    /// <summary>
    /// Handle ListView selection changed
    /// </summary>
    private void OnSelectionChanged(object sender, ItemSelectionChangedEventArgs e)
    {
        this.SafeExecute(() =>
        {
            this.LogDebug($"ListView selection changed - Added: {e.AddedItems?.Count ?? 0}, Removed: {e.RemovedItems?.Count ?? 0}");

            // Handle multi-selection state changes
            if (_viewModel.IsMultiSelectMode)
            {
                foreach (SpeciesItemViewModel item in e.AddedItems ?? new List<object>())
                {
                    item.IsSelected = true;
                }

                foreach (SpeciesItemViewModel item in e.RemovedItems ?? new List<object>())
                {
                    item.IsSelected = false;
                }
            }

        }, "OnSelectionChanged");
    }

    #endregion
}