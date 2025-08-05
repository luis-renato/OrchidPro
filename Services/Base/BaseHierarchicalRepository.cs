using OrchidPro.Models.Base;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;

namespace OrchidPro.Services.Base;

/// <summary>
/// Base implementation for hierarchical repositories managing parent-child relationships.
/// Provides comprehensive operations for hierarchical data structures with intelligent caching.
/// Eliminates massive code duplication across Family→Genus→Species and future hierarchical entities.
/// </summary>
/// <typeparam name="TChild">Child entity type</typeparam>
/// <typeparam name="TParent">Parent entity type</typeparam>
public abstract class BaseHierarchicalRepository<TChild, TParent> : BaseRepository<TChild>, IHierarchicalRepository<TChild, TParent>
    where TChild : class, IBaseEntity, IHierarchicalEntity<TParent>, new()
    where TParent : class, IBaseEntity
{
    #region Protected Fields

    protected readonly IBaseRepository<TParent> _parentRepository;

    #endregion

    #region Abstract Properties

    /// <summary>
    /// Parent entity type name for logging and error messages
    /// </summary>
    protected abstract string ParentEntityTypeName { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initialize hierarchical repository with parent repository dependency
    /// </summary>
    protected BaseHierarchicalRepository(
        SupabaseService supabaseService,
        IBaseRepository<TParent> parentRepository)
        : base(supabaseService)
    {
        _parentRepository = parentRepository ?? throw new ArgumentNullException(nameof(parentRepository));
        this.LogInfo($"{EntityTypeName}Repository initialized with {ParentEntityTypeName} hierarchy support");
    }

    #endregion

    #region IHierarchicalRepository Implementation

    /// <summary>
    /// Gets all child entities belonging to a specific parent
    /// </summary>
    public virtual async Task<List<TChild>> GetByParentIdAsync(Guid parentId, bool includeInactive = false)
    {
        using (this.LogPerformance($"Get {EntityTypeName} by {ParentEntityTypeName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var allChildren = await GetAllAsync(includeInactive);
                return allChildren.Where(c => c.GetParentId() == parentId).ToList();
            }, $"{EntityTypeName} by {ParentEntityTypeName}");

            return result.Success && result.Data != null ? result.Data : new List<TChild>();
        }
    }

    /// <summary>
    /// Gets filtered child entities by parent with search and status filters
    /// </summary>
    public virtual async Task<List<TChild>> GetFilteredByParentAsync(Guid parentId, string? searchText = null, bool? statusFilter = null)
    {
        using (this.LogPerformance($"Get Filtered {EntityTypeName} by {ParentEntityTypeName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var parentChildren = await GetByParentIdAsync(parentId, statusFilter != true);
                var filtered = parentChildren.AsEnumerable();

                // Apply text search if provided
                if (!string.IsNullOrEmpty(searchText))
                {
                    filtered = ApplyTextSearch(filtered, searchText);
                }

                // Apply status filter if provided
                if (statusFilter.HasValue)
                {
                    filtered = filtered.Where(c => c.IsActive == statusFilter.Value);
                }

                return filtered.ToList();
            }, $"Filtered {EntityTypeName} by {ParentEntityTypeName}");

            return result.Success && result.Data != null ? result.Data : new List<TChild>();
        }
    }

    /// <summary>
    /// Gets count of child entities under a specific parent
    /// </summary>
    public virtual async Task<int> GetCountByParentAsync(Guid parentId, bool includeInactive = false)
    {
        using (this.LogPerformance($"Get {EntityTypeName} Count by {ParentEntityTypeName}"))
        {
            var children = await GetByParentIdAsync(parentId, includeInactive);
            return children.Count;
        }
    }

    #endregion

    #region Hierarchical Validation

    /// <summary>
    /// Checks if child entity name exists within a specific parent scope
    /// </summary>
    public virtual async Task<bool> NameExistsInParentAsync(string name, Guid parentId, Guid? excludeId = null)
    {
        using (this.LogPerformance($"Check Name Exists in {ParentEntityTypeName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var parentChildren = await GetByParentIdAsync(parentId, true);
                var exists = parentChildren.Any(c =>
                    string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase) &&
                    c.Id != excludeId);

                this.LogInfo($"Name '{name}' exists in {ParentEntityTypeName}: {exists}");
                return exists;
            }, "Name Check in Parent");

            return result.Success && result.Data;
        }
    }

    /// <summary>
    /// Validates that the parent entity exists and is accessible
    /// </summary>
    public virtual async Task<bool> ValidateParentAccessAsync(Guid parentId)
    {
        using (this.LogPerformance($"Validate {ParentEntityTypeName} Access"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var parent = await _parentRepository.GetByIdAsync(parentId);
                return parent != null;
            }, $"{ParentEntityTypeName} Validation");

            return result.Success && result.Data;
        }
    }

    #endregion

    #region Hierarchical Operations

    /// <summary>
    /// Gets child entities with their parent information populated
    /// </summary>
    public virtual async Task<List<TChild>> PopulateParentDataAsync(List<TChild> children)
    {
        if (!children.Any()) return children;

        using (this.LogPerformance($"Populate {ParentEntityTypeName} Data"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var parentIds = children.Select(c => c.GetParentId()).Distinct().ToList();
                var parents = new Dictionary<Guid, TParent>();

                foreach (var parentId in parentIds)
                {
                    var parent = await _parentRepository.GetByIdAsync(parentId);
                    if (parent != null)
                    {
                        parents[parentId] = parent;
                    }
                }

                foreach (var child in children)
                {
                    if (parents.TryGetValue(child.GetParentId(), out var parent))
                    {
                        child.Parent = parent;
                    }
                }

                return children;
            }, $"Populate {ParentEntityTypeName} Data");

            return result.Success && result.Data != null ? result.Data : children;
        }
    }

    /// <summary>
    /// Gets all child entities with their parent information in a single query
    /// </summary>
    public virtual async Task<List<TChild>> GetAllWithParentAsync(bool includeInactive = false)
    {
        var children = await GetAllAsync(includeInactive);
        return await PopulateParentDataAsync(children);
    }

    /// <summary>
    /// Gets filtered child entities with parent information
    /// </summary>
    public virtual async Task<List<TChild>> GetFilteredWithParentAsync(string? searchText = null, bool? statusFilter = null, Guid? parentId = null)
    {
        List<TChild> children;
        if (parentId.HasValue)
        {
            children = await GetFilteredByParentAsync(parentId.Value, searchText, statusFilter);
        }
        else
        {
            children = await GetFilteredAsync(searchText, statusFilter);
        }

        return await PopulateParentDataAsync(children);
    }

    #endregion

    #region Bulk Hierarchical Operations

    /// <summary>
    /// Deletes all child entities belonging to a parent (cascade operation)
    /// </summary>
    public virtual async Task<int> DeleteByParentAsync(Guid parentId)
    {
        using (this.LogPerformance($"Delete {EntityTypeName} by {ParentEntityTypeName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var parentChildren = await GetByParentIdAsync(parentId, true);
                if (parentChildren.Any())
                {
                    var ids = parentChildren.Select(c => c.Id).ToList();
                    return await DeleteMultipleAsync(ids);
                }
                return 0;
            }, $"Delete by {ParentEntityTypeName}");

            return result.Success ? result.Data : 0;
        }
    }

    /// <summary>
    /// Bulk updates parent assignment for multiple child entities
    /// </summary>
    public virtual async Task<int> BulkUpdateParentAsync(List<Guid> childIds, Guid newParentId)
    {
        using (this.LogPerformance($"Bulk Update {ParentEntityTypeName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                // Validate new parent exists
                var parentExists = await ValidateParentAccessAsync(newParentId);
                if (!parentExists)
                {
                    throw new ArgumentException($"{ParentEntityTypeName} with ID {newParentId} not found");
                }

                var updatedCount = 0;
                foreach (var childId in childIds)
                {
                    var child = await GetByIdAsync(childId);
                    if (child != null)
                    {
                        child.SetParentId(newParentId);
                        var updated = await UpdateAsync(child);
                        if (updated != null) updatedCount++;
                    }
                }

                this.LogDataOperation("Bulk updated parent", EntityTypeName, $"{updatedCount} items");
                return updatedCount;
            }, $"Bulk Update {ParentEntityTypeName}");

            return result.Success ? result.Data : 0;
        }
    }

    #endregion

    #region Hierarchical Statistics

    /// <summary>
    /// Gets statistics for child entities within a specific parent
    /// </summary>
    public virtual async Task<BaseStatistics> GetStatisticsByParentAsync(Guid parentId)
    {
        using (this.LogPerformance($"Get {EntityTypeName} Statistics by {ParentEntityTypeName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var parentChildren = await GetByParentIdAsync(parentId, true);

                return new BaseStatistics
                {
                    TotalCount = parentChildren.Count,
                    ActiveCount = parentChildren.Count(c => c.IsActive),
                    InactiveCount = parentChildren.Count(c => !c.IsActive),
                    SystemDefaultCount = parentChildren.Count(c => c.IsSystemDefault),
                    UserCreatedCount = parentChildren.Count(c => !c.IsSystemDefault),
                    LastRefreshTime = DateTime.UtcNow
                };
            }, $"{ParentEntityTypeName} Statistics");

            return result.Success && result.Data != null ? result.Data : new BaseStatistics();
        }
    }

    /// <summary>
    /// Gets hierarchical statistics including parent distribution
    /// </summary>
    public virtual async Task<HierarchicalStatistics> GetHierarchicalStatisticsAsync()
    {
        using (this.LogPerformance($"Get {EntityTypeName} Hierarchical Statistics"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var children = await GetAllAsync(true);
                var parentGroups = children.GroupBy(c => c.GetParentId()).ToList();
                var parents = await _parentRepository.GetAllAsync(true);

                var mostPopulousGroup = parentGroups
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();

                var mostPopulousParent = mostPopulousGroup != null
                    ? parents.FirstOrDefault(p => p.Id == mostPopulousGroup.Key)
                    : null;

                var parentDistribution = new Dictionary<string, int>();
                foreach (var group in parentGroups)
                {
                    var parent = parents.FirstOrDefault(p => p.Id == group.Key);
                    if (parent != null)
                    {
                        parentDistribution[parent.Name] = group.Count();
                    }
                }

                var emptyParentsCount = parents.Count(p =>
                    !parentGroups.Any(g => g.Key == p.Id));

                return new HierarchicalStatistics
                {
                    TotalCount = children.Count,
                    ActiveCount = children.Count(c => c.IsActive),
                    InactiveCount = children.Count(c => !c.IsActive),
                    SystemDefaultCount = children.Count(c => c.IsSystemDefault),
                    UserCreatedCount = children.Count(c => !c.IsSystemDefault),
                    LastRefreshTime = DateTime.UtcNow,
                    UniqueParentsCount = parentGroups.Count,
                    AverageChildrenPerParent = parentGroups.Count > 0 ? (double)children.Count / parentGroups.Count : 0,
                    MostPopulousParent = mostPopulousParent?.Name,
                    MostPopulousParentCount = mostPopulousGroup?.Count() ?? 0,
                    OrphanedChildrenCount = 0, // Would need additional logic to detect orphaned children
                    ParentDistribution = parentDistribution,
                    EmptyParentsCount = emptyParentsCount,
                    MaxHierarchyDepth = 2 // Can be calculated based on hierarchy depth
                };
            }, "Hierarchical Statistics");

            return result.Success && result.Data != null ? result.Data : new HierarchicalStatistics();
        }
    }

    #endregion

    #region Validation Overrides

    /// <summary>
    /// Override create validation to ensure parent relationship is valid
    /// </summary>
    public override async Task<TChild?> CreateAsync(TChild entity)
    {
        // Validate parent exists before creating child
        var parentExists = await ValidateParentAccessAsync(entity.GetParentId());
        if (!parentExists)
        {
            throw new ArgumentException($"{ParentEntityTypeName} with ID {entity.GetParentId()} not found");
        }

        // Validate hierarchy structure
        if (!entity.ValidateHierarchy())
        {
            throw new ArgumentException($"Invalid hierarchical relationship for {EntityTypeName}");
        }

        return await base.CreateAsync(entity);
    }

    /// <summary>
    /// Override update validation to ensure parent relationship remains valid
    /// </summary>
    public override async Task<TChild?> UpdateAsync(TChild entity)
    {
        // Validate parent exists before updating child
        var parentExists = await ValidateParentAccessAsync(entity.GetParentId());
        if (!parentExists)
        {
            throw new ArgumentException($"{ParentEntityTypeName} with ID {entity.GetParentId()} not found");
        }

        // Validate hierarchy structure
        if (!entity.ValidateHierarchy())
        {
            throw new ArgumentException($"Invalid hierarchical relationship for {EntityTypeName}");
        }

        return await base.UpdateAsync(entity);
    }

    #endregion
}