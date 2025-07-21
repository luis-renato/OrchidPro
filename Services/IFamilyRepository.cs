using OrchidPro.Models;
using OrchidPro.Models.Base;

namespace OrchidPro.Services;

/// <summary>
/// ✅ CORRIGIDO: IFamilyRepository com método ToggleFavoriteAsync
/// </summary>
public interface IFamilyRepository : IBaseRepository<Family>
{
    /// <summary>
    /// Gets count statistics for dashboard (versão específica de Family)
    /// </summary>
    Task<FamilyStatistics> GetFamilyStatisticsAsync();

    /// <summary>
    /// ✅ NOVO: Toggle favorite status for a family
    /// </summary>
    Task<Family> ToggleFavoriteAsync(Guid familyId);

    /// <summary>
    /// ✅ NOVO: Check if family name already exists
    /// </summary>
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null);

    // ✅ TODOS os outros métodos são herdados da IBaseRepository<Family>
    // Não precisamos redeclará-los com 'new' porque não há conflito
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