using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Families;

/// <summary>
/// MINIMAL Family list ViewModel - reduced from ~150 lines to essential code only!
/// All common functionality moved to BaseListViewModel and pattern classes.
/// </summary>
public partial class FamiliesListViewModel : BaseListViewModel<Family, FamilyItemViewModel>
{
    #region Private Fields

    private readonly IFamilyRepository _familyRepository;
    private readonly IGenusRepository _genusRepository;

    #endregion

    #region Required Base Class Overrides

    public override string EntityName => "Family";
    public override string EntityNamePlural => "Families";
    public override string EditRoute => "familyedit";

    #endregion

    #region Constructor

    public FamiliesListViewModel(IFamilyRepository repository, IGenusRepository genusRepository, INavigationService navigationService)
        : base(repository, navigationService)
    {
        _familyRepository = repository;
        _genusRepository = genusRepository;
        this.LogInfo("Initialized - using bases for ALL functionality");
    }

    #endregion

    #region Required Implementation

    protected override FamilyItemViewModel CreateItemViewModel(Family entity)
    {
        return this.SafeExecute(() =>
        {
            var itemViewModel = new FamilyItemViewModel(entity, _genusRepository);
            this.LogInfo($"Created FamilyItemViewModel with genus support for: {entity.Name}");
            return itemViewModel;
        }, fallbackValue: new FamilyItemViewModel(entity), operationName: "CreateItemViewModel");
    }

    #endregion

    #region Family-Specific Sort Override

    protected override IOrderedEnumerable<FamilyItemViewModel> ApplyEntitySpecificSort(IEnumerable<FamilyItemViewModel> filtered)
    {
        return this.SafeExecute(() =>
        {
            this.LogInfo($"Applying sort order: {SortOrder}");

            // Use BaseSortPatterns with family-specific extensions
            var sorted = BaseSortPatterns.ApplyStandardSort(
                filtered,
                SortOrder,
                BaseSortPatterns.ApplyFamilySpecificSort<FamilyItemViewModel>);

            this.LogSuccess($"Sort applied successfully: {SortOrder}");
            return sorted;

        }, fallbackValue: filtered.OrderBy(item => item.Name), operationName: "ApplyEntitySpecificSort");
    }

    #endregion

    #region Delete Operations Using Base Pattern

    public IAsyncRelayCommand<FamilyItemViewModel> DeleteSingleCommand =>
        new AsyncRelayCommand<FamilyItemViewModel>(DeleteSingleWithValidationAsync);

    private async Task DeleteSingleWithValidationAsync(FamilyItemViewModel? item)
    {
        await BaseDeleteOperations.ExecuteHierarchicalDeleteAsync<Family, Genus, FamilyItemViewModel>(
            item,
            _familyRepository,
            _genusRepository,
            EntityName,
            "genus",
            "genera",
            Items,
            UpdateCounters,
            async (title, message) => await ShowConfirmAsync(title, message),
            async (message) => await ShowSuccessToast(message));
    }

    #endregion
}