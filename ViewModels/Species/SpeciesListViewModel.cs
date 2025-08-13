using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Species;

/// <summary>
/// Species list ViewModel - FIXED to prevent multiple loading cycles
/// </summary>
public partial class SpeciesListViewModel : BaseListViewModel<Models.Species, SpeciesItemViewModel>
{
    #region Private Fields

    private readonly ISpeciesRepository _speciesRepository;
    private bool _isGenusMonitoring = false;
    private bool _hasInitialized = false;

    #endregion

    #region Required Base Class Overrides

    public override string EntityName => "Species";
    public override string EntityNamePlural => "Species";
    public override string EditRoute => "speciesedit";

    #endregion

    #region Constructor

    public SpeciesListViewModel(ISpeciesRepository repository, INavigationService navigationService)
        : base(repository, navigationService)
    {
        _speciesRepository = repository;

        // Initialize custom refresh command
        RefreshSpeciesCommand = new AsyncRelayCommand(RefreshSpeciesAsync);

        this.LogInfo("🚀 ULTRA-OPTIMIZED SpeciesListViewModel initialized - inheriting all BaseListViewModel optimizations");
    }

    #endregion

    #region CRITICAL FIX: Prevent Multiple Loading

    /// <summary>
    /// Override OnAppearingAsync to prevent multiple loading cycles
    /// </summary>
    public override async Task OnAppearingAsync()
    {
        this.LogInfo("Starting ViewModel Appearing");

        try
        {
            // Single initialization check
            if (!_hasInitialized)
            {
                this.LogInfo("Initializing ViewModel for first appearance");
                await InitializeAsync();
                _hasInitialized = true;
                this.LogSuccess("ViewModel initialization completed successfully");
            }

            // Start genus monitoring ONLY once and AFTER initial load
            if (!_isGenusMonitoring)
            {
                _isGenusMonitoring = true;
                _ = MonitorGenusHydrationAsync();
            }

            this.LogInfo("Starting View Appearing");

            // Call base OnAppearingAsync instead of LoadDataAsync directly
            await base.OnAppearingAsync();

            this.LogSuccess("ViewModel Appearing completed successfully");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error during ViewModel appearing");
            throw;
        }
    }

    /// <summary>
    /// Monitor genus hydration WITHOUT triggering additional refreshes
    /// </summary>
    private async Task MonitorGenusHydrationAsync()
    {
        await Task.Run(async () =>
        {
            try
            {
                // Wait for initial data to settle
                await Task.Delay(3000);

                for (int attempt = 0; attempt < 2; attempt++)
                {
                    await Task.Delay(2000 * (attempt + 1)); // 2s, 4s

                    var allSpecies = await _repository.GetAllAsync(false);
                    var speciesWithGenus = allSpecies.Where(s => s.Genus != null).ToList();

                    if (speciesWithGenus.Any())
                    {
                        this.LogInfo($"🔄 Genus hydration detected: {speciesWithGenus.Count} species now have genus data");

                        // CRITICAL FIX: NO REFRESH - just silent UI update
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            try
                            {
                                // Silent update of existing items without full refresh
                                for (int i = 0; i < Items.Count && i < speciesWithGenus.Count; i++)
                                {
                                    var item = Items[i];
                                    var speciesData = speciesWithGenus.FirstOrDefault(s => s.Id == item.Id);
                                    if (speciesData?.Genus != null)
                                    {
                                        item.UpdateGenusInfo(speciesData.Genus.Name);
                                    }
                                }
                                this.LogInfo("🔄 UI silently updated with genus data");
                            }
                            catch (Exception ex)
                            {
                                this.LogError(ex, "Error updating UI with genus data");
                            }
                        });

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Error in genus hydration monitoring");
            }
            finally
            {
                _isGenusMonitoring = false;
            }
        });
    }

    /// <summary>
    /// Custom refresh that prevents conflicts and multiple loading
    /// </summary>
    public async Task RefreshSpeciesAsync()
    {
        if (IsRefreshing || IsLoading)
        {
            this.LogInfo("Refresh already in progress - skipping");
            return;
        }

        try
        {
            IsRefreshing = true;

            // CRITICAL FIX: Direct repository refresh and manual item recreation
            await _repository.RefreshCacheAsync();

            // Get fresh data and recreate items manually
            var allSpecies = await _repository.GetAllAsync(false);

            // Clear and repopulate items manually
            Items.Clear();
            foreach (var species in allSpecies)
            {
                var itemVM = CreateItemViewModel(species);
                Items.Add(itemVM);
            }

            // Apply filters and sorting
            await ApplyFilterCommand.ExecuteAsync(null);

            this.LogSuccess($"Refresh completed - {Items.Count} {EntityNamePlural}");
        }
        catch (Exception ex)
        {
            this.LogError(ex, $"Error during refresh: {ex.Message}");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    #region CRITICAL DEBUG: Sort Testing

    /// <summary>
    /// Debug method to test sorting manually
    /// </summary>
    public void DebugCurrentSort()
    {
        try
        {
            this.LogInfo($"=== SORT DEBUG START ===");
            this.LogInfo($"Current SortOrder: '{SortOrder}'");
            this.LogInfo($"Items count: {Items.Count}");

            for (int i = 0; i < Math.Min(Items.Count, 5); i++)
            {
                var item = Items[i];
                this.LogInfo($"Item {i}: '{item.Name}' - Favorite: {item.IsFavorite} - Created: {item.CreatedAt:dd/MM/yyyy}");
            }

            this.LogInfo($"=== SORT DEBUG END ===");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error in debug sort");
        }
    }

    #endregion

    #endregion

    #region REQUIRED ONLY: CreateItemViewModel

    /// <summary>
    /// Only required override - creates SpeciesItemViewModel instances
    /// </summary>
    protected override SpeciesItemViewModel CreateItemViewModel(Models.Species entity)
    {
        return new SpeciesItemViewModel(entity);
    }

    #endregion

    #region UI COMPATIBILITY: Expose Commands

    /// <summary>
    /// Expose base commands for UI compatibility
    /// </summary>
    public IAsyncRelayCommand<SpeciesItemViewModel> DeleteSingleCommand => DeleteSingleItemCommand;
    public new IAsyncRelayCommand DeleteSelectedCommand => base.DeleteSelectedCommand;
    public IAsyncRelayCommand RefreshSpeciesCommand { get; private set; }

    #endregion
}