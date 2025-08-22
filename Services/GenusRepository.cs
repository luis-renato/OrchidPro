using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Services.Base;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;

namespace OrchidPro.Services;

/// <summary>
/// ULTRA MINIMAL Genus repository - just the absolute essentials!
/// BaseHierarchicalRepository handles ALL operations - this only provides service connection.
/// CRITICAL: Always loads with Family data for immediate display (no "Unknown Family" delays).
/// </summary>
public class GenusRepository : BaseHierarchicalRepository<Genus, Family>, IGenusRepository
{
    #region Private Fields

    private readonly SupabaseGenusService _genusService;

    #endregion

    #region Required Base Implementation

    protected override string EntityTypeName => "Genus";
    protected override string ParentEntityTypeName => "Family";

    /// <summary>
    /// CRITICAL: Always load with Family data immediately (like Species with Genus)
    /// Eliminates "Unknown Family" delays by populating parent data upfront
    /// </summary>
    protected override async Task<IEnumerable<Genus>> GetAllFromServiceAsync()
    {
        var rawGenera = await _genusService.GetAllAsync();
        var generaWithFamily = await PopulateParentDataAsync([.. rawGenera]);
        this.LogInfo($"✅ Loaded {generaWithFamily.Count} genera WITH family data immediately");
        return generaWithFamily;
    }

    protected override async Task<Genus?> GetByIdFromServiceAsync(Guid id)
    {
        var allGenera = await GetAllFromServiceAsync();
        return allGenera.FirstOrDefault(g => g.Id == id);
    }

    protected override async Task<Genus?> CreateInServiceAsync(Genus entity)
        => await _genusService.CreateAsync(entity);

    protected override async Task<Genus?> UpdateInServiceAsync(Genus entity)
        => await _genusService.UpdateAsync(entity);

    protected override async Task<bool> DeleteInServiceAsync(Guid id)
        => await _genusService.DeleteAsync(id);

    protected override async Task<bool> NameExistsInServiceAsync(string name, Guid? excludeId)
    {
        var allGenera = await GetAllFromServiceAsync();
        return allGenera.Any(g =>
            string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase) && g.Id != excludeId);
    }

    #endregion

    #region Constructor

    public GenusRepository(
        SupabaseService supabaseService,
        SupabaseGenusService genusService,
        IFamilyRepository familyRepository)
        : base(supabaseService, familyRepository)
    {
        _genusService = genusService ?? throw new ArgumentNullException(nameof(genusService));
        this.LogInfo("ULTRA MINIMAL GenusRepository - immediate family loading enabled!");
    }

    #endregion

    #region IGenusRepository - Required Interface Methods

    /// <summary>
    /// Get genera by family - uses base GetByParentIdAsync
    /// </summary>
    public async Task<List<Genus>> GetByFamilyIdAsync(Guid familyId, bool includeInactive = false)
        => await GetByParentIdAsync(familyId, includeInactive);

    /// <summary>
    /// Get filtered genera by family - uses base GetFilteredByParentAsync
    /// </summary>
    public async Task<List<Genus>> GetFilteredByFamilyAsync(Guid familyId, string? searchText = null, bool? statusFilter = null)
        => await GetFilteredByParentAsync(familyId, searchText, statusFilter);

    /// <summary>
    /// Get count by family - uses base GetCountByParentAsync
    /// </summary>
    public async Task<int> GetCountByFamilyAsync(Guid familyId, bool includeInactive = false)
        => await GetCountByParentAsync(familyId, includeInactive);

    /// <summary>
    /// Check name exists in family - uses base NameExistsInParentAsync
    /// </summary>
    public async Task<bool> NameExistsInFamilyAsync(string name, Guid familyId, Guid? excludeId = null)
        => await NameExistsInParentAsync(name, familyId, excludeId);

    /// <summary>
    /// Validate family access - uses base ValidateParentAccessAsync
    /// </summary>
    public async Task<bool> ValidateFamilyAccessAsync(Guid familyId)
        => await ValidateParentAccessAsync(familyId);

    /// <summary>
    /// Populate family data - uses base PopulateParentDataAsync
    /// </summary>
    public async Task<List<Genus>> PopulateFamilyDataAsync(List<Genus> genera)
        => await PopulateParentDataAsync(genera);

    /// <summary>
    /// Get all genera with family data - uses base GetAllWithParentAsync
    /// </summary>
    public async Task<List<Genus>> GetAllWithFamilyAsync(bool includeInactive = false)
        => await GetAllWithParentAsync(includeInactive);

    /// <summary>
    /// Get filtered with family - uses base GetFilteredWithParentAsync
    /// </summary>
    public async Task<List<Genus>> GetFilteredWithFamilyAsync(string? searchText = null, bool? statusFilter = null, Guid? familyId = null)
        => await GetFilteredWithParentAsync(searchText, statusFilter, familyId);

    /// <summary>
    /// Delete by family - uses base DeleteByParentAsync
    /// </summary>
    public async Task<int> DeleteByFamilyAsync(Guid familyId)
        => await DeleteByParentAsync(familyId);

    /// <summary>
    /// Bulk update family - uses base BulkUpdateParentAsync
    /// </summary>
    public async Task<int> BulkUpdateFamilyAsync(List<Guid> genusIds, Guid newFamilyId)
        => await BulkUpdateParentAsync(genusIds, newFamilyId);

    /// <summary>
    /// Get statistics by family - uses base GetStatisticsByParentAsync
    /// </summary>
    public async Task<BaseStatistics> GetStatisticsByFamilyAsync(Guid familyId)
        => await GetStatisticsByParentAsync(familyId);

    /// <summary>
    /// Get genus statistics
    /// </summary>
    public async Task<GenusStatistics> GetGenusStatisticsAsync()
    {
        var hierarchicalStats = await GetHierarchicalStatisticsAsync();
        return new GenusStatistics
        {
            TotalCount = hierarchicalStats.TotalCount,
            ActiveCount = hierarchicalStats.ActiveCount,
            InactiveCount = hierarchicalStats.InactiveCount,
            SystemDefaultCount = hierarchicalStats.SystemDefaultCount,
            UserCreatedCount = hierarchicalStats.UserCreatedCount,
            LastRefreshTime = hierarchicalStats.LastRefreshTime,
            UniqueFamiliesCount = hierarchicalStats.UniqueParentsCount,
            AverageGeneraPerFamily = hierarchicalStats.AverageChildrenPerParent,
            MostPopulousFamily = hierarchicalStats.MostPopulousParent,
            MostPopulousFamilyCount = hierarchicalStats.MostPopulousParentCount,
            OrphanedGeneraCount = hierarchicalStats.OrphanedChildrenCount,
            FamilyDistribution = hierarchicalStats.ParentDistribution
        };
    }

    #endregion
}