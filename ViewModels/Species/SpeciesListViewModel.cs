using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Species;

/// <summary>
/// 🚀 THREAD-OPTIMIZED Species list ViewModel.
/// FIXED: Removed excessive Task.Run() calls that were creating 14+ threads.
/// Now matches Families performance with efficient threading.
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
        this.LogInfo("🚀 ULTRA-OPTIMIZED SpeciesListViewModel initialized - inheriting all BaseListViewModel optimizations");
    }

    #endregion

    #region REQUIRED ONLY: CreateItemViewModel

    /// <summary>
    /// Only required override - creates SpeciesItemViewModel instances
    /// </summary>
    protected override SpeciesItemViewModel CreateItemViewModel(Models.Species entity)
    {
        return new SpeciesItemViewModel(entity);
    }

    #endregion

    #region UI COMPATIBILITY: Expose Base Commands

    /// <summary>
    /// Expose base DeleteSingleItemCommand as DeleteSingleCommand for UI compatibility
    /// </summary>
    public IAsyncRelayCommand<SpeciesItemViewModel> DeleteSingleCommand => DeleteSingleItemCommand;

    /// <summary>
    /// Expose base DeleteSelectedCommand for UI compatibility
    /// </summary>
    public new IAsyncRelayCommand DeleteSelectedCommand => base.DeleteSelectedCommand;

    #endregion

    #region SPECIES-SPECIFIC FEATURES - THREAD OPTIMIZED

    /// <summary>
    /// 🚀 THREAD-OPTIMIZED genus filtering - removed excessive Task.Run
    /// </summary>
    public async Task FilterByGenusAsync(Guid genusId)
    {
        await this.SafeExecuteAsync(async () =>
        {
            var speciesByGenus = await _speciesRepository.GetByGenusAsync(genusId);

            // FIXED: Use BaseListViewModel's efficient item creation instead of Task.Run
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Items.Clear();
                foreach (var species in speciesByGenus)
                {
                    Items.Add(CreateItemViewModel(species));
                }
                UpdateCounters();
            });

        }, "Filter Species by Genus");
    }

    /// <summary>
    /// 🚀 THREAD-OPTIMIZED scientific name search - removed excessive Task.Run
    /// </summary>
    public async Task SearchByScientificNameAsync(string scientificName)
    {
        await this.SafeExecuteAsync(async () =>
        {
            var matchingSpecies = await _speciesRepository.GetByScientificNameAsync(scientificName, exactMatch: false);

            // FIXED: Use BaseListViewModel's efficient item creation instead of Task.Run
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Items.Clear();
                foreach (var species in matchingSpecies)
                {
                    Items.Add(CreateItemViewModel(species));
                }
                UpdateCounters();
            });

        }, "Search by Scientific Name");
    }

    /// <summary>
    /// 🚀 THREAD-OPTIMIZED rarity filtering - removed excessive Task.Run
    /// </summary>
    public async Task FilterByRarityAsync(string rarityStatus)
    {
        await this.SafeExecuteAsync(async () =>
        {
            var rareSpecies = await _speciesRepository.GetByRarityStatusAsync(rarityStatus);

            // FIXED: Use BaseListViewModel's efficient item creation instead of Task.Run
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Items.Clear();
                foreach (var species in rareSpecies)
                {
                    Items.Add(CreateItemViewModel(species));
                }
                UpdateCounters();
            });

        }, "Filter by Rarity");
    }

    /// <summary>
    /// 🚀 THREAD-OPTIMIZED fragrant species filter - removed excessive Task.Run
    /// </summary>
    public async Task ShowFragrantSpeciesAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            var fragrantSpecies = await _speciesRepository.GetFragrantSpeciesAsync();

            // FIXED: Use BaseListViewModel's efficient item creation instead of Task.Run
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Items.Clear();
                foreach (var species in fragrantSpecies)
                {
                    Items.Add(CreateItemViewModel(species));
                }
                UpdateCounters();
            });

        }, "Show Fragrant Species");
    }

    #endregion
}