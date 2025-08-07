using OrchidPro.Models;
using OrchidPro.Models.Base;

namespace OrchidPro.Services;

/// <summary>
/// Repository interface for Species entity operations.
/// Extends base repository with species-specific methods for hierarchical relationships and botanical data.
/// Provides standardized data access patterns following the same structure as families and genera.
/// </summary>
public interface ISpeciesRepository : IBaseRepository<Species>
{
    #region Hierarchical Queries

    /// <summary>
    /// Get all species belonging to a specific genus
    /// </summary>
    /// <param name="genusId">Genus identifier</param>
    /// <param name="includeInactive">Include inactive species in results</param>
    /// <returns>List of species in the specified genus</returns>
    Task<List<Species>> GetByGenusAsync(Guid genusId, bool includeInactive = false);

    /// <summary>
    /// Get all species belonging to a specific family (through genus relationship)
    /// </summary>
    /// <param name="familyId">Family identifier</param>
    /// <param name="includeInactive">Include inactive species in results</param>
    /// <returns>List of species in the specified family</returns>
    Task<List<Species>> GetByFamilyAsync(Guid familyId, bool includeInactive = false);

    /// <summary>
    /// Check if a species name already exists within the same genus for the current user
    /// </summary>
    /// <param name="name">Species name to check</param>
    /// <param name="genusId">Genus identifier for scope validation</param>
    /// <param name="excludeId">Species ID to exclude from check (for updates)</param>
    /// <returns>True if name exists, false otherwise</returns>
    Task<bool> NameExistsInGenusAsync(string name, Guid genusId, Guid? excludeId = null);

    #endregion

    #region Botanical Queries

    /// <summary>
    /// Search species by scientific name across all genera
    /// </summary>
    /// <param name="scientificName">Scientific name to search</param>
    /// <param name="exactMatch">True for exact match, false for partial match</param>
    /// <returns>List of matching species</returns>
    Task<List<Species>> GetByScientificNameAsync(string scientificName, bool exactMatch = true);

    /// <summary>
    /// Get species by rarity status for conservation tracking
    /// </summary>
    /// <param name="rarityStatus">Rarity status (Common, Uncommon, Rare, Very Rare, Extinct)</param>
    /// <param name="includeInactive">Include inactive species</param>
    /// <returns>List of species with specified rarity status</returns>
    Task<List<Species>> GetByRarityStatusAsync(string rarityStatus, bool includeInactive = false);

    /// <summary>
    /// Get species by size category for space planning
    /// </summary>
    /// <param name="sizeCategory">Size category (Miniature, Small, Medium, Large, Giant)</param>
    /// <param name="includeInactive">Include inactive species</param>
    /// <returns>List of species with specified size category</returns>
    Task<List<Species>> GetBySizeCategoryAsync(string sizeCategory, bool includeInactive = false);

    /// <summary>
    /// Get species that bloom in a specific season
    /// </summary>
    /// <param name="season">Flowering season</param>
    /// <param name="includeInactive">Include inactive species</param>
    /// <returns>List of species that bloom in the specified season</returns>
    Task<List<Species>> GetByFloweringSeasonAsync(string season, bool includeInactive = false);

    /// <summary>
    /// Get fragrant species for specialized collections
    /// </summary>
    /// <param name="includeInactive">Include inactive species</param>
    /// <returns>List of fragrant species</returns>
    Task<List<Species>> GetFragrantSpeciesAsync(bool includeInactive = false);

    #endregion

    #region Cultivation Queries

    /// <summary>
    /// Get species by temperature preference for greenhouse planning
    /// </summary>
    /// <param name="temperaturePreference">Temperature preference (Cool, Intermediate, Warm)</param>
    /// <param name="includeInactive">Include inactive species</param>
    /// <returns>List of species with specified temperature preference</returns>
    Task<List<Species>> GetByTemperaturePreferenceAsync(string temperaturePreference, bool includeInactive = false);

    /// <summary>
    /// Get species by growth habit for mounting and potting decisions
    /// </summary>
    /// <param name="growthHabit">Growth habit (Epiphyte, Terrestrial, Lithophyte)</param>
    /// <param name="includeInactive">Include inactive species</param>
    /// <returns>List of species with specified growth habit</returns>
    Task<List<Species>> GetByGrowthHabitAsync(string growthHabit, bool includeInactive = false);

    /// <summary>
    /// Get species with specific light requirements
    /// </summary>
    /// <param name="lightRequirements">Light requirements (Low, Medium, High, Very High)</param>
    /// <param name="includeInactive">Include inactive species</param>
    /// <returns>List of species with specified light requirements</returns>
    Task<List<Species>> GetByLightRequirementsAsync(string lightRequirements, bool includeInactive = false);

    #endregion

    #region Analytics and Statistics

    /// <summary>
    /// Get species count by genus for collection overview
    /// </summary>
    /// <param name="genusId">Genus identifier</param>
    /// <param name="includeInactive">Include inactive species in count</param>
    /// <returns>Number of species in the genus</returns>
    Task<int> GetCountByGenusAsync(Guid genusId, bool includeInactive = false);

    /// <summary>
    /// Get species statistics for dashboard display
    /// </summary>
    /// <returns>Dictionary with statistical data (total, active, favorites, etc.)</returns>
    Task<Dictionary<string, int>> GetStatisticsAsync();

    /// <summary>
    /// Get the most recently added species for "What's New" features
    /// </summary>
    /// <param name="count">Number of recent species to retrieve</param>
    /// <returns>List of recently added species</returns>
    Task<List<Species>> GetRecentlyAddedAsync(int count = 10);

    /// <summary>
    /// Get species without sufficient cultivation information for data completion workflows
    /// </summary>
    /// <returns>List of species missing cultivation details</returns>
    Task<List<Species>> GetSpeciesNeedingCultivationInfoAsync();

    #endregion

    #region Advanced Search

    /// <summary>
    /// Advanced search with multiple criteria for complex filtering
    /// </summary>
    /// <param name="searchText">Text to search in names and descriptions</param>
    /// <param name="genusId">Optional genus filter</param>
    /// <param name="rarityStatus">Optional rarity status filter</param>
    /// <param name="sizeCategory">Optional size category filter</param>
    /// <param name="temperaturePreference">Optional temperature preference filter</param>
    /// <param name="growthHabit">Optional growth habit filter</param>
    /// <param name="hasFragrance">Optional fragrance filter</param>
    /// <param name="includeInactive">Include inactive species</param>
    /// <returns>List of species matching all specified criteria</returns>
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