using OrchidPro.Models;
using OrchidPro.ViewModels.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Navigation;
using OrchidPro.Extensions;
using CommunityToolkit.Mvvm.Input;

namespace OrchidPro.ViewModels.Substrates;

public partial class SubstratesListViewModel : BaseListViewModel<Substrate, SubstrateItemViewModel>
{
    #region Private Fields

    private readonly ISubstrateRepository _substrateRepository;

    #endregion

    #region Required Base Class Overrides

    public override string EntityName => "Substrate";
    public override string EntityNamePlural => "Substrates";
    public override string EditRoute => "substrateedit";

    #endregion

    #region Constructor

    public SubstratesListViewModel(ISubstrateRepository repository, INavigationService navigationService)
        : base(repository, navigationService)
    {
        _substrateRepository = repository;
        this.LogInfo("🚀 ULTRA CLEAN SubstratesListViewModel - base does everything!");
    }

    #endregion

    #region ONLY REQUIRED: CreateItemViewModel

    protected override SubstrateItemViewModel CreateItemViewModel(Substrate entity)
    {
        return new SubstrateItemViewModel(entity);
    }

    #endregion

    #region UI COMPATIBILITY: Expose Commands

    public IAsyncRelayCommand<SubstrateItemViewModel> DeleteSingleCommand => DeleteSingleItemCommand;
    public new IAsyncRelayCommand DeleteSelectedCommand => base.DeleteSelectedCommand;

    #endregion
}
