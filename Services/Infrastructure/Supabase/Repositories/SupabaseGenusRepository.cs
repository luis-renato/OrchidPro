using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Services.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Infrastructure.Supabase.Models;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;

namespace OrchidPro.Services.Infrastructure.Supabase.Repositories;

public class SupabaseGenusRepository(SupabaseService supabaseService, IFamilyRepository familyRepository)
    : BaseHierarchicalRepository<Genus, Family>(supabaseService, familyRepository), IGenusRepository
{
    private readonly BaseSupabaseEntityService<Genus, SupabaseGenus> _supabaseEntityService = new InternalSupabaseGenusService(supabaseService);

    protected override string EntityTypeName => "Genus";
    protected override string ParentEntityTypeName => "Family";

    protected override async Task<IEnumerable<Genus>> GetAllFromServiceAsync()
    {
        var rawGenera = await _supabaseEntityService.GetAllAsync();
        var generaWithFamily = await PopulateParentDataAsync([.. rawGenera]);
        this.LogInfo($"Loaded {generaWithFamily.Count} genera WITH family data immediately");
        return generaWithFamily;
    }

    protected override async Task<Genus?> GetByIdFromServiceAsync(Guid id)
    {
        var allGenera = await GetAllFromServiceAsync();
        return allGenera.FirstOrDefault(g => g.Id == id);
    }

    protected override async Task<Genus?> CreateInServiceAsync(Genus entity)
        => await _supabaseEntityService.CreateAsync(entity);

    protected override async Task<Genus?> UpdateInServiceAsync(Genus entity)
        => await _supabaseEntityService.UpdateAsync(entity);

    protected override async Task<bool> DeleteInServiceAsync(Guid id)
        => await _supabaseEntityService.DeleteAsync(id);

    protected override async Task<bool> NameExistsInServiceAsync(string name, Guid? excludeId)
    {
        var allGenera = await GetAllFromServiceAsync();
        return allGenera.Any(g =>
            string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase) && g.Id != excludeId);
    }

    // IGenusRepository implementations
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
}

internal class InternalSupabaseGenusService(SupabaseService supabaseService)
    : BaseSupabaseEntityService<Genus, SupabaseGenus>(supabaseService)
{
    protected override string EntityTypeName => "Genus";
    protected override string EntityPluralName => "Genera";

    protected override Genus ConvertToEntity(SupabaseGenus supabaseModel)
        => supabaseModel.ToGenus();

    protected override SupabaseGenus ConvertFromEntity(Genus entity)
        => SupabaseGenus.FromGenus(entity);
}