using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Genera;

/// <summary>
/// MINIMAL Genus list ViewModel - reduced from ~120 lines to essential code only!
/// All common functionality moved to BaseListViewModel and pattern classes.
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
        this.LogInfo("Initialized - using bases for ALL functionality");
    }

    #endregion

    #region Required Implementation

    protected override GenusItemViewModel CreateItemViewModel(Genus entity)
    {
        return this.SafeExecute(() =>
        {
            var itemViewModel = new GenusItemViewModel(entity);
            this.LogInfo($"Created GenusItemViewModel for: {entity.Name}");
            return itemViewModel;
        }, fallbackValue: new GenusItemViewModel(entity), operationName: "CreateItemViewModel");
    }

    #endregion

    #region Genus-Specific Sort Override (Standard Only)

    protected override IOrderedEnumerable<GenusItemViewModel> ApplyEntitySpecificSort(IEnumerable<GenusItemViewModel> filtered)
    {
        return this.SafeExecute(() =>
        {
            this.LogInfo($"Applying sort order: {SortOrder}");

            // Use BaseSortPatterns with standard sorts only
            var sorted = BaseSortPatterns.ApplyStandardSort<GenusItemViewModel>(
                filtered,
                SortOrder);

            this.LogSuccess($"Sort applied successfully: {SortOrder}");
            return sorted;

        }, fallbackValue: filtered.OrderBy(item => item.Name), operationName: "ApplyEntitySpecificSort");
    }

    #endregion

    #region Delete Operations Using Base Pattern

    public IAsyncRelayCommand<GenusItemViewModel> DeleteSingleCommand =>
        new AsyncRelayCommand<GenusItemViewModel>(DeleteSingleAsync);

    private async Task DeleteSingleAsync(GenusItemViewModel? item)
    {
        await BaseDeleteOperations.ExecuteSimpleDeleteAsync<Genus, GenusItemViewModel>(
            item,
            _genusRepository,
            EntityName,
            Items,
            UpdateCounters,
            async (title, message) => await ShowConfirmAsync(title, message),
            async (message) => await ShowSuccessToast(message));
    }

    #endregion
}