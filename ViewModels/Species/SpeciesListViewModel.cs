using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Species;

/// <summary>
/// Species list ViewModel - ULTRA CLEAN using BaseListViewModel core
/// Reduced from complex implementation to bare minimum required overrides
/// </summary>
public partial class SpeciesListViewModel : BaseListViewModel<Models.Species, SpeciesItemViewModel>
{
    #region Private Fields

    private readonly ISpeciesRepository _speciesRepository;
    private bool _isGenusMonitoring = false;

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
        this.LogInfo("🚀 CLEAN SpeciesListViewModel initialized using BaseListViewModel core");
    }

    #endregion

    #region ONLY REQUIRED: CreateItemViewModel

    /// <summary>
    /// Only required override - creates SpeciesItemViewModel instances
    /// </summary>
    protected override SpeciesItemViewModel CreateItemViewModel(Models.Species entity)
    {
        return new SpeciesItemViewModel(entity);
    }

    #endregion

    #region Species-Specific: Genus Hydration Monitoring

    /// <summary>
    /// Override OnAppearingAsync to add genus monitoring (Species-specific feature)
    /// </summary>
    public override async Task OnAppearingAsync()
    {
        try
        {
            // Call base implementation for all standard functionality
            await base.OnAppearingAsync();

            // Start genus monitoring ONLY once and AFTER initial load
            if (!_isGenusMonitoring)
            {
                _isGenusMonitoring = true;
                _ = MonitorGenusHydrationAsync();
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error during Species ViewModel appearing");
            throw;
        }
    }

    /// <summary>
    /// Species-specific: Monitor genus hydration WITHOUT triggering additional refreshes
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

                        // Silent UI update without full refresh
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            try
                            {
                                // Update existing items with genus info
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

    #endregion

    #region REMOVED - Now in Base

    // ❌ REMOVED: RefreshSpeciesAsync - Use base RefreshAsync
    // ❌ REMOVED: RefreshSpeciesCommand - Use base RefreshCommand
    // ❌ REMOVED: All UI compatibility exposures - Use base commands directly
    // ❌ REMOVED: _hasInitialized flag - Base handles initialization
    // ❌ REMOVED: Custom refresh logic - Base handles all refresh scenarios

    #endregion
}