using OrchidPro.Models;
using OrchidPro.ViewModels.Base;
using OrchidPro.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OrchidPro.ViewModels.Families;

/// <summary>
/// Item ViewModel for botanical family entities with family-specific properties and behaviors.
/// Extends base functionality with favorites, orchid family detection, and enhanced UI binding.
/// </summary>
public partial class FamilyItemViewModel : BaseItemViewModel<Family>
{
    #region Base Class Implementation

    public override string EntityName => "Family";

    #endregion

    #region Family-Specific Properties

    /// <summary>
    /// Favorite status specific to Family entities
    /// </summary>
    public bool IsFavorite { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initialize family item ViewModel with family-specific data
    /// </summary>
    public FamilyItemViewModel(Family family) : base(family)
    {
        IsFavorite = family.IsFavorite;
        this.LogInfo($"Created: {family.Name} (Favorite: {IsFavorite}, ID: {family.Id})");
    }

    #endregion

    #region Enhanced UI Properties

    /// <summary>
    /// Customized description preview for botanical families
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
    /// Recent indicator with family-specific icons including favorites
    /// </summary>
    public override string RecentIndicator =>
        this.SafeExecute(() => IsRecent ? "🌿" : (IsFavorite ? "⭐" : ""),
                        fallbackValue: "",
                        operationName: "RecentIndicator");

    /// <summary>
    /// Extended status display including family-specific indicators
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
                if (IsOrchidaceae) status += " • Orchid";
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
    /// Enhanced display name with visual indicators
    /// </summary>
    public new string DisplayName =>
        this.SafeExecute(() => $"{Name}{(IsSystemDefault ? " (System)" : "")}{(IsFavorite ? " ⭐" : "")}",
                        fallbackValue: Name,
                        operationName: "DisplayName");

    #endregion

    #region Family-Specific Properties

    /// <summary>
    /// Detect if this is an orchid family based on name
    /// </summary>
    public bool IsOrchidaceae =>
        this.SafeExecute(() => Name.Contains("Orchidaceae", StringComparison.OrdinalIgnoreCase),
                        fallbackValue: false,
                        operationName: "IsOrchidaceae");

    /// <summary>
    /// Family type indicator icon
    /// </summary>
    public string FamilyTypeIndicator => IsOrchidaceae ? "🌺" : "🌿";

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
                if (IsOrchidaceae) return "🌺";
                if (IsSystemDefault) return "🔒";
                return "🌿";
            }, fallbackValue: "🌿", operationName: "StatusIcon");
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

                if (IsFavorite) parts.Add("Favorite family");
                if (IsOrchidaceae) parts.Add("Orchid family");
                if (IsSystemDefault) parts.Add("System default");
                if (!IsActive) parts.Add("Inactive");

                parts.Add($"Created {CreatedAt:dd/MM/yyyy}");

                return string.Join(" • ", parts);
            }, fallbackValue: $"Family: {Name}", operationName: "TooltipText");
        }
    }

    #endregion

    #region Data Access and Utility Methods

    /// <summary>
    /// Get the underlying Family model with proper typing
    /// </summary>
    public new Family ToModel()
    {
        return this.SafeExecute(() =>
        {
            var model = base.ToModel();
            this.LogInfo($"Retrieved Family model for: {Name}");
            return model;
        }, fallbackValue: base.ToModel(), operationName: "ToModel");
    }

    /// <summary>
    /// Compare families for sorting with favorites-first logic
    /// </summary>
    public int CompareTo(FamilyItemViewModel? other)
    {
        return this.SafeExecute(() =>
        {
            if (other == null) return 1;

            this.LogInfo($"Comparing {Name} with {other.Name}");

            // Favorites first
            if (IsFavorite && !other.IsFavorite) return -1;
            if (!IsFavorite && other.IsFavorite) return 1;

            // Then by name
            var result = string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
            this.LogInfo($"Comparison result: {result}");
            return result;

        }, fallbackValue: 0, operationName: "CompareTo");
    }

    /// <summary>
    /// Enhanced string representation for debugging
    /// </summary>
    public override string ToString()
    {
        return this.SafeExecute(() =>
            $"FamilyItemVM: {Name} (ID: {Id}, Selected: {IsSelected}, Favorite: {IsFavorite})",
            fallbackValue: $"FamilyItemVM: [Error getting details]",
            operationName: "ToString");
    }

    #endregion
}