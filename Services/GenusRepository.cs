using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Services.Base;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;

namespace OrchidPro.Services;

/// <summary>
/// MINIMAL Genus repository - reduced from ~500 lines to just the essentials!
/// BaseHierarchicalRepository handles ALL the heavy lifting - this just provides service connection and aliases.
/// </summary>
public class GenusRepository : BaseHierarchicalRepository<Genus, Family>, IGenusRepository
{
    #region Private Fields

    private readonly SupabaseGenusService _genusService;

    #endregion

    #region Required Base Implementation

    protected override string EntityTypeName => "Genus";
    protected override string ParentEntityTypeName => "Family";

    protected override async Task<IEnumerable<Genus>> GetAllFromServiceAsync()
        => await _genusService.GetAllAsync();

    protected override async Task<Genus?> GetByIdFromServiceAsync(Guid id)
    {
        // SupabaseGenusService doesn't have GetByIdAsync - use base implementation
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
        // SupabaseGenusService doesn't have NameExistsAsync - use base implementation
        var allGenera = await GetAllFromServiceAsync();
        return allGenera.Any(g =>
            string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase) &&
            g.Id != excludeId);
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
        this.LogInfo("GenusRepository initialized - BaseHierarchicalRepository handles everything!");
    }

    #endregion

    #region IGenusRepository Aliases - Just Forward to Base Methods

    public async Task<List<Genus>> GetByFamilyIdAsync(Guid familyId, bool includeInactive = false)
        => await GetByParentIdAsync(familyId, includeInactive);

    public async Task<List<Genus>> GetFilteredByFamilyAsync(Guid familyId, string? searchText = null, bool? statusFilter = null)
        => await GetFilteredByParentAsync(familyId, searchText, statusFilter);

    public async Task<int> GetCountByFamilyAsync(Guid familyId, bool includeInactive = false)
        => await GetCountByParentAsync(familyId, includeInactive);

    public async Task<bool> NameExistsInFamilyAsync(string name, Guid familyId, Guid? excludeId = null)
        => await NameExistsInParentAsync(name, familyId, excludeId);

    public async Task<bool> ValidateFamilyAccessAsync(Guid familyId)
        => await ValidateParentAccessAsync(familyId);

    public async Task<List<Genus>> PopulateFamilyDataAsync(List<Genus> genera)
        => await PopulateParentDataAsync(genera);

    public async Task<List<Genus>> GetAllWithFamilyAsync(bool includeInactive = false)
        => await GetAllWithParentAsync(includeInactive);

    public async Task<List<Genus>> GetFilteredWithFamilyAsync(string? searchText = null, bool? statusFilter = null, Guid? familyId = null)
        => await GetFilteredWithParentAsync(searchText, statusFilter, familyId);

    public async Task<int> DeleteByFamilyAsync(Guid familyId)
        => await DeleteByParentAsync(familyId);

    public async Task<int> BulkUpdateFamilyAsync(List<Guid> genusIds, Guid newFamilyId)
        => await BulkUpdateParentAsync(genusIds, newFamilyId);

    public async Task<BaseStatistics> GetStatisticsByFamilyAsync(Guid familyId)
        => await GetStatisticsByParentAsync(familyId);

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

// NOTE: GenusStatistics is already defined in IGenusRepository.cs