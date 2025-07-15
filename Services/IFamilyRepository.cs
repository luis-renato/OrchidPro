using OrchidPro.Models;

namespace OrchidPro.Services;

/// <summary>
/// MIGRADO: Interface simplificada para operações com Family
/// Remove complexidade de sincronização, foca em operações diretas
/// </summary>
public interface IFamilyRepository
{
    /// <summary>
    /// Gets all families for the current user including system defaults
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive families</param>
    /// <returns>List of families</returns>
    Task<List<Family>> GetAllAsync(bool includeInactive = false);

    /// <summary>
    /// Gets families with filtering and searching
    /// </summary>
    /// <param name="searchText">Text to search in name and description</param>
    /// <param name="statusFilter">Filter by active/inactive status</param>
    /// <param name="syncFilter">Filter by synchronization status (ignored in simplified architecture)</param>
    /// <returns>Filtered list of families</returns>
    Task<List<Family>> GetFilteredAsync(
        string? searchText = null,
        bool? statusFilter = null,
        SyncStatus? syncFilter = null);

    /// <summary>
    /// Gets a family by its ID
    /// </summary>
    /// <param name="id">Family ID</param>
    /// <returns>Family if found, null otherwise</returns>
    Task<Family?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets a family by its name (case-insensitive)
    /// </summary>
    /// <param name="name">Family name</param>
    /// <returns>Family if found, null otherwise</returns>
    Task<Family?> GetByNameAsync(string name);

    /// <summary>
    /// Creates a new family
    /// </summary>
    /// <param name="family">Family to create</param>
    /// <returns>Created family with generated ID</returns>
    Task<Family> CreateAsync(Family family);

    /// <summary>
    /// Updates an existing family
    /// </summary>
    /// <param name="family">Family to update</param>
    /// <returns>Updated family</returns>
    Task<Family> UpdateAsync(Family family);

    /// <summary>
    /// Soft deletes a family (sets IsActive = false)
    /// </summary>
    /// <param name="id">Family ID to delete</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Soft deletes multiple families
    /// </summary>
    /// <param name="ids">Family IDs to delete</param>
    /// <returns>Number of families deleted</returns>
    Task<int> DeleteMultipleAsync(IEnumerable<Guid> ids);

    /// <summary>
    /// Checks if a family name already exists for the current user
    /// </summary>
    /// <param name="name">Family name to check</param>
    /// <param name="excludeId">ID to exclude from check (for updates)</param>
    /// <returns>True if name exists</returns>
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null);

    /// <summary>
    /// Gets count statistics for dashboard
    /// </summary>
    /// <returns>Family statistics</returns>
    Task<FamilyStatistics> GetStatisticsAsync();

    /// <summary>
    /// MIGRADO: Forces cache refresh from server (replaces complex sync)
    /// </summary>
    /// <returns>Refresh result with statistics</returns>
    Task<SyncResult> ForceFullSyncAsync();

    /// <summary>
    /// NOVO: Manually refresh cache from server
    /// </summary>
    Task RefreshCacheAsync();

    /// <summary>
    /// NOVO: Test connectivity with server
    /// </summary>
    Task<bool> TestConnectionAsync();

    /// <summary>
    /// NOVO: Get cache information for debugging
    /// </summary>
    string GetCacheInfo();
}

/// <summary>
/// Statistics for family data
/// </summary>
public class FamilyStatistics
{
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int SyncedCount { get; set; }
    public int LocalCount { get; set; }
    public int PendingCount { get; set; }
    public int ErrorCount { get; set; }
    public int SystemDefaultCount { get; set; }
    public int UserCreatedCount { get; set; }
    public DateTime LastSyncTime { get; set; }
}