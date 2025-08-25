using OrchidPro.Models;
using OrchidPro.ViewModels.Base;

namespace OrchidPro.ViewModels.Sources;

/// <summary>
/// SourceItemViewModel - CORRIGIDO seguindo padrão FamilyItemViewModel
/// </summary>
public partial class SourceItemViewModel(Source source) : BaseItemViewModel<Source>(source)
{
    #region Required Base Class Override

    public override string EntityName => "Source";

    #endregion

    #region Source-Specific Properties (READ-ONLY) - Seguindo padrão Family

    /// <summary>
    /// Favorite status for UI display - IGUAL ao Family
    /// </summary>
    public bool IsFavorite => Entity?.IsFavorite ?? false;

    /// <summary>
    /// Supplier type for UI display
    /// </summary>
    public string? SupplierType => Entity?.SupplierType;

    /// <summary>
    /// Contact info for UI display
    /// </summary>
    public string? ContactInfo => Entity?.ContactInfo;

    /// <summary>
    /// Website for UI display
    /// </summary>
    public string? Website => Entity?.Website;

    #endregion

    #region Display Properties

    /// <summary>
    /// Display formatting for supplier type
    /// </summary>
    public string SupplierTypeDisplay => string.IsNullOrWhiteSpace(SupplierType) ? "Not specified" : SupplierType;

    /// <summary>
    /// Check if has contact information
    /// </summary>
    public bool HasContact => !string.IsNullOrWhiteSpace(ContactInfo) || !string.IsNullOrWhiteSpace(Website);

    #endregion

    #region Computed Properties - IGUAL ao Family

    private Source Entity => ToModel();

    #endregion
}