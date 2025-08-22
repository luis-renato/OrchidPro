using CommunityToolkit.Mvvm.Input;
using OrchidPro.Extensions;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;

namespace OrchidPro.ViewModels.Families;

/// <summary>
/// Families list ViewModel - ULTRA CLEAN using BaseListViewModel core
/// Base does everything automatically!
/// </summary>
public partial class FamiliesListViewModel : BaseListViewModel<Family, FamilyItemViewModel>
{
    #region Private Fields

    private readonly IFamilyRepository _familyRepository;

    #endregion

    #region Required Base Class Overrides

    public override string EntityName => "Family";
    public override string EntityNamePlural => "Families";
    public override string EditRoute => "familyedit";

    #endregion

    #region Constructor

    public FamiliesListViewModel(IFamilyRepository repository, INavigationService navigationService)
        : base(repository, navigationService)
    {
        _familyRepository = repository;
        this.LogInfo("🚀 ULTRA CLEAN FamiliesListViewModel - base does everything!");
    }

    #endregion

    #region ONLY REQUIRED: CreateItemViewModel

    /// <summary>
    /// Only required override - creates FamilyItemViewModel instances
    /// </summary>
    protected override FamilyItemViewModel CreateItemViewModel(Family entity)
    {
        return new FamilyItemViewModel(entity);
    }

    #endregion

    #region UI COMPATIBILITY: Expose Commands

    /// <summary>
    /// Expose base commands for UI compatibility
    /// </summary>
    public IAsyncRelayCommand<FamilyItemViewModel> DeleteSingleCommand => DeleteSingleItemCommand;
    public new IAsyncRelayCommand DeleteSelectedCommand => base.DeleteSelectedCommand;

    #endregion
}