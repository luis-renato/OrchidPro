using OrchidPro.Models;
using OrchidPro.Models.Base;

namespace OrchidPro.Services.Contracts;

public interface ILocationRepository : IBaseRepository<PlantLocation>
{
    Task<LocationStatistics> GetLocationStatisticsAsync();
    Task<PlantLocation> ToggleFavoriteAsync(Guid locationId);
    Task<OperationResult> RefreshAllDataAsync();
    new Task<bool> NameExistsAsync(string name, Guid? excludeId = null);
}

public class LocationStatistics : BaseStatistics
{
    // Herda todos os campos da BaseStatistics
}