using OrchidPro.Models;
using OrchidPro.Models.Base;

namespace OrchidPro.Services.Contracts;

public interface ISpeciesRepository : IBaseRepository<Species>
{
    #region Hierarchical Queries
    Task<List<Species>> GetByGenusAsync(Guid genusId, bool includeInactive = false);
    Task<List<Species>> GetByFamilyAsync(Guid familyId, bool includeInactive = false);
    Task<bool> NameExistsInGenusAsync(string name, Guid genusId, Guid? excludeId = null);
    #endregion

    #region Botanical Queries
    Task<List<Species>> GetByScientificNameAsync(string scientificName, bool exactMatch = true);
    Task<List<Species>> GetByRarityStatusAsync(string rarityStatus, bool includeInactive = false);
    Task<List<Species>> GetBySizeCategoryAsync(string sizeCategory, bool includeInactive = false);
    Task<List<Species>> GetByFloweringSeasonAsync(string season, bool includeInactive = false);
    Task<List<Species>> GetFragrantSpeciesAsync(bool includeInactive = false);
    #endregion

    #region Cultivation Queries
    Task<List<Species>> GetByTemperaturePreferenceAsync(string temperaturePreference, bool includeInactive = false);
    Task<List<Species>> GetByGrowthHabitAsync(string growthHabit, bool includeInactive = false);
    Task<List<Species>> GetByLightRequirementsAsync(string lightRequirements, bool includeInactive = false);
    #endregion

    #region Analytics and Statistics
    Task<int> GetCountByGenusAsync(Guid genusId, bool includeInactive = false);
    Task<Dictionary<string, int>> GetSpeciesStatisticsAsync();
    Task<List<Species>> GetRecentlyAddedAsync(int count = 10);
    Task<List<Species>> GetSpeciesNeedingCultivationInfoAsync();
    #endregion

    #region Advanced Search
    Task<List<Species>> SearchAdvancedAsync(
        string? searchText = null,
        Guid? genusId = null,
        string? rarityStatus = null,
        string? sizeCategory = null,
        string? temperaturePreference = null,
        string? growthHabit = null,
        bool? hasFragrance = null,
        bool includeInactive = false);
    #endregion
}