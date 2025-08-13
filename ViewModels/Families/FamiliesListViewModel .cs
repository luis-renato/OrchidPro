using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Families;

/// <summary>
/// 🚀 SPECIES CLONE - Simple Family list ViewModel exactly like Species
/// NO genus counts, NO complexity, JUST stable core functionality
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
        this.LogInfo("SPECIES CLONE: FamiliesListViewModel initialized - simple and stable");
    }
    #endregion

    #region Required Implementation
    protected override FamilyItemViewModel CreateItemViewModel(Family entity)
    {
        return this.SafeExecute(() =>
        {
            var itemViewModel = new FamilyItemViewModel(entity);
            this.LogInfo($"Created FamilyItemViewModel for: {entity.Name}");
            return itemViewModel;
        }, fallbackValue: new FamilyItemViewModel(entity), operationName: "CreateItemViewModel");
    }
    #endregion

    #region UI Compatibility Layer

    /// <summary>
    /// 🚀 UI Compatibility: DeleteSingleCommand for XAML binding
    /// </summary>
    public IAsyncRelayCommand<FamilyItemViewModel> DeleteSingleCommand => DeleteSingleItemCommand;

    #endregion
}