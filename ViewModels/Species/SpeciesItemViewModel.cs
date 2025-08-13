using OrchidPro.Models;
using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OrchidPro.ViewModels.Species;

/// <summary>
/// PERFORMANCE OPTIMIZED Item ViewModel for botanical species.
/// Simple optimizations: cached properties, reduced logging, pre-computed values.
/// Maintains 100% API compatibility.
/// </summary>
public partial class SpeciesItemViewModel : BaseItemViewModel<Models.Species>
{
    #region PERFORMANCE OPTIMIZATION: Cached Model Reference

    private readonly Models.Species _cachedModel;

    #endregion

    #region Base Class Implementation

    public override string EntityName => "Species";

    #endregion

    #region PERFORMANCE OPTIMIZED: Species-Specific Properties

    public bool IsFavorite { get; }
    public Guid GenusId { get; }
    public string GenusName { get; }
    public string ScientificName { get; }
    public string CommonName { get; }
    public string RarityStatus { get; }
    public string SizeCategory { get; }
    public bool? Fragrance { get; }
    public string FloweringSeason { get; }

    #endregion

    #region Commands (minimal setup)

    public IAsyncRelayCommand? ViewDetailsCommand { get; set; }
    public IAsyncRelayCommand? ToggleFavoriteCommand { get; set; }

    #endregion

    #region PERFORMANCE OPTIMIZED Constructor

    /// <summary>
    /// OPTIMIZED Constructor - removed logging overhead for frequent calls
    /// </summary>
    public SpeciesItemViewModel(Models.Species species) : base(species)
    {
        // Cache the model reference to avoid repeated calls
        _cachedModel = species;

        // Cache all properties at construction to avoid repeated calculations
        IsFavorite = species.IsFavorite;
        GenusId = species.GenusId;
        GenusName = species.Genus?.Name ?? "Unknown";
        ScientificName = species.ScientificName ?? string.Empty;
        CommonName = species.CommonName ?? string.Empty;
        RarityStatus = species.RarityStatus ?? "Common";
        SizeCategory = species.SizeCategory ?? "Medium";
        Fragrance = species.Fragrance;
        FloweringSeason = species.FloweringSeason ?? string.Empty;

        // REMOVED logging for performance - constructor called frequently
    }

    #endregion

    #region PERFORMANCE OPTIMIZED: UI Properties

    private string? _cachedDescriptionPreview;
    public override string DescriptionPreview
    {
        get
        {
            if (_cachedDescriptionPreview == null)
            {
                _cachedDescriptionPreview = !string.IsNullOrWhiteSpace(ScientificName)
                    ? ScientificName
                    : string.IsNullOrWhiteSpace(Description)
                        ? "No botanical description available"
                        : Description.Length > 120
                            ? $"{Description[..117]}..."
                            : Description;
            }
            return _cachedDescriptionPreview;
        }
    }

    private string? _cachedRecentIndicator;
    public override string RecentIndicator
    {
        get
        {
            if (_cachedRecentIndicator == null)
            {
                _cachedRecentIndicator = IsRecent ? "🌺"
                    : IsFavorite ? "⭐"
                    : (RarityStatus == "Rare" || RarityStatus == "Very Rare") ? "💎"
                    : Fragrance == true ? "🌸"
                    : string.Empty;
            }
            return _cachedRecentIndicator;
        }
    }

    private string? _cachedSubtitle;
    public string Subtitle
    {
        get
        {
            if (_cachedSubtitle == null)
            {
                var parts = new List<string>();

                if (!string.IsNullOrWhiteSpace(GenusName) && GenusName != "Unknown")
                    parts.Add($"Genus: {GenusName}");

                if (!string.IsNullOrWhiteSpace(ScientificName) && ScientificName != Name)
                    parts.Add(ScientificName);
                else if (!string.IsNullOrWhiteSpace(CommonName))
                    parts.Add($"'{CommonName}'");

                _cachedSubtitle = parts.Count > 0 ? string.Join(" • ", parts) : "Species";
            }
            return _cachedSubtitle;
        }
    }

    private string? _cachedCharacteristicsSummary;
    public string CharacteristicsSummary
    {
        get
        {
            if (_cachedCharacteristicsSummary == null)
            {
                var characteristics = new List<string>();

                if (SizeCategory != "Medium" && !string.IsNullOrEmpty(SizeCategory))
                    characteristics.Add($"Size: {SizeCategory}");

                if (RarityStatus != "Common" && !string.IsNullOrEmpty(RarityStatus))
                    characteristics.Add($"Rarity: {RarityStatus}");

                if (Fragrance == true)
                    characteristics.Add("Fragrant");

                if (!string.IsNullOrWhiteSpace(FloweringSeason))
                    characteristics.Add($"Blooms: {FloweringSeason}");

                _cachedCharacteristicsSummary = characteristics.Count > 0
                    ? string.Join(" • ", characteristics)
                    : "Standard characteristics";
            }
            return _cachedCharacteristicsSummary;
        }
    }

    private string? _cachedShortGenusName;
    public string ShortGenusName
    {
        get
        {
            if (_cachedShortGenusName == null)
            {
                _cachedShortGenusName = string.IsNullOrWhiteSpace(GenusName) || GenusName == "Unknown"
                    ? "Unknown"
                    : GenusName.Length > 15
                        ? $"{GenusName[..12]}..."
                        : GenusName;
            }
            return _cachedShortGenusName;
        }
    }

    private string? _cachedPreferredDisplayName;
    public string PreferredDisplayName
    {
        get
        {
            if (_cachedPreferredDisplayName == null)
            {
                _cachedPreferredDisplayName = !string.IsNullOrWhiteSpace(ScientificName)
                    ? ScientificName
                    : !string.IsNullOrWhiteSpace(CommonName)
                        ? CommonName
                        : Name;
            }
            return _cachedPreferredDisplayName;
        }
    }

    #endregion

    #region PERFORMANCE OPTIMIZED: Data Access Methods

    /// <summary>
    /// Get the underlying Species model (OPTIMIZED - returns cached reference)
    /// </summary>
    public new Models.Species ToModel()
    {
        return _cachedModel;
    }

    /// <summary>
    /// OPTIMIZED comparison for sorting
    /// </summary>
    public int CompareTo(SpeciesItemViewModel? other)
    {
        if (other == null) return 1;

        // Favorites first
        if (IsFavorite && !other.IsFavorite) return -1;
        if (!IsFavorite && other.IsFavorite) return 1;

        // Then by rarity
        if (RarityStatus != other.RarityStatus)
        {
            var rarityOrder = new Dictionary<string, int>
            {
                ["Extinct"] = 0,
                ["Very Rare"] = 1,
                ["Rare"] = 2,
                ["Uncommon"] = 3,
                ["Common"] = 4
            };

            var thisValue = rarityOrder.GetValueOrDefault(RarityStatus, 5);
            var otherValue = rarityOrder.GetValueOrDefault(other.RarityStatus, 5);

            if (thisValue != otherValue)
                return thisValue.CompareTo(otherValue);
        }

        // Then by genus name
        var genusComparison = string.Compare(GenusName, other.GenusName, StringComparison.OrdinalIgnoreCase);
        if (genusComparison != 0) return genusComparison;

        // Finally by preferred display name
        return string.Compare(PreferredDisplayName, other.PreferredDisplayName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// OPTIMIZED summary with cached properties
    /// </summary>
    public string GetSummary()
    {
        var parts = new List<string> { PreferredDisplayName };

        if (!string.IsNullOrEmpty(GenusName) && GenusName != "Unknown")
            parts.Add($"Genus: {ShortGenusName}");

        if (!string.IsNullOrEmpty(Description))
        {
            var shortDesc = Description.Length > 50
                ? $"{Description[..47]}..."
                : Description;
            parts.Add(shortDesc);
        }

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// OPTIMIZED cultivation summary with cached model access
    /// </summary>
    public string GetCultivationSummary()
    {
        var summary = new List<string>();

        if (!string.IsNullOrWhiteSpace(_cachedModel.TemperaturePreference))
            summary.Add($"Temp: {_cachedModel.TemperaturePreference}");

        if (!string.IsNullOrWhiteSpace(_cachedModel.LightRequirements))
            summary.Add($"Light: {_cachedModel.LightRequirements}");

        if (!string.IsNullOrWhiteSpace(_cachedModel.GrowthHabit))
            summary.Add($"Growth: {_cachedModel.GrowthHabit}");

        return summary.Count > 0
            ? string.Join(" • ", summary)
            : "Cultivation info not available";
    }

    #endregion
}