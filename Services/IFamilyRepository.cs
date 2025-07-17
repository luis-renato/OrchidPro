using OrchidPro.Models;
using OrchidPro.Models.Base;

namespace OrchidPro.Services;

/// <summary>
/// PASSO 3.1: IFamilyRepository corrigido - implementa IBaseRepository sem conflitos
/// Mantém TODAS as assinaturas específicas + compatibilidade com interface base
/// </summary>
public interface IFamilyRepository : IBaseRepository<Family>
{
    // ✅ MANTEMOS apenas assinaturas que diferem da base ou são específicas

    /// <summary>
    /// Gets count statistics for dashboard (versão específica de Family)
    /// </summary>
    Task<FamilyStatistics> GetFamilyStatisticsAsync();

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