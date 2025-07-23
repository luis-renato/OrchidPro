using OrchidPro.Models;
using OrchidPro.ViewModels;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OrchidPro.ViewModels.Families;

/// <summary>
/// ✅ CORRIGIDO: FamilyItemViewModel que estende o BaseItemViewModel existente
/// - Adiciona apenas funcionalidades específicas de Family
/// - IsSelected já existe na base
/// - IsFavorite específico de Family
/// - Compatível com código existente do GitHub
/// </summary>
public partial class FamilyItemViewModel : BaseItemViewModel<Family>
{
    public override string EntityName => "Family";

    /// <summary>
    /// ✅ Propriedade IsFavorite específica de Family
    /// </summary>
    public bool IsFavorite { get; }

    public FamilyItemViewModel(Family family) : base(family)
    {
        IsFavorite = family.IsFavorite;
        Debug.WriteLine($"✅ [FAMILY_ITEM_VM] Created: {family.Name} (Favorite: {IsFavorite}, ID: {family.Id})");
    }

    // ✅ CUSTOMIZAÇÕES ESPECÍFICAS DE FAMILY:

    /// <summary>
    /// ✅ Preview personalizado para famílias botânicas
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
    /// ✅ Indicador específico para famílias com favorito
    /// </summary>
    public override string RecentIndicator => IsRecent ? "🌿" : (IsFavorite ? "⭐" : "");

    /// <summary>
    /// ✅ Propriedades específicas de Family
    /// </summary>
    public bool IsOrchidaceae => Name.Contains("Orchidaceae", StringComparison.OrdinalIgnoreCase);

    public string FamilyTypeIndicator => IsOrchidaceae ? "🌺" : "🌿";

    /// <summary>
    /// ✅ Status display estendido para famílias com favorito
    /// </summary>
    public override string FullStatusDisplay
    {
        get
        {
            var status = StatusDisplay;
            if (IsSystemDefault) status += " • System";
            if (IsRecent) status += " • New";
            if (IsOrchidaceae) status += " • Orchid";
            if (IsFavorite) status += " • Favorite";
            return status;
        }
    }

    /// <summary>
    /// ✅ Cor do badge considerando favorito
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
    /// ✅ Display name com indicadores visuais
    /// </summary>
    public new string DisplayName => $"{Name}{(IsSystemDefault ? " (System)" : "")}{(IsFavorite ? " ⭐" : "")}";

    /// <summary>
    /// ✅ NOVO: Propriedades para UI binding otimizado
    /// </summary>
    public string SelectionIcon => IsSelected ? "☑️" : "☐";
    public Color SelectionColor => IsSelected ?
        Color.FromArgb("#2196F3") :
        Color.FromArgb("#E0E0E0");

    /// <summary>
    /// ✅ NOVO: Indicador de status visual combinado
    /// </summary>
    public string StatusIcon
    {
        get
        {
            if (!IsActive) return "⏸️";
            if (IsFavorite) return "⭐";
            if (IsOrchidaceae) return "🌺";
            if (IsSystemDefault) return "🔒";
            return "🌿";
        }
    }

    /// <summary>
    /// ✅ NOVO: Tooltip text para informações adicionais
    /// </summary>
    public string TooltipText
    {
        get
        {
            var parts = new List<string>();

            if (IsFavorite) parts.Add("Favorite family");
            if (IsOrchidaceae) parts.Add("Orchid family");
            if (IsSystemDefault) parts.Add("System default");
            if (!IsActive) parts.Add("Inactive");

            parts.Add($"Created {CreatedAt:dd/MM/yyyy}");

            return string.Join(" • ", parts);
        }
    }

    /// <summary>
    /// ✅ Método para obter o modelo atualizado
    /// </summary>
    public new Family ToModel()
    {
        var family = base.ToModel();
        // O IsFavorite já está no modelo base, mas garantimos consistência
        return family;
    }

    /// <summary>
    /// ✅ NOVO: Método para comparação (útil para sorting)
    /// </summary>
    public int CompareTo(FamilyItemViewModel? other)
    {
        if (other == null) return 1;

        // Favoritos primeiro
        if (IsFavorite && !other.IsFavorite) return -1;
        if (!IsFavorite && other.IsFavorite) return 1;

        // Depois por nome
        return string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// ✅ NOVO: Override ToString para debug
    /// </summary>
    public override string ToString()
    {
        return $"FamilyItemVM: {Name} (ID: {Id}, Selected: {IsSelected}, Favorite: {IsFavorite})";
    }

    // ✅ TODA A FUNCIONALIDADE ORIGINAL MANTIDA:
    // ✅ Herança de BaseItemViewModel com todas as funcionalidades
    // ✅ Propriedades para UI binding
    // ✅ Status badges com cores
    // ✅ Indicadores visuais (recent, system, etc.)
    // ✅ Preview de descrição truncada
    // ✅ Formatação de datas
    // ✅ Debug e diagnóstico
    // ✅ Compatibilidade com código existente
}