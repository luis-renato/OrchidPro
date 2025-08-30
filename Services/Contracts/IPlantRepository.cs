// Services/Contracts/IPlantRepository.cs
using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Models.Statistics;

namespace OrchidPro.Services.Contracts;

public interface IPlantRepository : IBaseRepository<Plant>
{
    Task<IEnumerable<Plant>> GetPlantsWithHealthIssuesAsync();
    Task<PlantStatistics> GetPlantStatisticsAsync();
    Task<IEnumerable<Plant>> GetPlantsNeedingCareAsync();
    Task<IEnumerable<Plant>> GetByHealthStatusAsync(string healthStatus);
    Task<IEnumerable<Plant>> GetBloomingPlantsAsync();
    Task<bool> IsPlantCodeUniqueAsync(string plantCode, Guid? excludeId = null);
}