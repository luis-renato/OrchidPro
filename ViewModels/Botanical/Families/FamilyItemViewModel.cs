using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Botanical.Families;

/// <summary>
/// Family item ViewModel - MINIMAL implementation using base class
/// MIGRATED FROM 180 LINES TO ~60 LINES - Same pattern as SpeciesItemViewModel
/// MODERNIZED: Uses primary constructor for cleaner code
/// </summary>
public partial class FamilyItemViewModel(Models.Family entity) : BaseItemViewModel<Models.Family>(entity)
{
    #region Required Base Class Override

    public override string EntityName => "Family";

    #endregion

    #region Family-Specific Properties (READ-ONLY)

    /// <summary>
    /// Favorite status for UI display
    /// </summary>
    public bool IsFavorite => Entity?.IsFavorite ?? false;

    /// <summary>
    /// Quick check if this is an orchid family
    /// </summary>
    public bool IsOrchidaceae => Name.Contains("Orchidaceae", StringComparison.OrdinalIgnoreCase);

    #endregion

    #region Debug Helper

    /// <summary>
    /// Debug method to check sorting properties
    /// </summary>
    public void DebugSortProperties()
    {
        this.LogInfo($"Family Debug - Name: '{Name}', IsFavorite: {IsFavorite}, IsOrchidaceae: {IsOrchidaceae}, CreatedAt: {CreatedAt:yyyy-MM-dd}");
    }

    #endregion

    #region Computed Properties (using base class Entity access)

    private Models.Family Entity => ToModel();

    #endregion
}