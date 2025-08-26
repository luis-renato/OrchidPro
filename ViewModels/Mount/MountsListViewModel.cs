using OrchidPro.Models;
using OrchidPro.ViewModels.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Navigation;
using OrchidPro.Extensions;
using CommunityToolkit.Mvvm.Input;

namespace OrchidPro.ViewModels.Mounts;

public partial class MountsListViewModel : BaseListViewModel<Mount, MountItemViewModel>
{
    #region Private Fields

    private readonly IMountRepository _MountRepository;

    #endregion

    #region Required Base Class Overrides

    public override string EntityName => "Mount";
    public override string EntityNamePlural => "Mounts";
    public override string EditRoute => "mountedit";

    #endregion

    #region Constructor

    public MountsListViewModel(IMountRepository repository, INavigationService navigationService)
        : base(repository, navigationService)
    {
        _MountRepository = repository;
        this.LogInfo("🚀 ULTRA CLEAN MountsListViewModel - base does everything!");
    }

    #endregion

    #region ONLY REQUIRED: CreateItemViewModel

    protected override MountItemViewModel CreateItemViewModel(Mount entity)
    {
        return new MountItemViewModel(entity);
    }

    #endregion

    #region UI COMPATIBILITY: Expose Commands

    public IAsyncRelayCommand<MountItemViewModel> DeleteSingleCommand => DeleteSingleItemCommand;
    public new IAsyncRelayCommand DeleteSelectedCommand => base.DeleteSelectedCommand;

    #endregion
}