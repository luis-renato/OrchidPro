using OrchidPro.Models;
using OrchidPro.ViewModels;

namespace OrchidPro.ViewModels.Variants;

/// <summary>
/// Item ViewModel for individual Variant entities in list displays.
/// Follows exact pattern of FamilyItemViewModel since Variant is an independent entity.
/// Inherits all functionality from BaseItemViewModel with minimal customization.
/// MODERNIZED: Uses primary constructor pattern for cleaner code.
/// </summary>
public partial class VariantItemViewModel(Variant variant) : BaseItemViewModel<Variant>(variant)
{
    #region Required Base Class Override

    public override string EntityName => "Variant";

    #endregion

    #region Variant-Specific Properties (READ-ONLY)

    /// <summary>
    /// Favorite status for UI display
    /// </summary>
    public bool IsFavorite => Entity?.IsFavorite ?? false;

    #endregion

    #region Computed Properties (using base class Entity access)

    private Models.Variant Entity => ToModel();

    #endregion
}