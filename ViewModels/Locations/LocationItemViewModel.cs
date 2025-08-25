using OrchidPro.Models;
using OrchidPro.ViewModels.Base;

namespace OrchidPro.ViewModels.Locations;

/// <summary>
/// LocationItemViewModel - CORRIGIDO seguindo padrão FamilyItemViewModel
/// </summary>
public partial class LocationItemViewModel(PlantLocation location) : BaseItemViewModel<PlantLocation>(location)
{
    #region Required Base Class Override

    public override string EntityName => "Location";

    #endregion

    #region Location-Specific Properties (READ-ONLY) - Seguindo padrão Family

    /// <summary>
    /// Favorite status for UI display - IGUAL ao Family
    /// </summary>
    public bool IsFavorite => Entity?.IsFavorite ?? false;

    /// <summary>
    /// Location type for UI display
    /// </summary>
    public string? LocationType => Entity?.LocationType;

    /// <summary>
    /// Environment notes for UI display
    /// </summary>
    public string? EnvironmentNotes => Entity?.EnvironmentNotes;

    #endregion

    #region Display Properties

    /// <summary>
    /// Display formatting for location type
    /// </summary>
    public string LocationTypeDisplay => string.IsNullOrWhiteSpace(LocationType) ? "Not specified" : LocationType;

    /// <summary>
    /// Check if has environment information
    /// </summary>
    public bool HasEnvironmentInfo => !string.IsNullOrWhiteSpace(EnvironmentNotes);

    #endregion

    #region Computed Properties - IGUAL ao Family

    private PlantLocation Entity => ToModel();

    #endregion
}