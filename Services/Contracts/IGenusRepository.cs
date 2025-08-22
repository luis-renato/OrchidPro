using OrchidPro.Models;
using OrchidPro.Models.Base;

namespace OrchidPro.Services.Contracts;

public interface IGenusRepository : IBaseRepository<Genus>
{
    #region Family-Specific Queries
    Task<List<Genus>> GetByFamilyIdAsync(Guid familyId, bool includeInactive = false);
    Task<List<Genus>> GetFilteredByFamilyAsync(Guid familyId, string? searchText = null, bool? statusFilter = null);
    Task<int> GetCountByFamilyAsync(Guid familyId, bool includeInactive = false);
    #endregion

    #region Validation and Business Logic
    Task<bool> NameExistsInFamilyAsync(string name, Guid familyId, Guid? excludeId = null);
    Task<bool> ValidateFamilyAccessAsync(Guid familyId);
    #endregion

    #region Hierarchical Operations
    Task<List<Genus>> PopulateFamilyDataAsync(List<Genus> genera);
    Task<List<Genus>> GetAllWithFamilyAsync(bool includeInactive = false);
    Task<List<Genus>> GetFilteredWithFamilyAsync(string? searchText = null, bool? statusFilter = null, Guid? familyId = null);
    #endregion

    #region Statistics and Analytics
    Task<GenusStatistics> GetGenusStatisticsAsync();
    Task<BaseStatistics> GetStatisticsByFamilyAsync(Guid familyId);
    #endregion

    #region Bulk Operations
    Task<int> DeleteByFamilyAsync(Guid familyId);
    Task<int> BulkUpdateFamilyAsync(List<Guid> genusIds, Guid newFamilyId);
    #endregion
}

public class GenusStatistics : BaseStatistics
{
    public int UniqueFamiliesCount { get; set; }
    public double AverageGeneraPerFamily { get; set; }
    public string? MostPopulousFamily { get; set; }
    public int MostPopulousFamilyCount { get; set; }
    public int OrphanedGeneraCount { get; set; }
    public Dictionary<string, int> FamilyDistribution { get; set; } = [];
}