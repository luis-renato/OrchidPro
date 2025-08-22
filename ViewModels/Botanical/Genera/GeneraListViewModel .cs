using OrchidPro.Extensions;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;

namespace OrchidPro.ViewModels.Botanical.Genera;

/// <summary>
/// Genus list ViewModel - ULTRA CLEAN using BaseListViewModel core
/// FIXED: Using SAME strategy as Species - silent family hydration monitoring
/// </summary>
public partial class GeneraListViewModel : BaseListViewModel<Models.Genus, GenusItemViewModel>
{
    #region Private Fields

    private readonly IGenusRepository _genusRepository;
    private bool _isFamilyMonitoring = false;

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
        this.LogInfo("🚀 CLEAN GeneraListViewModel initialized using BaseListViewModel core");
    }

    #endregion

    #region ONLY REQUIRED: CreateItemViewModel

    /// <summary>
    /// Only required override - creates GenusItemViewModel instances
    /// </summary>
    protected override GenusItemViewModel CreateItemViewModel(Models.Genus entity)
    {
        return new GenusItemViewModel(entity);
    }

    #endregion

    #region Genus-Specific: Family Hydration Monitoring (SAME AS SPECIES)

    /// <summary>
    /// Override OnAppearingAsync to add family monitoring (Genus-specific feature)
    /// COPIES EXACT STRATEGY FROM SPECIES - silent monitoring without interference
    /// </summary>
    public override async Task OnAppearingAsync()
    {
        try
        {
            // Call base implementation for all standard functionality (SAME AS SPECIES)
            await base.OnAppearingAsync();

            // Start family monitoring ONLY once and AFTER initial load (SAME AS SPECIES)
            if (!_isFamilyMonitoring)
            {
                _isFamilyMonitoring = true;
                _ = MonitorFamilyHydrationAsync();
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error during Genus ViewModel appearing");
            throw;
        }
    }

    /// <summary>
    /// Genus-specific: Monitor family hydration WITHOUT triggering additional refreshes
    /// COPIES EXACT STRATEGY FROM SPECIES but for Family data
    /// FIXED CA1860: Use Count > 0 instead of Any() for better performance
    /// </summary>
    private async Task MonitorFamilyHydrationAsync()
    {
        await Task.Run(async () =>
        {
            try
            {
                // Wait for initial data to settle (SAME AS SPECIES)
                await Task.Delay(3000);

                for (int attempt = 0; attempt < 2; attempt++)
                {
                    await Task.Delay(2000 * (attempt + 1)); // 2s, 4s (SAME AS SPECIES)

                    // Get genera with family data (parallel to species with genus)
                    var allGenera = await _genusRepository.GetAllWithFamilyAsync(false);
                    var generaWithFamily = allGenera.Where(g => g.Family != null).ToList();

                    // FIXED CA1860: Use Count > 0 instead of Any() for better performance
                    if (generaWithFamily.Count > 0)
                    {
                        this.LogInfo($"🔄 Family hydration detected: {generaWithFamily.Count} genera now have family data");

                        // Silent UI update without full refresh (SAME AS SPECIES)
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            try
                            {
                                // Update existing items with family info (parallel to species genus update)
                                for (int i = 0; i < Items.Count && i < generaWithFamily.Count; i++)
                                {
                                    var item = Items[i];
                                    var genusData = generaWithFamily.FirstOrDefault(g => g.Id == item.Id);
                                    if (genusData?.Family != null)
                                    {
                                        item.UpdateFamilyInfo(genusData.Family.Name);
                                    }
                                }
                                this.LogInfo("🔄 UI silently updated with family data");
                            }
                            catch (Exception ex)
                            {
                                this.LogError(ex, "Error updating UI with family data");
                            }
                        });

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                this.LogError(ex, "Error in family hydration monitoring");
            }
            finally
            {
                _isFamilyMonitoring = false;
            }
        });
    }

    #endregion
}