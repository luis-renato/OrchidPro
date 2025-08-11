using OrchidPro.Models;
using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OrchidPro.ViewModels.Species;

/// <summary>
/// OPTIMIZED Item ViewModel for botanical species entities.
/// Reduced logging, cached properties, minimal SafeExecute usage for performance.
/// </summary>
public partial class SpeciesItemViewModel : BaseItemViewModel<Models.Species>
{
    #region Cached Model Reference (PERFORMANCE OPTIMIZATION)

    private readonly Models.Species _cachedModel;

    #endregion

    #region Base Class Implementation

    public override string EntityName => "Species";

    #endregion

    #region Species-Specific Properties (CACHED FOR PERFORMANCE)

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

    #region Constructor (OPTIMIZED)

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

        // COMPLETELY REMOVED logging for performance - constructor called frequently
    }

    #endregion

    #region OPTIMIZED UI Properties (CACHED, NO REPEATED CALCULATIONS)

    private string? _descriptionPreview;
    public override string DescriptionPreview
    {
        get
        {
            if (_descriptionPreview == null)
            {
                _descriptionPreview = !string.IsNullOrWhiteSpace(ScientificName)
                    ? ScientificName
                    : string.IsNullOrWhiteSpace(Description)
                        ? "No botanical description available"
                        : Description.Length > 120
                            ? $"{Description.Substring(0, 117)}..."
                            : Description;
            }
            return _descriptionPreview;
        }
    }

    private DateTime? _createdAt;
    public DateTime CreatedAt
    {
        get
        {
            _createdAt ??= _cachedModel.CreatedAt;
            return _createdAt.Value;
        }
    }

    private string? _recentIndicator;
    public override string RecentIndicator
    {
        get
        {
            if (_recentIndicator == null)
            {
                _recentIndicator = IsRecent ? "🌺"
                    : IsFavorite ? "⭐"
                    : (RarityStatus == "Rare" || RarityStatus == "Very Rare") ? "💎"
                    : Fragrance == true ? "🌸"
                    : string.Empty;
            }
            return _recentIndicator;
        }
    }

    private string? _subtitle;
    public string Subtitle
    {
        get
        {
            if (_subtitle == null)
            {
                var parts = new List<string>();

                if (!string.IsNullOrWhiteSpace(GenusName) && GenusName != "Unknown")
                    parts.Add($"Genus: {GenusName}");

                if (!string.IsNullOrWhiteSpace(ScientificName) && ScientificName != Name)
                    parts.Add(ScientificName);
                else if (!string.IsNullOrWhiteSpace(CommonName))
                    parts.Add($"'{CommonName}'");

                _subtitle = parts.Count > 0 ? string.Join(" • ", parts) : "Species";
            }
            return _subtitle;
        }
    }

    private string? _characteristicsSummary;
    public string CharacteristicsSummary
    {
        get
        {
            if (_characteristicsSummary == null)
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

                _characteristicsSummary = characteristics.Count > 0
                    ? string.Join(" • ", characteristics)
                    : "Standard characteristics";
            }
            return _characteristicsSummary;
        }
    }

    private string? _shortGenusName;
    public string ShortGenusName
    {
        get
        {
            if (_shortGenusName == null)
            {
                _shortGenusName = string.IsNullOrWhiteSpace(GenusName) || GenusName == "Unknown"
                    ? "Unknown"
                    : GenusName.Length > 15
                        ? $"{GenusName.Substring(0, 12)}..."
                        : GenusName;
            }
            return _shortGenusName;
        }
    }

    private string? _preferredDisplayName;
    public string PreferredDisplayName
    {
        get
        {
            if (_preferredDisplayName == null)
            {
                _preferredDisplayName = !string.IsNullOrWhiteSpace(ScientificName)
                    ? ScientificName
                    : !string.IsNullOrWhiteSpace(CommonName)
                        ? CommonName
                        : Name;
            }
            return _preferredDisplayName;
        }
    }

    #endregion

    #region OPTIMIZED Data Access Methods

    /// <summary>
    /// Get the underlying Species model (OPTIMIZED - returns cached reference)
    /// </summary>
    public new Models.Species ToModel()
    {
        // Return cached model instead of calling base and adding logging overhead
        return _cachedModel;
    }

    /// <summary>
    /// OPTIMIZED comparison for sorting with minimal calculations
    /// </summary>
    public int CompareTo(SpeciesItemViewModel? other)
    {
        if (other == null) return 1;

        // Favorites first
        if (IsFavorite && !other.IsFavorite) return -1;
        if (!IsFavorite && other.IsFavorite) return 1;

        // Then by rarity (using simple string comparison for performance)
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
                ? $"{Description.Substring(0, 47)}..."
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