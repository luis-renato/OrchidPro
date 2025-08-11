using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Species;

/// <summary>
/// OPTIMIZED Species list ViewModel with performance improvements.
/// Reduced logging, efficient item creation, cached operations.
/// Following exact pattern of working FamiliesListViewModel and GeneraListViewModel.
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
        this.LogInfo("Initialized list ViewModel for Species");
    }

    #endregion

    #region Required Implementation (OPTIMIZED)

    /// <summary>
    /// OPTIMIZED CreateItemViewModel - reduced logging, direct creation
    /// </summary>
    protected override SpeciesItemViewModel CreateItemViewModel(Models.Species entity)
    {
        // REMOVED SafeExecute wrapper for performance - this is called frequently
        // REMOVED excessive logging that was happening for every item
        return new SpeciesItemViewModel(entity);
    }

    #endregion

    #region Species-Specific Sort Override (OPTIMIZED)

    /// <summary>
    /// OPTIMIZED sorting with minimal logging
    /// </summary>
    protected override IOrderedEnumerable<SpeciesItemViewModel> ApplyEntitySpecificSort(IEnumerable<SpeciesItemViewModel> filtered)
    {
        // REMOVED excessive logging and SafeExecute for performance
        return BaseSortPatterns.ApplyStandardSort<SpeciesItemViewModel>(filtered, SortOrder);
    }

    #endregion

    #region Delete Operations Using Base Pattern (OPTIMIZED)

    public IAsyncRelayCommand<SpeciesItemViewModel> DeleteSingleCommand =>
        new AsyncRelayCommand<SpeciesItemViewModel>(DeleteSingleAsync);

    private async Task DeleteSingleAsync(SpeciesItemViewModel? item)
    {
        if (item == null) return;

        await this.SafeExecuteAsync(async () =>
        {
            var confirmTitle = $"Delete {item.Name}?";
            var confirmMessage = "Are you sure you want to delete this species?";

            var confirmed = await ShowConfirmAsync(confirmTitle, confirmMessage);
            if (!confirmed) return;

            var success = await _speciesRepository.DeleteAsync(item.Id);
            if (success)
            {
                Items.Remove(item);
                UpdateCounters();
                await ShowSuccessToastAsync($"Species '{item.Name}' deleted successfully");
            }
            else
            {
                await ShowErrorAsync("Delete Failed", "Unable to delete the species. Please try again.");
            }

        }, $"Delete Species: {item.Name}");
    }

    #endregion

    #region Species-Specific Features (OPTIMIZED)

    /// <summary>
    /// OPTIMIZED genus filtering with minimal logging
    /// </summary>
    public async Task FilterByGenusAsync(Guid genusId)
    {
        await this.SafeExecuteAsync(async () =>
        {
            var speciesByGenus = await _speciesRepository.GetByGenusAsync(genusId);
            var filteredItems = speciesByGenus.Select(CreateItemViewModel).ToList();

            Items.Clear();
            foreach (var item in filteredItems)
            {
                Items.Add(item);
            }

            UpdateCounters();

        }, "Filter Species by Genus");
    }

    /// <summary>
    /// OPTIMIZED scientific name search
    /// </summary>
    public async Task SearchByScientificNameAsync(string scientificName)
    {
        await this.SafeExecuteAsync(async () =>
        {
            var matchingSpecies = await _speciesRepository.GetByScientificNameAsync(scientificName, exactMatch: false);
            var searchResults = matchingSpecies.Select(CreateItemViewModel).ToList();

            Items.Clear();
            foreach (var item in searchResults)
            {
                Items.Add(item);
            }

            UpdateCounters();

        }, "Search by Scientific Name");
    }

    /// <summary>
    /// OPTIMIZED rarity filtering
    /// </summary>
    public async Task FilterByRarityAsync(string rarityStatus)
    {
        await this.SafeExecuteAsync(async () =>
        {
            var rareSpecies = await _speciesRepository.GetByRarityStatusAsync(rarityStatus);
            var rarityItems = rareSpecies.Select(CreateItemViewModel).ToList();

            Items.Clear();
            foreach (var item in rarityItems)
            {
                Items.Add(item);
            }

            UpdateCounters();

        }, "Filter by Rarity");
    }

    /// <summary>
    /// OPTIMIZED fragrant species filter
    /// </summary>
    public async Task ShowFragrantSpeciesAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            var fragrantSpecies = await _speciesRepository.GetFragrantSpeciesAsync();
            var fragrantItems = fragrantSpecies.Select(CreateItemViewModel).ToList();

            Items.Clear();
            foreach (var item in fragrantItems)
            {
                Items.Add(item);
            }

            UpdateCounters();

        }, "Show Fragrant Species");
    }

    #endregion
}