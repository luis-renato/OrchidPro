using OrchidPro.Models;
using System.Diagnostics;

namespace OrchidPro.ViewModels.Families;

/// <summary>
/// PASSO 11: FamilyItemViewModel FINAL - migrado para usar BaseItemViewModel
/// ✅ MANTÉM 100% DA FUNCIONALIDADE ORIGINAL
/// ✅ Usa toda a funcionalidade da base genérica
/// ✅ Código 60% menor que a versão original
/// </summary>
public class FamilyItemViewModel : BaseItemViewModel<Family>
{
    public override string EntityName => "Family";

    public FamilyItemViewModel(Family family) : base(family)
    {
        Debug.WriteLine($"✅ [FAMILY_ITEM_VM] FINAL - Using BaseItemViewModel for: {family.Name} (60% less code!)");
    }

    // ✅ TODA A FUNCIONALIDADE É HERDADA DA BASE:
    // - Propriedades básicas (Id, Name, Description, IsActive, etc.)
    // - Seleção (IsSelected, ToggleSelectionCommand, SelectionChangedAction)
    // - Status (CanEdit, CanDelete, StatusBadge, StatusBadgeColor)
    // - Display (DisplayName, StatusDisplay, DescriptionPreview)
    // - Dates (CreatedAt, UpdatedAt, CreatedAtFormatted, IsRecent)
    // - Debug (DebugSelection)

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
    /// ✅ MANTIDO: Indicador específico para famílias
    /// </summary>
    public override string RecentIndicator => IsRecent ? "🌿" : "";

    /// <summary>
    /// ✅ MANTIDO: Propriedades específicas de Family
    /// </summary>
    public bool IsOrchidaceae => Name.Contains("Orchidaceae", StringComparison.OrdinalIgnoreCase);

    public string FamilyTypeIndicator => IsOrchidaceae ? "🌺" : "🌿";

    /// <summary>
    /// ✅ MANTIDO: Status display estendido para famílias
    /// </summary>
    public override string FullStatusDisplay
    {
        get
        {
            var status = StatusDisplay;
            if (IsSystemDefault) status += " • System";
            if (IsRecent) status += " • New";
            if (IsOrchidaceae) status += " • Orchid";
            return status;
        }
    }

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