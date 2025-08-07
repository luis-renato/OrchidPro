using OrchidPro.Models;
using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OrchidPro.ViewModels.Species;

/// <summary>
/// Item ViewModel for botanical species entities with species-specific properties and behaviors.
/// Extends base functionality with favorites, genus relationship display, and botanical characteristics.
/// </summary>
public partial class SpeciesItemViewModel : BaseItemViewModel<Models.Species>
{
    #region Base Class Implementation

    public override string EntityName => "Species";

    #endregion

    #region Species-Specific Properties

    /// <summary>
    /// Favorite status specific to Species entities
    /// </summary>
    public bool IsFavorite { get; }

    /// <summary>
    /// Foreign key to parent genus
    /// </summary>
    public Guid GenusId { get; }

    /// <summary>
    /// Genus name for display (if available)
    /// </summary>
    public string? GenusName { get; }

    /// <summary>
    /// Scientific name for botanical reference
    /// </summary>
    public string? ScientificName { get; }

    /// <summary>
    /// Common name for user-friendly display
    /// </summary>
    public string? CommonName { get; }

    /// <summary>
    /// Rarity status for collection interest
    /// </summary>
    public string RarityStatus { get; }

    /// <summary>
    /// Size category for space planning
    /// </summary>
    public string SizeCategory { get; }

    /// <summary>
    /// Fragrance indicator
    /// </summary>
    public bool? Fragrance { get; }

    /// <summary>
    /// Flowering season information
    /// </summary>
    public string? FloweringSeason { get; }

    #endregion

    #region Commands (will be set up later if needed)

    /// <summary>
    /// Command to view details - initially null, can be set by parent
    /// </summary>
    public IAsyncRelayCommand? ViewDetailsCommand { get; set; }

    /// <summary>
    /// Command to toggle favorite status - initially null, can be set by parent
    /// </summary>
    public IAsyncRelayCommand? ToggleFavoriteCommand { get; set; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initialize species item ViewModel with species-specific data
    /// </summary>
    public SpeciesItemViewModel(Models.Species species) : base(species)
    {
        IsFavorite = species.IsFavorite;
        GenusId = species.GenusId;
        GenusName = species.Genus?.Name;
        ScientificName = species.ScientificName;
        CommonName = species.CommonName;
        RarityStatus = species.RarityStatus;
        SizeCategory = species.SizeCategory;
        Fragrance = species.Fragrance;
        FloweringSeason = species.FloweringSeason;

        this.LogInfo($"Created: {species.Name} (Genus: {GenusName}, Scientific: {ScientificName}, Rarity: {RarityStatus})");
    }

    #endregion

    #region Enhanced UI Properties

    /// <summary>
    /// Customized description preview for botanical species
    /// </summary>
    public override string DescriptionPreview
    {
        get
        {
            return this.SafeExecute(() =>
            {
                if (!string.IsNullOrWhiteSpace(ScientificName))
                    return ScientificName;

                if (string.IsNullOrWhiteSpace(Description))
                    return "No botanical description available";

                return Description.Length > 120
                    ? $"{Description.Substring(0, 117)}..."
                    : Description;
            }, fallbackValue: "Description unavailable", operationName: "DescriptionPreview");
        }
    }

    /// <summary>
    /// Gets creation date for display
    /// </summary>
    public DateTime CreatedAt => this.SafeExecute(() => ToModel()?.CreatedAt ?? DateTime.Now);

    /// <summary>
    /// Recent indicator with species-specific icons including favorites and rarity
    /// </summary>
    public override string RecentIndicator =>
        this.SafeExecute(() =>
        {
            if (IsRecent) return "🌺";
            if (IsFavorite) return "⭐";
            if (RarityStatus == "Rare" || RarityStatus == "Very Rare") return "💎";
            if (Fragrance == true) return "🌸";
            return string.Empty;
        }, fallbackValue: string.Empty, operationName: "RecentIndicator");

    /// <summary>
    /// Subtitle showing genus and scientific name context
    /// </summary>
    public string Subtitle =>
        this.SafeExecute(() =>
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(GenusName))
                parts.Add($"Genus: {GenusName}");

            if (!string.IsNullOrWhiteSpace(ScientificName) && ScientificName != Name)
                parts.Add(ScientificName);
            else if (!string.IsNullOrWhiteSpace(CommonName))
                parts.Add($"'{CommonName}'");

            return parts.Count > 0 ? string.Join(" • ", parts) : "Species";
        }, fallbackValue: "Species", operationName: "Subtitle");

    /// <summary>
    /// Characteristics summary for quick reference
    /// </summary>
    public string CharacteristicsSummary =>
        this.SafeExecute(() =>
        {
            var characteristics = new List<string>();

            if (SizeCategory != "Medium")
                characteristics.Add($"Size: {SizeCategory}");

            if (RarityStatus != "Common")
                characteristics.Add($"Rarity: {RarityStatus}");

            if (Fragrance == true)
                characteristics.Add("Fragrant");

            if (!string.IsNullOrWhiteSpace(FloweringSeason))
                characteristics.Add($"Blooms: {FloweringSeason}");

            return characteristics.Count > 0
                ? string.Join(" • ", characteristics)
                : "Standard characteristics";
        }, fallbackValue: "No characteristics data", operationName: "CharacteristicsSummary");

    /// <summary>
    /// Short genus name for compact display
    /// </summary>
    public string ShortGenusName =>
        this.SafeExecute(() =>
        {
            if (string.IsNullOrWhiteSpace(GenusName))
                return "Unknown";

            return GenusName.Length > 15
                ? $"{GenusName.Substring(0, 12)}..."
                : GenusName;
        }, fallbackValue: "N/A", operationName: "ShortGenusName");

    /// <summary>
    /// Display name priority: Scientific > Common > Name
    /// </summary>
    public string PreferredDisplayName =>
        this.SafeExecute(() =>
        {
            if (!string.IsNullOrWhiteSpace(ScientificName))
                return ScientificName;

            if (!string.IsNullOrWhiteSpace(CommonName))
                return CommonName;

            return Name;
        }, fallbackValue: Name, operationName: "PreferredDisplayName");

    #endregion

    #region Data Access and Utility Methods

    /// <summary>
    /// Get the underlying Species model with proper typing
    /// </summary>
    public new Models.Species ToModel()
    {
        return this.SafeExecute(() =>
        {
            var model = base.ToModel();
            this.LogInfo($"Retrieved Species model for: {Name}");
            return model;
        }, fallbackValue: base.ToModel(), operationName: "ToModel");
    }

    /// <summary>
    /// Compare species for sorting with favorites-first and rarity logic
    /// </summary>
    public int CompareTo(SpeciesItemViewModel? other)
    {
        return this.SafeExecute(() =>
        {
            if (other == null) return 1;

            // Favorites first
            if (IsFavorite && !other.IsFavorite) return -1;
            if (!IsFavorite && other.IsFavorite) return 1;

            // Then by rarity (rare species first)
            var rarityOrder = new Dictionary<string, int>
            {
                ["Extinct"] = 0,
                ["Very Rare"] = 1,
                ["Rare"] = 2,
                ["Uncommon"] = 3,
                ["Common"] = 4
            };

            var thisRarityValue = rarityOrder.ContainsKey(RarityStatus) ? rarityOrder[RarityStatus] : 5;
            var otherRarityValue = rarityOrder.ContainsKey(other.RarityStatus) ? rarityOrder[other.RarityStatus] : 5;

            if (thisRarityValue != otherRarityValue)
                return thisRarityValue.CompareTo(otherRarityValue);

            // Then by genus name
            var genusComparison = string.Compare(GenusName, other.GenusName, StringComparison.OrdinalIgnoreCase);
            if (genusComparison != 0) return genusComparison;

            // Finally by species name (prefer scientific name if available)
            var thisDisplayName = PreferredDisplayName;
            var otherDisplayName = other.PreferredDisplayName;
            return string.Compare(thisDisplayName, otherDisplayName, StringComparison.OrdinalIgnoreCase);

        }, fallbackValue: 0, operationName: "CompareTo");
    }

    /// <summary>
    /// Get summary text for quick info display
    /// </summary>
    public string GetSummary()
    {
        return this.SafeExecute(() =>
        {
            var parts = new List<string> { PreferredDisplayName };

            if (!string.IsNullOrEmpty(GenusName))
                parts.Add($"Genus: {ShortGenusName}");

            if (!string.IsNullOrEmpty(Description))
            {
                var shortDesc = Description.Length > 50
                    ? $"{Description.Substring(0, 47)}..."
                    : Description;
                parts.Add(shortDesc);
            }

            return string.Join(" | ", parts);
        }, fallbackValue: Name, operationName: "GetSummary");
    }

    /// <summary>
    /// Get cultivation summary for care planning
    /// </summary>
    public string GetCultivationSummary()
    {
        return this.SafeExecute(() =>
        {
            var summary = new List<string>();

            var species = ToModel();

            if (!string.IsNullOrWhiteSpace(species.TemperaturePreference))
                summary.Add($"Temp: {species.TemperaturePreference}");

            if (!string.IsNullOrWhiteSpace(species.LightRequirements))
                summary.Add($"Light: {species.LightRequirements}");

            if (!string.IsNullOrWhiteSpace(species.GrowthHabit))
                summary.Add($"Growth: {species.GrowthHabit}");

            return summary.Count > 0
                ? string.Join(" • ", summary)
                : "Cultivation info not available";

        }, fallbackValue: "No cultivation data", operationName: "GetCultivationSummary");
    }

    #endregion
}