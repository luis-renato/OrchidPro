using OrchidPro.Models;
using OrchidPro.Models.Base;

namespace OrchidPro.Services.Contracts;

public interface ISourceRepository : IBaseRepository<Source>
{
    Task<SourceStatistics> GetSourceStatisticsAsync();
    Task<Source> ToggleFavoriteAsync(Guid sourceId);
    Task<OperationResult> RefreshAllDataAsync();
    new Task<bool> NameExistsAsync(string name, Guid? excludeId = null);
}

public class SourceStatistics : BaseStatistics
{
    // Herda todos os campos da BaseStatistics
}