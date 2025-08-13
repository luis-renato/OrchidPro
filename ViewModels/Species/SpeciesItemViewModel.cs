using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Species;

/// <summary>
/// Species item ViewModel - MINIMAL implementation using base class
/// </summary>
public partial class SpeciesItemViewModel : BaseItemViewModel<Models.Species>
{
    #region Constructor

    public SpeciesItemViewModel(Models.Species entity) : base(entity)
    {
    }

    #endregion

    #region Required Base Class Override

    public override string EntityName => "Species";

    #endregion

    #region Species-Specific Properties (READ-ONLY)

    public string GenusName => Entity?.Genus?.Name ?? "Unknown Genus";
    public string? SizeCategory => Entity?.SizeCategory;
    public string? RarityStatus => Entity?.RarityStatus;
    public string? FloweringSeason => Entity?.FloweringSeason;
    public bool Fragrance => Entity?.Fragrance ?? false;
    public bool IsFavorite => Entity?.IsFavorite ?? false;

    #endregion

    #region CRITICAL FIX: Silent Genus Update

    /// <summary>
    /// Update genus information and notify UI
    /// </summary>
    public void UpdateGenusInfo(string genusName)
    {
        try
        {
            if (ToModel()?.Genus != null)
            {
                ToModel().Genus.Name = genusName;
                OnPropertyChanged(nameof(GenusName));
                this.LogInfo($"Updated genus info for species {Name}: {genusName}");
            }
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error updating genus info");
        }
    }

    #endregion

    #region CRITICAL FIX: Debug Sort Properties

    /// <summary>
    /// Debug method to check sorting properties
    /// </summary>
    public void DebugSortProperties()
    {
        this.LogInfo($"Species Debug - Name: '{Name}', DisplayName: '{DisplayName}', IsFavorite: {IsFavorite}, CreatedAt: {CreatedAt:yyyy-MM-dd}");
    }

    #endregion

    #region Computed Properties (using base class Entity access)

    private Models.Species Entity => ToModel();

    #endregion
}