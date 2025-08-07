using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Species;

/// <summary>
/// MINIMAL Species list ViewModel - following exact pattern of FamiliesListViewModel and GeneraListViewModel.
/// All common functionality moved to BaseListViewModel and pattern classes.
/// Only species-specific customizations implemented here.
/// </summary>
public partial class SpeciesListViewModel : BaseListViewModel<Models.Species, SpeciesItemViewModel>
{
    #region Private Fields

    private readonly ISpeciesRepository _speciesRepository;

    #endregion

    #region Required Base Class Overrides

    public override string EntityName => "Species";
    public override string EntityNamePlural => "Species";
    public override string EditRoute => "speciesedit";

    #endregion

    #region Constructor

    public SpeciesListViewModel(ISpeciesRepository repository, INavigationService navigationService)
        : base(repository, navigationService)
    {
        _speciesRepository = repository;
        this.LogInfo("Initialized - using bases for ALL functionality");
    }

    #endregion

    #region Required Implementation

    protected override SpeciesItemViewModel CreateItemViewModel(Models.Species entity)
    {
        return this.SafeExecute(() =>
        {
            var itemViewModel = new SpeciesItemViewModel(entity);
            this.LogInfo($"Created SpeciesItemViewModel for: {entity.Name}");
            return itemViewModel;
        }, fallbackValue: new SpeciesItemViewModel(entity), operationName: "CreateItemViewModel");
    }

    #endregion

    #region Species-Specific Sort Override

    protected override IOrderedEnumerable<SpeciesItemViewModel> ApplyEntitySpecificSort(IEnumerable<SpeciesItemViewModel> filtered)
    {
        return this.SafeExecute(() =>
        {
            this.LogInfo($"Applying sort order: {SortOrder}");

            // Use BaseSortPatterns with species-specific sorting
            var sorted = BaseSortPatterns.ApplyStandardSort<SpeciesItemViewModel>(
                filtered,
                SortOrder);

            this.LogSuccess($"Sort applied successfully: {SortOrder}");
            return sorted;

        }, fallbackValue: filtered.OrderBy(item => item.Name), operationName: "ApplyEntitySpecificSort");
    }

    #endregion

    #region Delete Operations Using Base Pattern

    public IAsyncRelayCommand<SpeciesItemViewModel> DeleteSingleCommand =>
        new AsyncRelayCommand<SpeciesItemViewModel>(DeleteSingleAsync);

    private async Task DeleteSingleAsync(SpeciesItemViewModel? item)
    {
        if (item == null) return;

        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Deleting species: {item.Name}");

            var confirmTitle = $"Delete {item.Name}?";
            var confirmMessage = "Are you sure you want to delete this species?";

            var confirmed = await ShowConfirmAsync(confirmTitle, confirmMessage);
            if (!confirmed) return;

            // Use base repository delete
            var success = await _speciesRepository.DeleteAsync(item.Id);
            if (success)
            {
                Items.Remove(item);
                UpdateCounters();
                await ShowSuccessToastAsync($"Species '{item.Name}' deleted successfully");
                this.LogSuccess($"Species deleted: {item.Name}");
            }
            else
            {
                await ShowErrorAsync("Delete Failed", "Unable to delete the species. Please try again.");
                this.LogError($"Failed to delete species: {item.Name}");
            }

        }, $"Delete Species: {item.Name}");
    }

    #endregion

    #region Species-Specific Features

    /// <summary>
    /// Get species by genus for hierarchical filtering (if needed in future)
    /// </summary>
    public async Task FilterByGenusAsync(Guid genusId)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Filtering species by genus: {genusId}");

            var speciesByGenus = await _speciesRepository.GetByGenusAsync(genusId);
            var filteredItems = speciesByGenus.Select(CreateItemViewModel).ToList();

            Items.Clear();
            foreach (var item in filteredItems)
            {
                Items.Add(item);
            }

            UpdateCounters();
            this.LogSuccess($"Filtered to {Items.Count} species for genus {genusId}");

        }, "Filter Species by Genus");
    }

    /// <summary>
    /// Search species by scientific name (species-specific feature)
    /// </summary>
    public async Task SearchByScientificNameAsync(string scientificName)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Searching species by scientific name: {scientificName}");

            var matchingSpecies = await _speciesRepository.GetByScientificNameAsync(scientificName, exactMatch: false);
            var searchResults = matchingSpecies.Select(CreateItemViewModel).ToList();

            Items.Clear();
            foreach (var item in searchResults)
            {
                Items.Add(item);
            }

            UpdateCounters();
            this.LogSuccess($"Found {Items.Count} species matching scientific name: {scientificName}");

        }, "Search by Scientific Name");
    }

    /// <summary>
    /// Filter by rarity status (species-specific feature)
    /// </summary>
    public async Task FilterByRarityAsync(string rarityStatus)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Filtering species by rarity: {rarityStatus}");

            var rareSpecies = await _speciesRepository.GetByRarityStatusAsync(rarityStatus);
            var rarityItems = rareSpecies.Select(CreateItemViewModel).ToList();

            Items.Clear();
            foreach (var item in rarityItems)
            {
                Items.Add(item);
            }

            UpdateCounters();
            this.LogSuccess($"Filtered to {Items.Count} species with rarity: {rarityStatus}");

        }, "Filter by Rarity");
    }

    /// <summary>
    /// Show only fragrant species (species-specific feature)
    /// </summary>
    public async Task ShowFragrantSpeciesAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Filtering to show only fragrant species");

            var fragrantSpecies = await _speciesRepository.GetFragrantSpeciesAsync();
            var fragrantItems = fragrantSpecies.Select(CreateItemViewModel).ToList();

            Items.Clear();
            foreach (var item in fragrantItems)
            {
                Items.Add(item);
            }

            UpdateCounters();
            this.LogSuccess($"Showing {Items.Count} fragrant species");

        }, "Show Fragrant Species");
    }

    #endregion
}