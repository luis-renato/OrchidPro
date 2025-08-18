using OrchidPro.Extensions;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;

namespace OrchidPro.ViewModels.Species;

/// <summary>
/// Species list ViewModel - ULTRA CLEAN using BaseListViewModel core
/// FIXED: No monitoring needed - repository loads with Genus data immediately
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
        this.LogInfo("🚀 CLEAN SpeciesListViewModel initialized using BaseListViewModel core");
    }

    #endregion

    #region ONLY REQUIRED: CreateItemViewModel

    /// <summary>
    /// Only required override - creates SpeciesItemViewModel instances
    /// </summary>
    protected override SpeciesItemViewModel CreateItemViewModel(Models.Species entity)
    {
        return new SpeciesItemViewModel(entity);
    }

    #endregion
}