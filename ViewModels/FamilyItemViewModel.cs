using OrchidPro.Models;
using System.Diagnostics;

namespace OrchidPro.ViewModels;

/// <summary>
/// CORRIGIDO: FamilyItemViewModel - representa item individual na lista
/// </summary>
public class FamilyItemViewModel : BaseItemViewModel<Family>
{
    public override string EntityName => "Family";

    public FamilyItemViewModel(Family family) : base(family)
    {
        Debug.WriteLine($"✅ [FAMILY_ITEM_VM] Created: {family.Name}");
    }

    /// <summary>
    /// Propriedade específica: indica se é família de orquídeas
    /// </summary>
    public bool IsOrchidaceae => Name?.Contains("Orchidaceae", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// Status badge personalizado para famílias
    /// </summary>
    public override Color StatusBadgeColor
    {
        get
        {
            if (IsOrchidaceae) return Color.FromArgb("#9C27B0"); // Purple for orchids
            return base.StatusBadgeColor;
        }
    }

    /// <summary>
    /// Indica se é família popular/conhecida
    /// </summary>
    public bool IsPopularFamily => IsOrchidaceae ||
                                   Name.Contains("Bromeliac", StringComparison.OrdinalIgnoreCase) ||
                                   Name.Contains("Arac", StringComparison.OrdinalIgnoreCase);
}