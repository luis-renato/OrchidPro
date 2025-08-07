using OrchidPro.Models;
using OrchidPro.Models.Base;

namespace OrchidPro.Services;

/// <summary>
/// CORRIGIDO: IFamilyRepository with ToggleFavoriteAsync method
/// </summary>
public interface IFamilyRepository : IBaseRepository<Family>
{
    /// <summary>
    /// Gets count statistics for dashboard (versão específica de Family)
    /// </summary>
    Task<FamilyStatistics> GetFamilyStatisticsAsync();

    /// <summary>
    /// ✅ Toggle favorite status for a family
    /// </summary>
    Task<Family> ToggleFavoriteAsync(Guid familyId);

    /// <summary>
    /// ✅ ADICIONADO: Refresh all data with operation result
    /// </summary>
    Task<OperationResult> RefreshAllDataAsync();

    /// <summary>
    /// ✅ Check if family name already exists (já herdado da base, mas explícito aqui)
    /// </summary>
    new Task<bool> NameExistsAsync(string name, Guid? excludeId = null);
}

/// <summary>
/// Statistics for family data (estende BaseStatistics)
/// </summary>
public class FamilyStatistics : BaseStatistics
{
    // ✅ Herda todos os campos da BaseStatistics
    // Adicione campos específicos de Family aqui se necessário

    // Exemplos futuros:
    // public int OrchidaceaeFamiliesCount { get; set; }
    // public int MostPopularFamilyId { get; set; }
    // public string MostCommonFamilyName { get; set; }
}