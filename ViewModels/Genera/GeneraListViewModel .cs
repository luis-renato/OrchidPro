using CommunityToolkit.Mvvm.Input;
using OrchidPro.Extensions;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;

namespace OrchidPro.ViewModels.Genera;

/// <summary>
/// Genera list ViewModel - CLEAN VERSION using BaseListViewModel core
/// </summary>
public partial class GeneraListViewModel : BaseListViewModel<Genus, GenusItemViewModel>
{
    #region Private Fields

    private readonly IGenusRepository _genusRepository;

    #endregion

    #region Required Base Class Overrides

    public override string EntityName => "Genus";
    public override string EntityNamePlural => "Genera";
    public override string EditRoute => "genusedit";

    #endregion

    #region Constructor

    public GeneraListViewModel(IGenusRepository repository, INavigationService navigationService)
        : base(repository, navigationService)
    {
        _genusRepository = repository;

        this.LogInfo("Initialized CLEAN GeneraListViewModel using BaseListViewModel core");
    }

    #endregion

    #region Required Override

    /// <summary>
    /// Create GenusItemViewModel from Genus entity
    /// </summary>
    protected override GenusItemViewModel CreateItemViewModel(Genus entity)
    {
        return new GenusItemViewModel(entity);
    }

    #endregion

    #region UI COMPATIBILITY: Expose Commands

    /// <summary>
    /// Expose base commands for UI compatibility
    /// </summary>
    public IAsyncRelayCommand<GenusItemViewModel> DeleteSingleCommand => DeleteSingleItemCommand;
    public new IAsyncRelayCommand DeleteSelectedCommand => base.DeleteSelectedCommand;

    #endregion
}