using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Genera;

/// <summary>
/// ViewModel for managing list of botanical genera with enhanced base functionality.
/// Provides genus-specific implementations while leveraging generic base operations.
/// </summary>
public partial class GeneraListViewModel : BaseListViewModel<Genus, GenusItemViewModel>
{
    #region Private Fields

    // Genus-specific repository for accessing genus-only methods
    private readonly IGenusRepository _genusRepository;

    #endregion

    #region Required Base Class Overrides

    public override string EntityName => "Genus";
    public override string EntityNamePlural => "Genera";
    public override string EditRoute => "genusedit";

    #endregion

    #region Constructor

    /// <summary>
    /// Initialize genera list ViewModel with enhanced base functionality
    /// </summary>
    public GeneraListViewModel(IGenusRepository repository, INavigationService navigationService)
        : base(repository, navigationService)
    {
        _genusRepository = repository;
        this.LogInfo("Initialized - using enhanced base with all extracted functionality");
    }

    #endregion

    #region Required Implementation

    /// <summary>
    /// Create genus-specific item ViewModel from entity
    /// </summary>
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

    #region Genus-Specific Overrides

    /// <summary>
    /// Apply genus-specific sorting logic including favorites-first option
    /// </summary>
    protected override IOrderedEnumerable<GenusItemViewModel> ApplyEntitySpecificSort(IEnumerable<GenusItemViewModel> filtered)
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
    /// Toggle favorite status using genus-specific repository implementation
    /// </summary>
    protected override async Task ToggleFavoriteAsync(GenusItemViewModel item)
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (item?.Id == null) return;

            this.LogInfo($"Toggling favorite for: {item.Name}");

            // Use the genus-specific repository directly
            var updatedGenus = await _genusRepository.ToggleFavoriteAsync(item.Id);

            // Update the item by replacing it with updated data
            var index = Items.IndexOf(item);
            if (index >= 0)
            {
                var newItem = new GenusItemViewModel(updatedGenus);
                newItem.SelectionChangedAction = item.SelectionChangedAction;
                Items[index] = newItem;
                this.LogInfo($"Updated item at index {index} with new favorite status");
            }

            UpdateCounters();
            this.LogSuccess($"Favorite toggled: {item.Name} → {updatedGenus.IsFavorite}");

        }, $"ToggleFavorite failed for {item?.Name}");
    }

    #endregion

    #region Public Commands for XAML Integration

    /// <summary>
    /// ✅ FIXED: Public command for single item deletion - same pattern as FamiliesListViewModel
    /// </summary>
    public IAsyncRelayCommand<GenusItemViewModel> DeleteSingleCommand =>
        new AsyncRelayCommand<GenusItemViewModel>(DeleteSingleAsync);

    private async Task DeleteSingleAsync(GenusItemViewModel? item)
    {
        if (item?.Id == null) return;

        await this.SafeExecuteAsync(async () =>
        {
            // Simple confirmation - no genus validation like Family has
            var confirmed = await ShowConfirmAsync("Delete Genus", $"Are you sure you want to delete '{item.Name}'?");

            if (confirmed)
            {
                await _genusRepository.DeleteAsync(item.Id);
                Items.Remove(item);
                UpdateCounters();
                this.LogSuccess($"Genus deleted: {item.Name}");
            }
            // ✅ FIXED: No toast on cancel - just like Family

        }, $"DeleteSingle failed for {item?.Name}");
    }

    #endregion

}