using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Botanical.Species;

/// <summary>
/// Species item ViewModel - MINIMAL implementation using base class
/// MODERNIZED: Uses primary constructor and fixed null reference warnings
/// </summary>
public partial class SpeciesItemViewModel(Models.Species entity) : BaseItemViewModel<Models.Species>(entity)
{
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
    /// FIXED CS8602: Added null checks to prevent null reference exceptions
    /// </summary>
    public void UpdateGenusInfo(string genusName)
    {
        try
        {
            var model = ToModel();
            // FIXED CS8602: Check model and Genus for null before dereferencing
            if (model?.Genus != null)
            {
                model.Genus.Name = genusName;
                OnPropertyChanged(nameof(GenusName));
                this.LogInfo($"Updated genus info for species {Name}: {genusName}");
            }
            else
            {
                this.LogWarning($"Cannot update genus info for species {Name}: model or genus is null");
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