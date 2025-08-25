using OrchidPro.Models;
using OrchidPro.Models.Base;

namespace OrchidPro.Services.Contracts;

public interface IMountRepository : IBaseRepository<Mount>
{
    Task<MountStatistics> GetMountStatisticsAsync();
    Task<Mount> ToggleFavoriteAsync(Guid MountId);
    Task<OperationResult> RefreshAllDataAsync();
    new Task<bool> NameExistsAsync(string name, Guid? excludeId = null);
}

public class MountStatistics : BaseStatistics
{
    public int UniqueMaterialsCount { get; set; }
    public Dictionary<string, int> MaterialDistribution { get; set; } = [];
    public Dictionary<string, int> SizeDistribution { get; set; } = [];
}