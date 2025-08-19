using OrchidPro.Models.Base;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;

namespace OrchidPro.Services.Base;

/// <summary>
/// Performance optimized base implementation for hierarchical repositories managing parent-child relationships.
/// Provides comprehensive operations for hierarchical data structures with intelligent caching and parallel processing.
/// Eliminates massive code duplication across Family→Genus→Species and future hierarchical entities.
/// </summary>
/// <typeparam name="TChild">Child entity type</typeparam>
/// <typeparam name="TParent">Parent entity type</typeparam>
public abstract class BaseHierarchicalRepository<TChild, TParent> : BaseRepository<TChild>, IHierarchicalRepository<TChild, TParent>
    where TChild : class, IBaseEntity, IHierarchicalEntity<TParent>, new()
    where TParent : class, IBaseEntity
{
    #region Performance Optimization Constants

    /// <summary>
    /// Threshold for using parallel processing - below this use sequential processing
    /// </summary>
    private const int PARALLEL_THRESHOLD = 50;

    #endregion

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
        this.LogInfo($"🚀 OPTIMIZED {EntityTypeName}Repository initialized with {ParentEntityTypeName} hierarchy support and parallel processing");
    }

    #endregion

    #region IHierarchicalRepository Implementation - Performance Optimized

    /// <summary>
    /// Gets all child entities belonging to a specific parent with smart threshold
    /// </summary>
    public virtual async Task<List<TChild>> GetByParentIdAsync(Guid parentId, bool includeInactive = false)
    {
        using (this.LogPerformance($"Get {EntityTypeName} by {ParentEntityTypeName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var allChildren = await GetAllAsync(includeInactive);

                // Smart threshold to prevent unnecessary parallel processing
                return allChildren.Count > PARALLEL_THRESHOLD
                    ? await Task.Run(() =>
                        allChildren.AsParallel()
                            .WithDegreeOfParallelism(Math.Min(Environment.ProcessorCount, Math.Max(1, allChildren.Count / 20)))
                            .Where(c => c.GetParentId() == parentId)
                            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                            .ToList()
                    )
                    : [.. allChildren
                        .Where(c => c.GetParentId() == parentId)
                        .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)];
            }, $"{EntityTypeName} by {ParentEntityTypeName}");

            return result.Success && result.Data != null ? result.Data : [];
        }
    }

    /// <summary>
    /// Gets filtered child entities by parent with smart threshold
    /// </summary>
    public virtual async Task<List<TChild>> GetFilteredByParentAsync(Guid parentId, string? searchText = null, bool? statusFilter = null)
    {
        using (this.LogPerformance($"Get Filtered {EntityTypeName} by {ParentEntityTypeName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var parentChildren = await GetByParentIdAsync(parentId, statusFilter != true);

                // Smart threshold for filtering operations
                if (parentChildren.Count <= PARALLEL_THRESHOLD)
                {
                    // Sequential processing for small datasets
                    var query = parentChildren.AsEnumerable();

                    if (!string.IsNullOrEmpty(searchText))
                    {
                        query = query.Where(c => c.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
                    }

                    if (statusFilter.HasValue)
                    {
                        query = query.Where(c => c.IsActive == statusFilter.Value);
                    }

                    return [.. query.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)];
                }
                else
                {
                    // Parallel processing only for large datasets
                    return await Task.Run(() =>
                    {
                        var query = parentChildren.AsParallel()
                            .WithDegreeOfParallelism(Math.Min(Environment.ProcessorCount, Math.Max(1, parentChildren.Count / 20)));

                        if (!string.IsNullOrEmpty(searchText))
                        {
                            query = ApplyTextSearchParallel(query, searchText);
                        }

                        if (statusFilter.HasValue)
                        {
                            query = query.Where(c => c.IsActive == statusFilter.Value);
                        }

                        return query.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
                    });
                }
            }, $"Filtered {EntityTypeName} by {ParentEntityTypeName}");

            return result.Success && result.Data != null ? result.Data : [];
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

    #region Hierarchical Validation - Performance Optimized

    /// <summary>
    /// Checks if child entity name exists within a specific parent scope with smart threshold
    /// </summary>
    public virtual async Task<bool> NameExistsInParentAsync(string name, Guid parentId, Guid? excludeId = null)
    {
        using (this.LogPerformance($"Check Name Exists in {ParentEntityTypeName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var parentChildren = await GetByParentIdAsync(parentId, true);

                // Smart threshold for name checking
                var exists = parentChildren.Count > PARALLEL_THRESHOLD
                    ? await Task.Run(() =>
                        parentChildren.AsParallel()
                            .WithDegreeOfParallelism(Math.Min(Environment.ProcessorCount, Math.Max(1, parentChildren.Count / 20)))
                            .Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase) && c.Id != excludeId)
                    )
                    : parentChildren.Any(c =>
                        string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase) && c.Id != excludeId);

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

    #region Hierarchical Operations - Performance Optimized

    /// <summary>
    /// Gets child entities with their parent information populated using parallel processing
    /// </summary>
    public virtual async Task<List<TChild>> PopulateParentDataAsync(List<TChild> children)
    {
        if (children.Count == 0) return children;

        using (this.LogPerformance($"Populate {ParentEntityTypeName} Data"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                // Get unique parent IDs and batch load parents
                var parentIds = children.AsParallel()
                    .Select(c => c.GetParentId())
                    .Distinct()
                    .ToList();

                // Parallel parent loading
                var parentTasks = parentIds.Select(async parentId =>
                {
                    var parent = await _parentRepository.GetByIdAsync(parentId);
                    return new { ParentId = parentId, Parent = parent };
                });

                var parentResults = await Task.WhenAll(parentTasks);
                var parents = parentResults
                    .Where(pr => pr.Parent != null)
                    .ToDictionary(pr => pr.ParentId, pr => pr.Parent!);

                // Parallel assignment of parent data
                await Task.Run(() =>
                {
                    Parallel.ForEach(children, child =>
                    {
                        if (parents.TryGetValue(child.GetParentId(), out var parent))
                        {
                            child.Parent = parent;
                        }
                    });
                });

                return children;
            }, $"Populate {ParentEntityTypeName} Data");

            return result.Success && result.Data != null ? result.Data : children;
        }
    }

    /// <summary>
    /// Gets all child entities with their parent information in a single optimized query
    /// </summary>
    public virtual async Task<List<TChild>> GetAllWithParentAsync(bool includeInactive = false)
    {
        using (this.LogPerformance($"Get All {EntityTypeName} with {ParentEntityTypeName}"))
        {
            var children = await GetAllAsync(includeInactive);
            return await PopulateParentDataAsync(children);
        }
    }

    /// <summary>
    /// Gets filtered child entities with parent information using parallel processing
    /// </summary>
    public virtual async Task<List<TChild>> GetFilteredWithParentAsync(string? searchText = null, bool? statusFilter = null, Guid? parentId = null)
    {
        using (this.LogPerformance($"Get Filtered {EntityTypeName} with {ParentEntityTypeName}"))
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
    }

    #endregion

    #region Bulk Hierarchical Operations - Performance Optimized

    /// <summary>
    /// Deletes all child entities belonging to a parent (cascade operation) with parallel processing
    /// </summary>
    public virtual async Task<int> DeleteByParentAsync(Guid parentId)
    {
        using (this.LogPerformance($"Delete {EntityTypeName} by {ParentEntityTypeName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var parentChildren = await GetByParentIdAsync(parentId, true);
                if (parentChildren.Count > 0)
                {
                    var ids = parentChildren.Select(c => c.Id).ToList();
                    var deletedCount = await DeleteMultipleAsync(ids);
                    this.LogDataOperation("Cascade deleted", EntityTypeName, $"{deletedCount} items from {ParentEntityTypeName}");
                    return deletedCount;
                }
                return 0;
            }, $"Delete by {ParentEntityTypeName}");

            return result.Success ? result.Data : 0;
        }
    }

    /// <summary>
    /// Bulk updates parent assignment for multiple child entities with parallel processing
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

                // Parallel updates
                var updateTasks = childIds.Select(async childId =>
                {
                    var child = await GetByIdAsync(childId);
                    if (child != null)
                    {
                        child.SetParentId(newParentId);
                        var updated = await UpdateAsync(child);
                        return updated != null;
                    }
                    return false;
                });

                var results = await Task.WhenAll(updateTasks);
                var updatedCount = results.Count(success => success);

                this.LogDataOperation("Bulk updated parent", EntityTypeName, $"{updatedCount} items");
                return updatedCount;
            }, $"Bulk Update {ParentEntityTypeName}");

            return result.Success ? result.Data : 0;
        }
    }

    #endregion

    #region Hierarchical Statistics - Performance Optimized

    /// <summary>
    /// Gets statistics for child entities within a specific parent using parallel aggregation
    /// </summary>
    public virtual async Task<BaseStatistics> GetStatisticsByParentAsync(Guid parentId)
    {
        using (this.LogPerformance($"Get {EntityTypeName} Statistics by {ParentEntityTypeName}"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                var parentChildren = await GetByParentIdAsync(parentId, true);

                // Parallel statistics calculation
                return await Task.Run(() =>
                {
                    var query = parentChildren.AsParallel();
                    return new BaseStatistics
                    {
                        TotalCount = parentChildren.Count,
                        ActiveCount = query.Count(c => c.IsActive),
                        InactiveCount = query.Count(c => !c.IsActive),
                        SystemDefaultCount = query.Count(c => c.IsSystemDefault),
                        UserCreatedCount = query.Count(c => !c.IsSystemDefault),
                        LastRefreshTime = DateTime.UtcNow
                    };
                });
            }, $"{ParentEntityTypeName} Statistics");

            return result.Success && result.Data != null ? result.Data : new BaseStatistics();
        }
    }

    /// <summary>
    /// Gets hierarchical statistics including parent distribution with parallel processing
    /// </summary>
    public virtual async Task<HierarchicalStatistics> GetHierarchicalStatisticsAsync()
    {
        using (this.LogPerformance($"Get {EntityTypeName} Hierarchical Statistics"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                // Parallel loading of children and parents
                var childrenTask = GetAllAsync(true);
                var parentsTask = _parentRepository.GetAllAsync(true);

                await Task.WhenAll(childrenTask, parentsTask);

                var children = await childrenTask;
                var parents = await parentsTask;

                // Parallel statistics calculation
                return await Task.Run(() =>
                {
                    var childrenQuery = children.AsParallel();
                    var parentsQuery = parents.AsParallel();

                    var parentGroups = childrenQuery
                        .GroupBy(c => c.GetParentId())
                        .ToList();

                    var mostPopulousGroup = parentGroups
                        .AsParallel()
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault();

                    var mostPopulousParent = mostPopulousGroup != null
                        ? parentsQuery.FirstOrDefault(p => p.Id == mostPopulousGroup.Key)
                        : null;

                    var parentDistribution = parentGroups
                        .AsParallel()
                        .Select(group =>
                        {
                            var parent = parentsQuery.FirstOrDefault(p => p.Id == group.Key);
                            return parent != null ? new { parent.Name, Count = group.Count() } : null;
                        })
                        .Where(x => x != null)
                        .ToDictionary(x => x!.Name, x => x!.Count);

                    var emptyParentsCount = parentsQuery.Count(p =>
                        !parentGroups.Any(g => g.Key == p.Id));

                    return new HierarchicalStatistics
                    {
                        TotalCount = children.Count,
                        ActiveCount = childrenQuery.Count(c => c.IsActive),
                        InactiveCount = childrenQuery.Count(c => !c.IsActive),
                        SystemDefaultCount = childrenQuery.Count(c => c.IsSystemDefault),
                        UserCreatedCount = childrenQuery.Count(c => !c.IsSystemDefault),
                        LastRefreshTime = DateTime.UtcNow,
                        UniqueParentsCount = parentGroups.Count,
                        AverageChildrenPerParent = parentGroups.Count > 0 ? (double)children.Count / parentGroups.Count : 0,
                        MostPopulousParent = mostPopulousParent?.Name ?? string.Empty,
                        MostPopulousParentCount = mostPopulousGroup?.Count() ?? 0,
                        OrphanedChildrenCount = 0, // Would need additional logic to detect orphaned children
                        ParentDistribution = parentDistribution,
                        EmptyParentsCount = emptyParentsCount,
                        MaxHierarchyDepth = 2 // Can be calculated based on hierarchy depth
                    };
                });
            }, "Hierarchical Statistics");

            return result.Success && result.Data != null ? result.Data : new HierarchicalStatistics();
        }
    }

    #endregion

    #region Validation Overrides - Performance Optimized

    /// <summary>
    /// Override create validation to ensure parent relationship is valid
    /// </summary>
    public override async Task<TChild> CreateAsync(TChild entity)
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
    public override async Task<TChild> UpdateAsync(TChild entity)
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