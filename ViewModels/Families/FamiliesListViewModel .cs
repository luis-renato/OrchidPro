using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Families;

/// <summary>
/// ViewModel for managing list of botanical families with enhanced base functionality.
/// Provides family-specific implementations while leveraging generic base operations.
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

    /// <summary>
    /// Initialize families list ViewModel with enhanced base functionality
    /// </summary>
    public FamiliesListViewModel(IFamilyRepository repository, IGenusRepository genusRepository, INavigationService navigationService)
        : base(repository, navigationService)
    {
        _familyRepository = repository;
        _genusRepository = genusRepository;
        this.LogInfo("Initialized with genus count support");
    }

    #endregion

    #region Required Implementation

    /// <summary>
    /// Create family-specific item ViewModel from entity
    /// </summary>
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

    #region Family-Specific Overrides

    /// <summary>
    /// Apply family-specific sorting logic including favorites-first option
    /// </summary>
    protected override IOrderedEnumerable<FamilyItemViewModel> ApplyEntitySpecificSort(IEnumerable<FamilyItemViewModel> filtered)
    {
        return this.SafeExecute(() =>
        {
            this.LogInfo($"Applying sort order: {SortOrder}");

            var sorted = SortOrder switch
            {
                "Name A→Z" => filtered.OrderBy(item => item.Name),
                "Name Z→A" => filtered.OrderByDescending(item => item.Name),
                "Recent First" => filtered.OrderByDescending(item => item.UpdatedAt),
                "Oldest First" => filtered.OrderBy(item => item.CreatedAt),
                "Favorites First" => filtered.OrderByDescending(item => item.IsFavorite).ThenBy(item => item.Name),
                "Most Genera" => filtered.OrderByDescending(item => item.GenusCount).ThenBy(item => item.Name),
                "Fewest Genera" => filtered.OrderBy(item => item.GenusCount).ThenBy(item => item.Name),
                _ => filtered.OrderBy(item => item.Name)
            };

            this.LogSuccess($"Sort applied successfully: {SortOrder}");
            return sorted;

        }, fallbackValue: filtered.OrderBy(item => item.Name), operationName: "ApplyEntitySpecificSort");
    }

    /// <summary>
    /// Toggle favorite status using family-specific repository implementation
    /// </summary>
    protected override async Task ToggleFavoriteAsync(FamilyItemViewModel item)
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (item?.Id == null) return;

            this.LogInfo($"Toggling favorite for: {item.Name}");

            var updatedFamily = await _familyRepository.ToggleFavoriteAsync(item.Id);

            var index = Items.IndexOf(item);
            if (index >= 0)
            {
                var newItem = new FamilyItemViewModel(updatedFamily, _genusRepository);
                newItem.SelectionChangedAction = item.SelectionChangedAction;
                Items[index] = newItem;
                this.LogInfo($"Updated item at index {index} with new favorite status");
            }

            UpdateCounters();
            this.LogSuccess($"Favorite toggled: {item.Name} → {updatedFamily.IsFavorite}");

        }, $"ToggleFavorite failed for {item?.Name}");
    }

    #endregion

    #region Public Commands for XAML Integration

    /// <summary>
    /// Enhanced single delete command with genus validation
    /// </summary>
    public IAsyncRelayCommand<FamilyItemViewModel> DeleteSingleCommand =>
        new AsyncRelayCommand<FamilyItemViewModel>(DeleteSingleWithValidationAsync);

    private async Task DeleteSingleWithValidationAsync(FamilyItemViewModel? item)
    {
        if (item?.Id == null) return;

        await this.SafeExecuteAsync(async () =>
        {
            var genusCount = await _genusRepository.GetCountByFamilyAsync(item.Id, includeInactive: true);

            bool confirmed;
            if (genusCount > 0)
            {
                var genusText = genusCount == 1 ? "genus" : "genera";
                confirmed = await ShowConfirmAsync("Delete Family with Genera",
                    $"Family '{item.Name}' has {genusCount} {genusText}. Deleting will also remove all {genusText}. Continue?");
            }
            else
            {
                confirmed = await ShowConfirmAsync("Delete Family", $"Are you sure you want to delete '{item.Name}'?");
            }

            if (confirmed)
            {
                await _familyRepository.DeleteAsync(item.Id);
                Items.Remove(item);
                UpdateCounters();
                this.LogSuccess($"Family deleted: {item.Name}");
            }

        }, $"DeleteSingle failed for {item?.Name}");
    }

    #endregion
}