using OrchidPro.Models;
using OrchidPro.Models.Base;

namespace OrchidPro.Services.Contracts;

public interface ISubstrateRepository : IBaseRepository<Substrate>
{
    Task<SubstrateStatistics> GetSubstrateStatisticsAsync();
    Task<Substrate> ToggleFavoriteAsync(Guid substrateId);
    Task<OperationResult> RefreshAllDataAsync();
    new Task<bool> NameExistsAsync(string name, Guid? excludeId = null);
}

public class SubstrateStatistics : BaseStatistics
{
    public int UniqueSuppliersCount { get; set; }
    public Dictionary<string, int> DrainageLevelDistribution { get; set; } = [];
    public Dictionary<string, int> SupplierDistribution { get; set; } = [];
}