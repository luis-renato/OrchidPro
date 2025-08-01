using OrchidPro.Models;
using OrchidPro.Models.Base;

namespace OrchidPro.Services;

/// <summary>
/// Repository interface for Genus entities extending base functionality
/// Provides genus-specific operations while maintaining base contract
/// </summary>
public interface IGenusRepository : IBaseRepository<Genus>
{
    /// <summary>
    /// Get all genera for a specific family
    /// </summary>
    /// <param name="familyId">Family ID to filter by</param>
    /// <param name="includeInactive">Include inactive genera</param>
    /// <returns>List of genera belonging to the family</returns>
    Task<List<Genus>> GetByFamilyAsync(Guid familyId, bool includeInactive = false);

    /// <summary>
    /// Get filtered genera with family name included
    /// </summary>
    /// <param name="searchText">Text to search in genus names</param>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="isFavorite">Filter by favorite status</param>
    /// <param name="familyId">Optional family filter</param>
    /// <returns>Filtered list of genera with family names</returns>
    Task<List<Genus>> GetFilteredWithFamilyAsync(string? searchText = null, bool? isActive = null, bool? isFavorite = null, Guid? familyId = null);

    /// <summary>
    /// Toggle favorite status of a genus
    /// </summary>
    /// <param name="genusId">ID of genus to toggle</param>
    /// <returns>Updated genus or null if not found</returns>
    Task<Genus?> ToggleFavoriteAsync(Guid genusId);

    /// <summary>
    /// Get genera count for a specific family
    /// </summary>
    /// <param name="familyId">Family ID</param>
    /// <param name="includeInactive">Include inactive genera in count</param>
    /// <returns>Number of genera in the family</returns>
    Task<int> GetCountByFamilyAsync(Guid familyId, bool includeInactive = false);

    /// <summary>
    /// Bulk delete genera by family ID (when family is deleted)
    /// </summary>
    /// <param name="familyId">Family ID</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteByFamilyAsync(Guid familyId);

    /// <summary>
    /// Check if genus name exists within the same family
    /// </summary>
    /// <param name="name">Genus name to check</param>
    /// <param name="familyId">Family ID context</param>
    /// <param name="excludeId">Genus ID to exclude from check (for edit mode)</param>
    /// <returns>True if name exists</returns>
    Task<bool> ExistsInFamilyAsync(string name, Guid familyId, Guid? excludeId = null);
}