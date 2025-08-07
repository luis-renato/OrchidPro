using OrchidPro.Models;
using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OrchidPro.ViewModels.Genera;

/// <summary>
/// Item ViewModel for botanical genus entities with genus-specific properties and behaviors.
/// Extends base functionality with favorites, family relationship display, and enhanced UI binding.
/// </summary>
public partial class GenusItemViewModel : BaseItemViewModel<Genus>
{
    #region Base Class Implementation

    public override string EntityName => "Genus";

    #endregion

    #region Genus-Specific Properties

    /// <summary>
    /// Favorite status specific to Genus entities
    /// </summary>
    public bool IsFavorite { get; }

    /// <summary>
    /// Foreign key to parent family
    /// </summary>
    public Guid FamilyId { get; }

    /// <summary>
    /// Family name for display (if available)
    /// </summary>
    public string? FamilyName { get; }

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
    /// Initialize genus item ViewModel with genus-specific data
    /// </summary>
    public GenusItemViewModel(Genus genus) : base(genus)
    {
        IsFavorite = genus.IsFavorite;
        FamilyId = genus.FamilyId;
        FamilyName = genus.Family?.Name;
        this.LogInfo($"Created: {genus.Name} (Family: {FamilyName}, Favorite: {IsFavorite}, ID: {genus.Id})");
    }

    #endregion

    #region Enhanced UI Properties

    /// <summary>
    /// Customized description preview for botanical genera
    /// </summary>
    public override string DescriptionPreview
    {
        get
        {
            return this.SafeExecute(() =>
            {
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
    /// Recent indicator with genus-specific icons including favorites
    /// </summary>
    public override string RecentIndicator =>
        this.SafeExecute(() => IsRecent ? "🌱" : (IsFavorite ? "⭐" : ""),
                        fallbackValue: "",
                        operationName: "RecentIndicator");

    /// <summary>
    /// Extended status display including genus-specific indicators and family context
    /// </summary>
    public override string FullStatusDisplay
    {
        get
        {
            return this.SafeExecute(() =>
            {
                var status = StatusDisplay;
                if (IsSystemDefault) status += " • System";
                if (IsRecent) status += " • New";
                if (!string.IsNullOrEmpty(FamilyName)) status += $" • {FamilyName}";
                if (IsFavorite) status += " • Favorite";
                return status;
            }, fallbackValue: StatusDisplay, operationName: "FullStatusDisplay");
        }
    }

    /// <summary>
    /// Status badge color with special handling for favorites
    /// </summary>
    public override Color StatusBadgeColor
    {
        get
        {
            return this.SafeExecute(() =>
            {
                if (IsFavorite)
                    return Color.FromArgb("#FF9800"); // Special color for favorites

                return base.StatusBadgeColor;
            }, fallbackValue: base.StatusBadgeColor, operationName: "StatusBadgeColor");
        }
    }

    /// <summary>
    /// Enhanced display name with family context and visual indicators
    /// </summary>
    public new string DisplayName =>
        this.SafeExecute(() =>
        {
            var name = Name;
            if (!string.IsNullOrEmpty(FamilyName))
                name += $" ({FamilyName})";
            if (IsSystemDefault)
                name += " (System)";
            if (IsFavorite)
                name += " ⭐";
            return name;
        }, fallbackValue: Name, operationName: "DisplayName");

    #endregion

    #region Genus-Specific Properties

    /// <summary>
    /// Detect if this genus belongs to orchid family based on family name
    /// </summary>
    public bool IsOrchidGenus =>
        this.SafeExecute(() =>
            !string.IsNullOrEmpty(FamilyName) &&
            FamilyName.Contains("Orchidaceae", StringComparison.OrdinalIgnoreCase),
            fallbackValue: false,
            operationName: "IsOrchidGenus");

    /// <summary>
    /// Genus type indicator icon based on family and status
    /// </summary>
    public string GenusTypeIndicator => IsOrchidGenus ? "🌺" : "🌱";

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
                if (!string.IsNullOrEmpty(FamilyName)) parts.Add($"Family: {FamilyName}");
                if (IsSystemDefault) parts.Add("System default");
                if (!IsActive) parts.Add("Inactive");

                parts.Add($"Created {CreatedAt:dd/MM/yyyy}");

                return string.Join(" • ", parts);
            }, fallbackValue: $"Genus: {Name}", operationName: "TooltipText");
        }
    }

    /// <summary>
    /// Family context display for hierarchical view
    /// </summary>
    public string FamilyContext =>
        this.SafeExecute(() =>
            !string.IsNullOrEmpty(FamilyName) ? $"in {FamilyName}" : "Family unknown",
            fallbackValue: "Family unknown",
            operationName: "FamilyContext");

    /// <summary>
    /// Short family name for compact display
    /// </summary>
    public string ShortFamilyName =>
        this.SafeExecute(() =>
        {
            if (string.IsNullOrEmpty(FamilyName))
                return "N/A";

            // For orchidaceae, show just "Orchidaceae", for others show first word
            if (FamilyName.Contains("Orchidaceae", StringComparison.OrdinalIgnoreCase))
                return "Orchidaceae";

            var firstWord = FamilyName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return firstWord ?? FamilyName;
        }, fallbackValue: "N/A", operationName: "ShortFamilyName");

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
            this.LogInfo($"Retrieved Genus model for: {Name}");
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

            // Favorites first
            if (IsFavorite && !other.IsFavorite) return -1;
            if (!IsFavorite && other.IsFavorite) return 1;

            // Then by family name
            var familyComparison = string.Compare(FamilyName, other.FamilyName, StringComparison.OrdinalIgnoreCase);
            if (familyComparison != 0) return familyComparison;

            // Finally by genus name
            return string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }, fallbackValue: 0, operationName: "CompareTo");
    }

    /// <summary>
    /// Get summary text for quick info display
    /// </summary>
    public string GetSummary()
    {
        return this.SafeExecute(() =>
        {
            var parts = new List<string> { Name };

            if (!string.IsNullOrEmpty(FamilyName))
                parts.Add($"Family: {ShortFamilyName}");

            if (!string.IsNullOrEmpty(Description))
            {
                var shortDesc = Description.Length > 50
                    ? Description.Substring(0, 47) + "..."
                    : Description;
                parts.Add(shortDesc);
            }

            return string.Join(" • ", parts);
        }, fallbackValue: Name, operationName: "GetSummary");
    }

    /// <summary>
    /// Check if this genus matches search criteria
    /// </summary>
    public bool MatchesSearch(string searchText)
    {
        return this.SafeExecute(() =>
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return true;

            var searchLower = searchText.ToLower();

            return Name.ToLower().Contains(searchLower) ||
                   (!string.IsNullOrEmpty(Description) && Description.ToLower().Contains(searchLower)) ||
                   (!string.IsNullOrEmpty(FamilyName) && FamilyName.ToLower().Contains(searchLower));
        }, fallbackValue: false, operationName: "MatchesSearch");
    }

    /// <summary>
    /// Get hierarchical path for breadcrumb navigation
    /// </summary>
    public string GetHierarchicalPath()
    {
        return this.SafeExecute(() =>
        {
            if (!string.IsNullOrEmpty(FamilyName))
                return $"{FamilyName} → {Name}";

            return Name;
        }, fallbackValue: Name, operationName: "GetHierarchicalPath");
    }

    #endregion

    #region Override ToString for Debugging

    /// <summary>
    /// Override ToString for debugging and logging
    /// </summary>
    public override string ToString()
    {
        return this.SafeExecute(() =>
        {
            return $"GenusItemViewModel: {Name} (Family: {FamilyName ?? "Unknown"}, " +
                   $"Active: {IsActive}, Favorite: {IsFavorite}, Selected: {IsSelected})";
        }, fallbackValue: $"GenusItemViewModel: {Name}", operationName: "ToString");
    }

    #endregion
}