using OrchidPro.Models;
using OrchidPro.ViewModels.Base;
using System.Diagnostics;

namespace OrchidPro.ViewModels.Families;

/// <summary>
/// ✅ CORRIGIDO: FamilyItemViewModel com suporte a favoritos sem erros de override
/// </summary>
public class FamilyItemViewModel : BaseItemViewModel<Family>
{
    public override string EntityName => "Family";

    /// <summary>
    /// ✅ NOVO: Propriedade IsFavorite
    /// </summary>
    public bool IsFavorite { get; }

    public FamilyItemViewModel(Family family) : base(family)
    {
        IsFavorite = family.IsFavorite; // ✅ NOVO: Capturar favorito do modelo
        Debug.WriteLine($"✅ [FAMILY_ITEM_VM] Created: {family.Name} (Favorite: {IsFavorite})");
    }

    // ✅ CUSTOMIZAÇÕES ESPECÍFICAS DE FAMILY:

    /// <summary>
    /// ✅ MANTIDO: Preview personalizado para famílias botânicas
    /// </summary>
    public override string DescriptionPreview
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Description))
                return "No botanical description available";

            return Description.Length > 120
                ? $"{Description.Substring(0, 117)}..."
                : Description;
        }
    }

    /// <summary>
    /// ✅ ATUALIZADO: Indicador específico para famílias com favorito
    /// </summary>
    public override string RecentIndicator => IsRecent ? "🌿" : (IsFavorite ? "⭐" : "");

    /// <summary>
    /// ✅ MANTIDO: Propriedades específicas de Family
    /// </summary>
    public bool IsOrchidaceae => Name.Contains("Orchidaceae", StringComparison.OrdinalIgnoreCase);

    public string FamilyTypeIndicator => IsOrchidaceae ? "🌺" : "🌿";

    /// <summary>
    /// ✅ ATUALIZADO: Status display estendido para famílias com favorito
    /// </summary>
    public override string FullStatusDisplay
    {
        get
        {
            var status = StatusDisplay;
            if (IsSystemDefault) status += " • System";
            if (IsRecent) status += " • New";
            if (IsOrchidaceae) status += " • Orchid";
            if (IsFavorite) status += " • Favorite"; // ✅ NOVO: Indicador de favorito
            return status;
        }
    }

    /// <summary>
    /// ✅ NOVO: Cor do badge considerando favorito
    /// </summary>
    public override Color StatusBadgeColor
    {
        get
        {
            if (IsFavorite)
                return Color.FromArgb("#FF9800"); // Cor especial para favoritos

            return base.StatusBadgeColor;
        }
    }

    /// <summary>
    /// ✅ CORRIGIDO: Usar new ao invés de override para propriedade não virtual
    /// </summary>
    public new string DisplayName => $"{Name}{(IsSystemDefault ? " (System)" : "")}{(IsFavorite ? " ⭐" : "")}";

    // ✅ COMPATIBILIDADE: Método para obter o modelo (mantido para não quebrar código existente)
    public new Family ToModel() => base.ToModel();

    // ✅ TODA A FUNCIONALIDADE ORIGINAL MANTIDA:
    // ✅ Seleção com checkbox binding
    // ✅ Command para toggle de seleção  
    // ✅ Action para notificar mudanças de seleção
    // ✅ Status badges com cores
    // ✅ Indicadores visuais (recent, system, etc.)
    // ✅ Preview de descrição truncada
    // ✅ Formatação de datas
    // ✅ Propriedades para UI binding
    // ✅ Debug e diagnóstico
}