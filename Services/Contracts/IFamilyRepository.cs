using OrchidPro.Models;
using OrchidPro.Models.Base;

namespace OrchidPro.Services.Contracts;

public interface IFamilyRepository : IBaseRepository<Family>
{
    Task<FamilyStatistics> GetFamilyStatisticsAsync();
    Task<Family> ToggleFavoriteAsync(Guid familyId);
    Task<OperationResult> RefreshAllDataAsync();
    new Task<bool> NameExistsAsync(string name, Guid? excludeId = null);
}

public class FamilyStatistics : BaseStatistics
{
    // Herda todos os campos da BaseStatistics
}