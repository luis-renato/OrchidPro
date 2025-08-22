using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Botanical.Genera;

/// <summary>
/// Genus item ViewModel - MINIMAL implementation using base class
/// MIGRATED FROM 328 LINES TO ~70 LINES - Same pattern as SpeciesItemViewModel
/// MODERNIZED: Uses primary constructor and fixed null reference warnings
/// </summary>
public partial class GenusItemViewModel(Models.Genus entity) : BaseItemViewModel<Models.Genus>(entity)
{
    #region Required Base Class Override

    public override string EntityName => "Genus";

    #endregion

    #region Genus-Specific Properties (READ-ONLY)

    /// <summary>
    /// Family name for hierarchical display
    /// </summary>
    public string FamilyName => Entity?.Family?.Name ?? "Unknown Family";

    /// <summary>
    /// Family ID for relationship tracking
    /// </summary>
    public Guid FamilyId => Entity?.FamilyId ?? Guid.Empty;

    /// <summary>
    /// Favorite status for UI display
    /// </summary>
    public bool IsFavorite => Entity?.IsFavorite ?? false;

    /// <summary>
    /// Quick check if this is an orchid genus
    /// </summary>
    public bool IsOrchidGenus => !string.IsNullOrEmpty(FamilyName) &&
                                FamilyName.Contains("Orchidaceae", StringComparison.OrdinalIgnoreCase);

    #endregion

    #region CRITICAL FIX: Silent Family Update

    /// <summary>
    /// Update family information and notify UI
    /// Similar to Species.UpdateGenusInfo pattern
    /// FIXED CS8602: Added null checks to prevent null reference exceptions
    /// </summary>
    public void UpdateFamilyInfo(string familyName)
    {
        try
        {
            var model = ToModel();
            // FIXED CS8602: Check model and Family for null before dereferencing
            if (model?.Family != null)
            {
                model.Family.Name = familyName;
                OnPropertyChanged(nameof(FamilyName));
                this.LogInfo($"Updated family info for genus {Name}: {familyName}");
            }
            else
            {
                this.LogWarning($"Cannot update family info for genus {Name}: model or family is null");
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error updating family info");
        }
    }

    #endregion

    #region Debug Helper

    /// <summary>
    /// Debug method to check sorting properties
    /// </summary>
    public void DebugSortProperties()
    {
        this.LogInfo($"Genus Debug - Name: '{Name}', FamilyName: '{FamilyName}', IsFavorite: {IsFavorite}, CreatedAt: {CreatedAt:yyyy-MM-dd}");
    }

    #endregion

    #region Computed Properties (using base class Entity access)

    private Models.Genus Entity => ToModel();

    #endregion
}