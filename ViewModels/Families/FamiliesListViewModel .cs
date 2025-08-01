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

    // Family-specific repository for accessing family-only methods
    private readonly IFamilyRepository _familyRepository;

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
    public FamiliesListViewModel(IFamilyRepository repository, INavigationService navigationService)
        : base(repository, navigationService)
    {
        _familyRepository = repository;
        this.LogInfo("Initialized - using enhanced base with all extracted functionality");
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
            var itemViewModel = new FamilyItemViewModel(entity);
            this.LogInfo($"Created FamilyItemViewModel for: {entity.Name}");
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

            // Use the family-specific repository directly
            var updatedFamily = await _familyRepository.ToggleFavoriteAsync(item.Id);

            // Update the item by replacing it with updated data
            var index = Items.IndexOf(item);
            if (index >= 0)
            {
                Items[index] = new FamilyItemViewModel(updatedFamily);
                this.LogInfo($"Updated item at index {index} with new favorite status");
            }

            UpdateCounters();
            this.LogSuccess($"Favorite toggled: {item.Name} → {updatedFamily.IsFavorite}");

        }, $"ToggleFavorite failed for {item?.Name}");
    }

    #endregion

    #region Public Commands for XAML Integration

    /// <summary>
    /// Public command for single item deletion used by swipe-to-delete actions
    /// </summary>
    public IAsyncRelayCommand<FamilyItemViewModel> DeleteSingleCommand => DeleteSingleItemSafeCommand;

    #endregion

}