using OrchidPro.Models.Base;

namespace OrchidPro.Services.Base;

/// <summary>
/// Generic interface for hierarchical entity repositories managing parent-child relationships.
/// Provides standardized operations for hierarchical data structures like Family→Genus→Species.
/// Eliminates code duplication across hierarchical entity repositories.
/// </summary>
/// <typeparam name="TChild">Child entity type (e.g., Genus, Species)</typeparam>
/// <typeparam name="TParent">Parent entity type (e.g., Family, Genus)</typeparam>
public interface IHierarchicalRepository<TChild, TParent> : IBaseRepository<TChild>
    where TChild : class, IBaseEntity, IHierarchicalEntity<TParent>
    where TParent : class, IBaseEntity
{
    #region Parent-Child Relationship Queries

    /// <summary>
    /// Gets all child entities belonging to a specific parent
    /// </summary>
    /// <param name="parentId">Parent entity identifier</param>
    /// <param name="includeInactive">Include inactive child entities</param>
    /// <returns>List of child entities under the parent</returns>
    Task<List<TChild>> GetByParentIdAsync(Guid parentId, bool includeInactive = false);

    /// <summary>
    /// Gets filtered child entities by parent with search and status filters
    /// </summary>
    /// <param name="parentId">Parent entity identifier</param>
    /// <param name="searchText">Search text for name/description filtering</param>
    /// <param name="statusFilter">Status filter (true=active, false=inactive, null=all)</param>
    /// <returns>Filtered list of child entities under the parent</returns>
    Task<List<TChild>> GetFilteredByParentAsync(Guid parentId, string? searchText = null, bool? statusFilter = null);

    /// <summary>
    /// Gets count of child entities under a specific parent
    /// </summary>
    /// <param name="parentId">Parent entity identifier</param>
    /// <param name="includeInactive">Include inactive child entities in count</param>
    /// <returns>Count of child entities under the parent</returns>
    Task<int> GetCountByParentAsync(Guid parentId, bool includeInactive = false);

    #endregion

    #region Hierarchical Validation

    /// <summary>
    /// Checks if child entity name exists within a specific parent scope
    /// </summary>
    /// <param name="name">Child entity name to check</param>
    /// <param name="parentId">Parent entity identifier for scope</param>
    /// <param name="excludeId">Optional child entity ID to exclude from check</param>
    /// <returns>True if name exists within parent scope</returns>
    Task<bool> NameExistsInParentAsync(string name, Guid parentId, Guid? excludeId = null);

    /// <summary>
    /// Validates that the parent entity exists and is accessible
    /// </summary>
    /// <param name="parentId">Parent entity identifier to validate</param>
    /// <returns>True if parent is valid and accessible</returns>
    Task<bool> ValidateParentAccessAsync(Guid parentId);

    #endregion

    #region Hierarchical Operations

    /// <summary>
    /// Gets child entities with their parent information populated
    /// </summary>
    /// <param name="children">List of child entities to populate parent data</param>
    /// <returns>List with populated parent navigation properties</returns>
    Task<List<TChild>> PopulateParentDataAsync(List<TChild> children);

    /// <summary>
    /// Gets all child entities with their parent information in a single query
    /// </summary>
    /// <param name="includeInactive">Include inactive child entities</param>
    /// <returns>List of child entities with parent data populated</returns>
    Task<List<TChild>> GetAllWithParentAsync(bool includeInactive = false);

    /// <summary>
    /// Gets filtered child entities with parent information
    /// </summary>
    /// <param name="searchText">Search text for filtering</param>
    /// <param name="statusFilter">Status filter for child entities</param>
    /// <param name="parentId">Optional parent filter</param>
    /// <returns>Filtered child entities with parent data populated</returns>
    Task<List<TChild>> GetFilteredWithParentAsync(string? searchText = null, bool? statusFilter = null, Guid? parentId = null);

    #endregion

    #region Bulk Hierarchical Operations

    /// <summary>
    /// Deletes all child entities belonging to a parent (cascade operation)
    /// </summary>
    /// <param name="parentId">Parent entity identifier</param>
    /// <returns>Number of child entities deleted</returns>
    Task<int> DeleteByParentAsync(Guid parentId);

    /// <summary>
    /// Bulk updates parent assignment for multiple child entities
    /// </summary>
    /// <param name="childIds">List of child entity identifiers</param>
    /// <param name="newParentId">New parent entity identifier</param>
    /// <returns>Number of child entities updated</returns>
    Task<int> BulkUpdateParentAsync(List<Guid> childIds, Guid newParentId);

    #endregion

    #region Hierarchical Statistics

    /// <summary>
    /// Gets statistics for child entities within a specific parent
    /// </summary>
    /// <param name="parentId">Parent entity identifier</param>
    /// <returns>Statistics for child entities under the parent</returns>
    Task<BaseStatistics> GetStatisticsByParentAsync(Guid parentId);

    /// <summary>
    /// Gets hierarchical statistics including parent distribution
    /// </summary>
    /// <returns>Extended statistics showing hierarchical relationships</returns>
    Task<HierarchicalStatistics> GetHierarchicalStatisticsAsync();

    #endregion
}

/// <summary>
/// Extended statistics for hierarchical repositories showing parent-child relationships
/// </summary>
public class HierarchicalStatistics : BaseStatistics
{
    /// <summary>
    /// Number of unique parent entities represented
    /// </summary>
    public int UniqueParentsCount { get; set; }

    /// <summary>
    /// Average number of children per parent
    /// </summary>
    public double AverageChildrenPerParent { get; set; }

    /// <summary>
    /// Parent with most children
    /// </summary>
    public string? MostPopulousParent { get; set; }

    /// <summary>
    /// Count in most populous parent
    /// </summary>
    public int MostPopulousParentCount { get; set; }

    /// <summary>
    /// Number of children without parent assignment (orphaned)
    /// </summary>
    public int OrphanedChildrenCount { get; set; }

    /// <summary>
    /// Distribution of children across parents
    /// </summary>
    public Dictionary<string, int> ParentDistribution { get; set; } = new();

    /// <summary>
    /// Parents with no children
    /// </summary>
    public int EmptyParentsCount { get; set; }

    /// <summary>
    /// Maximum depth in the hierarchy
    /// </summary>
    public int MaxHierarchyDepth { get; set; }
}