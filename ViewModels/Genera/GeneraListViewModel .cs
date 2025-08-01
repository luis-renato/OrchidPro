using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OrchidPro.ViewModels.Genera;

/// <summary>
/// ViewModel for managing list of botanical genera with enhanced base functionality
/// Provides genus-specific implementations while leveraging generic base operations
/// Follows exact pattern from FamiliesListViewModel with corrected interface
/// </summary>
public partial class GeneraListViewModel : BaseListViewModel<Genus, GenusItemViewModel>
{
    #region Private Fields

    // Genus-specific repository for accessing genus-only methods
    private readonly IGenusRepository _genusRepository;
    private readonly IFamilyRepository _familyRepository;

    #endregion

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<Family> availableFamilies = new();

    [ObservableProperty]
    private Family? selectedFamily;

    [ObservableProperty]
    private string familyFilter = "All Families";

    [ObservableProperty]
    private bool showFamilyFilter = true;

    #endregion

    #region Filter Options

    public List<string> FamilyFilterOptions { get; private set; } = new() { "All Families" };

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
    public GeneraListViewModel(IGenusRepository repository, IFamilyRepository familyRepository, INavigationService navigationService)
        : base(repository, navigationService)
    {
        _genusRepository = repository;
        _familyRepository = familyRepository;

        // Initialize genus-specific properties
        EmptyStateMessage = "No genera found. Add your first genus to get started!";
        FabText = "Add Genus";

        // Load families for filtering
        _ = LoadFamiliesAsync();

        this.LogInfo("GeneraListViewModel initialized with family filtering support");
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
            this.LogInfo($"Created GenusItemViewModel for: {entity.Name} (Family: {entity.FamilyName})");
            return itemViewModel;
        }, fallbackValue: new GenusItemViewModel(entity), operationName: "CreateItemViewModel");
    }

    #endregion

    #region Family Management

    /// <summary>
    /// Load available families for filtering
    /// </summary>
    private async Task LoadFamiliesAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Loading families for genus filtering");

            var families = await _familyRepository.GetAllAsync(false); // Only active families

            AvailableFamilies.Clear();
            foreach (var family in families.OrderBy(f => f.Name))
            {
                AvailableFamilies.Add(family);
            }

            // Update filter options
            FamilyFilterOptions.Clear();
            FamilyFilterOptions.Add("All Families");
            FamilyFilterOptions.AddRange(families.Select(f => f.Name));

            this.LogSuccess($"Loaded {families.Count} families for filtering");
        }, "Load Families");
    }

    /// <summary>
    /// Handle family filter selection
    /// </summary>
    [RelayCommand]
    private async Task ApplyFamilyFilter()
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Applying family filter: {FamilyFilter}");

            if (FamilyFilter == "All Families")
            {
                SelectedFamily = null;
            }
            else
            {
                SelectedFamily = AvailableFamilies.FirstOrDefault(f => f.Name == FamilyFilter);
            }

            await RefreshDataAsync();
        }, "Apply Family Filter");
    }

    #endregion

    #region Data Operations (Compatible with BaseListViewModel)

    /// <summary>
    /// Load data with family context (method compatible with base)
    /// </summary>
    private async Task<List<Genus>> LoadDataWithFamilyAsync()
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            List<Genus> genera;

            if (SelectedFamily != null)
            {
                this.LogInfo($"Loading genera for family: {SelectedFamily.Name}");
                genera = await _genusRepository.GetByFamilyAsync(SelectedFamily.Id, false);
            }
            else
            {
                this.LogInfo("Loading all genera with family information");
                genera = await _genusRepository.GetFilteredWithFamilyAsync(
                    searchText: SearchText,
                    isActive: StatusFilter == "Active" ? true : StatusFilter == "Inactive" ? false : null,
                    isFavorite: null
                );
            }

            this.LogInfo($"Loaded {genera.Count} genera");
            return genera;
        }, "Load Genera Data") ?? new List<Genus>();
    }

    /// <summary>
    /// Refresh data using base pattern
    /// </summary>
    private async Task RefreshDataAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            IsRefreshing = true;

            try
            {
                var data = await LoadDataWithFamilyAsync();

                Items.Clear();
                foreach (var item in data)
                {
                    Items.Add(CreateItemViewModel(item));
                }

                await UpdateCountsInternalAsync();
                HasData = Items.Any();
            }
            finally
            {
                IsRefreshing = false;
            }
        }, "Refresh Data");
    }

    #endregion

    #region Navigation Commands

    /// <summary>
    /// Navigate to genus edit page
    /// </summary>
    [RelayCommand]
    private async Task EditGenus(GenusItemViewModel genusItem)
    {
        await this.SafeExecuteAsync(async () =>
        {
            var parameters = new Dictionary<string, object>
            {
                ["genusId"] = genusItem.Id
            };

            this.LogInfo($"Navigating to edit genus: {genusItem.Name}");
            await _navigationService.NavigateToAsync(EditRoute, parameters);
        }, "Navigate to Edit Genus");
    }

    /// <summary>
    /// Navigate to add new genus
    /// </summary>
    [RelayCommand]
    private async Task AddNew()
    {
        await this.SafeExecuteAsync(async () =>
        {
            var parameters = new Dictionary<string, object>();

            // Pass selected family as context if available
            if (SelectedFamily != null)
            {
                parameters["familyId"] = SelectedFamily.Id;
                this.LogInfo($"Navigating to add genus with family context: {SelectedFamily.Name}");
            }
            else
            {
                this.LogInfo("Navigating to add new genus");
            }

            await _navigationService.NavigateToAsync(EditRoute, parameters);
        }, "Navigate to Add Genus");
    }

    /// <summary>
    /// View family details
    /// </summary>
    [RelayCommand]
    private async Task ViewFamily(GenusItemViewModel genusItem)
    {
        await this.SafeExecuteAsync(async () =>
        {
            var parameters = new Dictionary<string, object>
            {
                ["familyId"] = genusItem.FamilyId
            };

            this.LogInfo($"Navigating to view family: {genusItem.FamilyName}");
            await _navigationService.NavigateToAsync("familyedit", parameters);
        }, "Navigate to View Family");
    }

    #endregion

    #region Genus-Specific Operations

    /// <summary>
    /// Toggle genus favorite status
    /// </summary>
    [RelayCommand]
    private async Task ToggleFavorite(GenusItemViewModel genusItem)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Toggling favorite for genus: {genusItem.Name}");

            var updated = await _genusRepository.ToggleFavoriteAsync(genusItem.Id);
            if (updated != null)
            {
                // Update the item in the list
                var index = Items.ToList().FindIndex(i => i.Id == genusItem.Id);
                if (index >= 0)
                {
                    var newItem = CreateItemViewModel(updated);
                    Items[index] = newItem;

                    // Update counts
                    await UpdateCountsInternalAsync();

                    this.LogSuccess($"Toggled favorite: {updated.Name} -> {updated.IsFavorite}");
                }
            }
            else
            {
                this.LogError($"Failed to toggle favorite for genus: {genusItem.Name}");
            }
        }, "Toggle Genus Favorite");
    }

    /// <summary>
    /// Delete genus with confirmation
    /// </summary>
    [RelayCommand]
    private async Task DeleteGenus(GenusItemViewModel genusItem)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Requesting delete confirmation for genus: {genusItem.Name}");

            // Simple confirmation for now
            var confirmed = true; // In real implementation, use confirmation dialog

            if (confirmed)
            {
                var success = await _genusRepository.DeleteAsync(genusItem.Id);
                if (success)
                {
                    Items.Remove(genusItem);
                    await UpdateCountsInternalAsync();

                    this.LogSuccess($"Deleted genus: {genusItem.Name}");

                    // Show success message
                    await ShowSuccessMessage($"Genus '{genusItem.Name}' deleted successfully");
                }
                else
                {
                    this.LogError($"Failed to delete genus: {genusItem.Name}");
                    await ShowErrorMessage($"Failed to delete genus '{genusItem.Name}'");
                }
            }
        }, "Delete Genus");
    }

    /// <summary>
    /// Filter genera by family
    /// </summary>
    [RelayCommand]
    private async Task FilterByFamily(string familyName)
    {
        await this.SafeExecuteAsync(async () =>
        {
            FamilyFilter = familyName;
            await ApplyFamilyFilter();
        }, "Filter By Family");
    }

    #endregion

    #region Sorting and Grouping

    /// <summary>
    /// Apply sorting with family-aware logic
    /// </summary>
    private void ApplySortingInternal()
    {
        this.SafeExecute(() =>
        {
            var sortedItems = SortOrder switch
            {
                "Name A→Z" => Items.OrderBy(i => i.Name),
                "Name Z→A" => Items.OrderByDescending(i => i.Name),
                "Family A→Z" => Items.OrderBy(i => i.FamilyName).ThenBy(i => i.Name),
                "Family Z→A" => Items.OrderByDescending(i => i.FamilyName).ThenBy(i => i.Name),
                "Recent First" => Items.OrderByDescending(i => i.CreatedAt),
                "Oldest First" => Items.OrderBy(i => i.CreatedAt),
                "Favorites First" => Items.OrderByDescending(i => i.IsFavorite).ThenBy(i => i.Name),
                _ => Items.OrderBy(i => i.Name)
            };

            var newItems = new ObservableCollection<GenusItemViewModel>(sortedItems);
            Items.Clear();
            foreach (var item in newItems)
            {
                Items.Add(item);
            }

            this.LogInfo($"Applied sorting: {SortOrder}");
        }, "Apply Genera Sorting");
    }

    /// <summary>
    /// Get grouped genera by family
    /// </summary>
    public IEnumerable<IGrouping<string, GenusItemViewModel>> GetGroupedByFamily()
    {
        return this.SafeExecute(() =>
        {
            return Items.GroupBy(g => g.FamilyName).OrderBy(group => group.Key);
        }, fallbackValue: Enumerable.Empty<IGrouping<string, GenusItemViewModel>>(), operationName: "Group By Family");
    }

    #endregion

    #region Statistics and Counts

    /// <summary>
    /// Update family-specific counts
    /// </summary>
    private async Task UpdateCountsInternalAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            // Update basic counts
            TotalCount = Items.Count;
            ActiveCount = Items.Count(i => i.IsActive);
            FavoriteCount = Items.Count(i => i.IsFavorite);

            // Add family-specific statistics
            var familyGroups = Items.GroupBy(g => g.FamilyName).ToList();
            var familyCount = familyGroups.Count;

            this.LogInfo($"Genera statistics: {TotalCount} total, {FavoriteCount} favorites, {familyCount} families");
        }, "Update Genera Counts");
    }

    /// <summary>
    /// Get genera count by family
    /// </summary>
    public int GetCountByFamily(string familyName)
    {
        return this.SafeExecute(() =>
        {
            return Items.Count(g => g.FamilyName.Equals(familyName, StringComparison.OrdinalIgnoreCase));
        }, fallbackValue: 0, operationName: "Get Count By Family");
    }

    /// <summary>
    /// Get family with most genera
    /// </summary>
    public string? GetMostPopularFamily()
    {
        return this.SafeExecute(() =>
        {
            return Items.GroupBy(g => g.FamilyName)
                       .OrderByDescending(group => group.Count())
                       .FirstOrDefault()?.Key;
        }, operationName: "Get Most Popular Family");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Show success message (compatible with your project structure)
    /// </summary>
    private async Task ShowSuccessMessage(string message)
    {
        try
        {
            // Use your project's toast system if available
            await this.ShowSuccessToast(message);
        }
        catch
        {
            // Fallback logging
            this.LogSuccess(message);
        }
    }

    /// <summary>
    /// Show error message (compatible with your project structure)
    /// </summary>
    private async Task ShowErrorMessage(string message)
    {
        try
        {
            // Use your project's toast system if available
            await this.ShowErrorToast(message);
        }
        catch
        {
            // Fallback logging
            this.LogError(message);
        }
    }

    #endregion

    #region Property Change Handlers

    /// <summary>
    /// Handle family filter changes
    /// </summary>
    partial void OnFamilyFilterChanged(string value)
    {
        this.SafeExecute(() =>
        {
            _ = ApplyFamilyFilter();
        }, "Handle Family Filter Change");
    }

    /// <summary>
    /// Handle selected family changes
    /// </summary>
    partial void OnSelectedFamilyChanged(Family? value)
    {
        this.SafeExecute(() =>
        {
            if (value != null)
            {
                FamilyFilter = value.Name;
                this.LogInfo($"Selected family changed to: {value.Name}");
            }
        }, "Handle Selected Family Change");
    }

    /// <summary>
    /// Handle search text changes (override base behavior)
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        this.SafeExecute(() =>
        {
            _ = RefreshDataAsync();
        }, "Handle Search Text Change");
    }

    /// <summary>
    /// Handle sort order changes (override base behavior)
    /// </summary>
    partial void OnSortOrderChanged(string value)
    {
        this.SafeExecute(() =>
        {
            ApplySortingInternal();
        }, "Handle Sort Order Change");
    }

    #endregion

    #region Public Methods for External Access

    /// <summary>
    /// Manually trigger data refresh
    /// </summary>
    public async Task RefreshAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("=== ENHANCED GENERA REFRESH ===");

            // Reload families in case they changed
            await LoadFamiliesAsync();

            // Refresh data
            await RefreshDataAsync();

            this.LogSuccess("Enhanced genera refresh completed");
        }, "Enhanced Refresh");
    }

    /// <summary>
    /// Load initial data
    /// </summary>
    public async Task LoadDataAsync()
    {
        await RefreshDataAsync();
    }

    #endregion
}