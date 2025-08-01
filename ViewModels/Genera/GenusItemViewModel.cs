using OrchidPro.Models;
using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Genera;

/// <summary>
/// ViewModel for individual genus items in lists
/// Provides genus-specific display logic and UI properties following Family pattern
/// </summary>
public class GenusItemViewModel : BaseItemViewModel<Genus>
{
    #region Private Fields

    private readonly Genus _genus;

    #endregion

    #region Public Properties

    /// <summary>
    /// Entity name for logging and display purposes
    /// </summary>
    public override string EntityName => "Genus";

    /// <summary>
    /// Family ID this genus belongs to
    /// </summary>
    public Guid FamilyId => _genus.FamilyId;

    /// <summary>
    /// Family name for display
    /// </summary>
    public string FamilyName => _genus.FamilyName;

    /// <summary>
    /// Full display name with family context
    /// </summary>
    public string FullDisplayName => _genus.FullDisplayName;

    /// <summary>
    /// Extended status display with family and favorite info
    /// </summary>
    public string FullStatusDisplay => _genus.FullStatusDisplay;

    /// <summary>
    /// IsFavorite property from base entity
    /// </summary>
    public bool IsFavorite => _genus.IsFavorite;

    #endregion

    #region Display Properties for UI Binding

    /// <summary>
    /// Primary text for list display
    /// </summary>
    public string PrimaryText => Name;

    /// <summary>
    /// Secondary text showing family relationship
    /// </summary>
    public string SecondaryText => $"Family: {FamilyName}";

    /// <summary>
    /// Tertiary text for additional context
    /// </summary>
    public string TertiaryText => !string.IsNullOrWhiteSpace(Description)
        ? Description.Length > 80
            ? $"{Description[..77]}..."
            : Description
        : "No description";

    /// <summary>
    /// Status color for visual indicators
    /// </summary>
    public Color StatusColor
    {
        get
        {
            return this.SafeExecute(() =>
            {
                if (!IsActive) return Color.FromArgb("#9E9E9E"); // Gray
                if (IsFavorite) return Color.FromArgb("#FF9800"); // Orange
                if (IsSystemDefault) return Color.FromArgb("#2196F3"); // Blue
                return Color.FromArgb("#4CAF50"); // Green for active
            }, fallbackValue: Color.FromArgb("#4CAF50"), operationName: "StatusColor");
        }
    }

    /// <summary>
    /// Icon for genus type visualization
    /// </summary>
    public string GenusIcon
    {
        get
        {
            return this.SafeExecute(() =>
            {
                // Check if it's a common orchid genus
                if (IsOrchidGenus) return "🌺";
                if (IsFavorite) return "⭐";
                if (IsSystemDefault) return "🔒";
                return "🌱"; // Default genus icon
            }, fallbackValue: "🌱", operationName: "GenusIcon");
        }
    }

    /// <summary>
    /// Check if this is a common orchid genus
    /// </summary>
    public bool IsOrchidGenus
    {
        get
        {
            return this.SafeExecute(() =>
            {
                var orchidGenera = new[]
                {
                    "Cattleya", "Phalaenopsis", "Dendrobium", "Oncidium",
                    "Paphiopedilum", "Cymbidium", "Vanda", "Miltonia",
                    "Brassia", "Laelia", "Bulbophyllum", "Epidendrum"
                };

                return orchidGenera.Contains(Name, StringComparer.OrdinalIgnoreCase);
            }, fallbackValue: false, operationName: "IsOrchidGenus");
        }
    }

    /// <summary>
    /// Selection state visual indicators for UI binding
    /// </summary>
    public string SelectionIcon => IsSelected ? "☑️" : "☐";
    public Color SelectionColor => IsSelected ? Color.FromArgb("#2196F3") : Color.FromArgb("#E0E0E0");

    /// <summary>
    /// Combined status icon with priority-based selection
    /// </summary>
    public string StatusIcon
    {
        get
        {
            return this.SafeExecute(() =>
            {
                if (!IsActive) return "⏸️";
                if (IsFavorite) return "⭐";
                if (IsOrchidGenus) return "🌺";
                if (IsSystemDefault) return "🔒";
                return "🌱";
            }, fallbackValue: "🌱", operationName: "StatusIcon");
        }
    }

    /// <summary>
    /// Comprehensive tooltip information for enhanced UX
    /// </summary>
    public string TooltipText
    {
        get
        {
            return this.SafeExecute(() =>
            {
                var parts = new List<string>();

                if (IsFavorite) parts.Add("Favorite genus");
                if (IsOrchidGenus) parts.Add("Orchid genus");
                if (IsSystemDefault) parts.Add("System default");
                if (!IsActive) parts.Add("Inactive");

                parts.Add($"Family: {FamilyName}");
                parts.Add($"Created {CreatedAt:dd/MM/yyyy}");

                return string.Join(" • ", parts);
            }, fallbackValue: $"Genus: {Name}", operationName: "TooltipText");
        }
    }

    /// <summary>
    /// Badge text for family reference
    /// </summary>
    public string FamilyBadge => FamilyName.Length > 15 ? $"{FamilyName[..12]}..." : FamilyName;

    /// <summary>
    /// Subtitle combining family and status
    /// </summary>
    public string Subtitle => $"{FamilyName} • {FullStatusDisplay}";

    #endregion

    #region Constructor

    /// <summary>
    /// Initialize genus item ViewModel with enhanced display properties
    /// </summary>
    public GenusItemViewModel(Genus genus) : base(genus)
    {
        _genus = genus;

        this.SafeExecute(() =>
        {
            this.LogInfo($"GenusItemViewModel created for: {Name} (Family: {FamilyName})");
        }, "GenusItemViewModel Constructor");
    }

    #endregion

    #region Data Access and Utility Methods

    /// <summary>
    /// Get the underlying Genus model with proper typing
    /// </summary>
    public new Genus ToModel()
    {
        return this.SafeExecute(() =>
        {
            var model = base.ToModel();
            this.LogInfo($"Retrieved Genus model for: {Name} (Family: {FamilyName})");
            return model;
        }, fallbackValue: base.ToModel(), operationName: "ToModel");
    }

    /// <summary>
    /// Compare genera for sorting with favorites-first logic
    /// </summary>
    public int CompareTo(GenusItemViewModel? other)
    {
        return this.SafeExecute(() =>
        {
            if (other == null) return 1;

            // Priority order: Favorites > System defaults > Regular > Inactive
            var thisPriority = GetSortPriority();
            var otherPriority = other.GetSortPriority();

            if (thisPriority != otherPriority)
                return thisPriority.CompareTo(otherPriority);

            // Secondary sort by family name
            var familyComparison = string.Compare(FamilyName, other.FamilyName, StringComparison.OrdinalIgnoreCase);
            if (familyComparison != 0)
                return familyComparison;

            // Tertiary sort by genus name
            return string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }, fallbackValue: 0, operationName: "CompareTo");
    }

    /// <summary>
    /// Get sorting priority based on genus properties
    /// </summary>
    private int GetSortPriority()
    {
        return this.SafeExecute(() =>
        {
            if (!IsActive) return 4; // Inactive last
            if (IsFavorite) return 1; // Favorites first
            if (IsSystemDefault) return 2; // System defaults second
            return 3; // Regular genera third
        }, fallbackValue: 3, operationName: "GetSortPriority");
    }

    /// <summary>
    /// Check if this genus matches search criteria
    /// </summary>
    public bool MatchesSearch(string searchText)
    {
        return this.SafeExecute(() =>
        {
            if (string.IsNullOrWhiteSpace(searchText)) return true;

            var search = searchText.ToLowerInvariant();
            return Name.ToLowerInvariant().Contains(search) ||
                   (Description?.ToLowerInvariant().Contains(search) ?? false) ||
                   FamilyName.ToLowerInvariant().Contains(search);
        }, fallbackValue: false, operationName: "MatchesSearch");
    }

    /// <summary>
    /// Get display text for current sorting mode
    /// </summary>
    public string GetSortDisplayText(string sortMode)
    {
        return this.SafeExecute(() =>
        {
            return sortMode?.ToLowerInvariant() switch
            {
                "family" => FamilyName,
                "created" => CreatedAt.ToString("dd/MM/yyyy"),
                "updated" => UpdatedAt.ToString("dd/MM/yyyy"),
                "status" => StatusDisplay,
                _ => Name
            };
        }, fallbackValue: Name, operationName: "GetSortDisplayText");
    }

    #endregion

    #region Equality and Hash Code

    /// <summary>
    /// Equality comparison based on genus ID
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is GenusItemViewModel other && Id == other.Id;
    }

    /// <summary>
    /// Hash code based on genus ID
    /// </summary>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    #endregion
}