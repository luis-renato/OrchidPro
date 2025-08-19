using OrchidPro.Models;
using OrchidPro.Models.Base;

namespace OrchidPro.Services;

/// <summary>
/// Repository interface for genus-specific operations extending base repository functionality.
/// Provides genus-specific queries including family relationships and hierarchical operations.
/// </summary>
public interface IGenusRepository : IBaseRepository<Genus>
{
    #region Family-Specific Queries

    /// <summary>
    /// Gets all genera belonging to a specific family
    /// </summary>
    /// <param name="familyId">Family identifier</param>
    /// <param name="includeInactive">Include inactive genera</param>
    /// <returns>List of genera in the family</returns>
    Task<List<Genus>> GetByFamilyIdAsync(Guid familyId, bool includeInactive = false);

    /// <summary>
    /// Gets filtered genera by family with search and status filter
    /// </summary>
    /// <param name="familyId">Family identifier</param>
    /// <param name="searchText">Search text for name/description</param>
    /// <param name="statusFilter">Status filter (true=active, false=inactive, null=all)</param>
    /// <returns>Filtered list of genera</returns>
    Task<List<Genus>> GetFilteredByFamilyAsync(Guid familyId, string? searchText = null, bool? statusFilter = null);

    /// <summary>
    /// Gets count of genera in a specific family
    /// </summary>
    /// <param name="familyId">Family identifier</param>
    /// <param name="includeInactive">Include inactive genera in count</param>
    /// <returns>Count of genera</returns>
    Task<int> GetCountByFamilyAsync(Guid familyId, bool includeInactive = false);

    #endregion

    #region Validation and Business Logic

    /// <summary>
    /// Checks if genus name exists within a specific family
    /// </summary>
    /// <param name="name">Genus name to check</param>
    /// <param name="familyId">Family identifier</param>
    /// <param name="excludeId">Optional genus ID to exclude from check</param>
    /// <returns>True if name exists in family</returns>
    Task<bool> NameExistsInFamilyAsync(string name, Guid familyId, Guid? excludeId = null);

    /// <summary>
    /// Validates that the family exists and is accessible to current user
    /// </summary>
    /// <param name="familyId">Family identifier to validate</param>
    /// <returns>True if family is valid and accessible</returns>
    Task<bool> ValidateFamilyAccessAsync(Guid familyId);

    #endregion

    #region Favorites Management

    /// <summary>
    /// Toggle favorite status for a genus
    /// </summary>
    /// <param name="genusId">Genus identifier</param>
    /// <returns>Updated genus with toggled favorite status</returns>
    Task<Genus> ToggleFavoriteAsync(Guid genusId);

    #endregion

    #region Hierarchical Operations

    /// <summary>
    /// Gets full family information for genera in results
    /// </summary>
    /// <param name="genera">List of genera to populate family data</param>
    /// <returns>List with populated family navigation properties</returns>
    Task<List<Genus>> PopulateFamilyDataAsync(List<Genus> genera);

    /// <summary>
    /// Gets genera with their family information in a single query
    /// </summary>
    /// <param name="includeInactive">Include inactive items</param>
    /// <returns>List of genera with family data populated</returns>
    Task<List<Genus>> GetAllWithFamilyAsync(bool includeInactive = false);

    /// <summary>
    /// Gets filtered genera with family information
    /// </summary>
    /// <param name="searchText">Search text</param>
    /// <param name="statusFilter">Status filter</param>
    /// <param name="familyId">Optional family filter</param>
    /// <returns>Filtered genera with family data</returns>
    Task<List<Genus>> GetFilteredWithFamilyAsync(string? searchText = null, bool? statusFilter = null, Guid? familyId = null);

    #endregion

    #region Statistics and Analytics

    /// <summary>
    /// Gets genus statistics including family distribution
    /// </summary>
    /// <returns>Extended statistics for genera</returns>
    Task<GenusStatistics> GetGenusStatisticsAsync();

    /// <summary>
    /// Gets statistics for genera within a specific family
    /// </summary>
    /// <param name="familyId">Family identifier</param>
    /// <returns>Statistics for genera in family</returns>
    Task<BaseStatistics> GetStatisticsByFamilyAsync(Guid familyId);

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Deletes all genera belonging to a family (cascade operation)
    /// </summary>
    /// <param name="familyId">Family identifier</param>
    /// <returns>Number of genera deleted</returns>
    Task<int> DeleteByFamilyAsync(Guid familyId);

    /// <summary>
    /// Bulk updates family assignment for multiple genera
    /// </summary>
    /// <param name="genusIds">List of genus identifiers</param>
    /// <param name="newFamilyId">New family identifier</param>
    /// <returns>Number of genera updated</returns>
    Task<int> BulkUpdateFamilyAsync(List<Guid> genusIds, Guid newFamilyId);

    #endregion
}

/// <summary>
/// Extended statistics for genus repository including family relationships
/// </summary>
public class GenusStatistics : BaseStatistics
{
    /// <summary>
    /// Number of unique families represented
    /// </summary>
    public int UniqueFamiliesCount { get; set; }

    /// <summary>
    /// Average number of genera per family
    /// </summary>
    public double AverageGeneraPerFamily { get; set; }

    /// <summary>
    /// Family with most genera
    /// </summary>
    public string? MostPopulousFamily { get; set; }

    /// <summary>
    /// Count in most populous family
    /// </summary>
    public int MostPopulousFamilyCount { get; set; }

    /// <summary>
    /// Number of genera without family assignment (orphaned)
    /// </summary>
    public int OrphanedGeneraCount { get; set; }

    /// <summary>
    /// Distribution of genera across families
    /// </summary>
    public Dictionary<string, int> FamilyDistribution { get; set; } = [];
}