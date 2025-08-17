using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Genera;

/// <summary>
/// Genus item ViewModel - MINIMAL implementation using base class
/// MIGRATED FROM 328 LINES TO ~70 LINES - Same pattern as SpeciesItemViewModel
/// </summary>
public partial class GenusItemViewModel : BaseItemViewModel<Models.Genus>
{
    #region Constructor

    public GenusItemViewModel(Models.Genus entity) : base(entity)
    {
    }

    #endregion

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
    /// </summary>
    public void UpdateFamilyInfo(string familyName)
    {
        try
        {
            if (ToModel()?.Family != null)
            {
                ToModel().Family.Name = familyName;
                OnPropertyChanged(nameof(FamilyName));
                this.LogInfo($"Updated family info for genus {Name}: {familyName}");
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