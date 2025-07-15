using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;

namespace OrchidPro.ViewModels;

/// <summary>
/// LIMPO: ViewModel para itens individuais na lista de famílias (sem sync)
/// </summary>
public partial class FamilyItemViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isSelected;

    public Guid Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public bool IsActive { get; }
    public bool IsSystemDefault { get; }
    public string DisplayName { get; }
    public string StatusDisplay { get; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; }

    public IRelayCommand<FamilyItemViewModel>? SelectionChangedCommand { get; set; }

    private readonly Family _model;

    public FamilyItemViewModel(Family family)
    {
        _model = family;
        Id = family.Id;
        Name = family.Name;
        Description = family.Description;
        IsActive = family.IsActive;
        IsSystemDefault = family.IsSystemDefault;
        DisplayName = family.DisplayName;
        StatusDisplay = family.StatusDisplay;
        CreatedAt = family.CreatedAt;
        UpdatedAt = family.UpdatedAt;
    }

    /// <summary>
    /// Gets the underlying model
    /// </summary>
    public Family ToModel() => _model;

    /// <summary>
    /// Toggles selection state
    /// </summary>
    [RelayCommand]
    private void ToggleSelection()
    {
        IsSelected = !IsSelected;
        SelectionChangedCommand?.Execute(this);
    }

    /// <summary>
    /// Indicates if item can be edited
    /// </summary>
    public bool CanEdit => true;

    /// <summary>
    /// Indicates if item can be deleted
    /// </summary>
    public bool CanDelete => !IsSystemDefault;

    /// <summary>
    /// Status badge color based on active state
    /// </summary>
    public Color StatusBadgeColor => IsActive ? Colors.Green : Colors.Red;

    /// <summary>
    /// Status badge text
    /// </summary>
    public string StatusBadge => IsActive ? "ACTIVE" : "INACTIVE";

    /// <summary>
    /// Description preview for UI (truncated)
    /// </summary>
    public string DescriptionPreview
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Description))
                return "No description available";

            return Description.Length > 100
                ? $"{Description.Substring(0, 97)}..."
                : Description;
        }
    }

    /// <summary>
    /// Formatted creation date
    /// </summary>
    public string CreatedAtFormatted => CreatedAt.ToString("MMM dd, yyyy");

    /// <summary>
    /// Indicates if this is a recent item (created in last 7 days)
    /// </summary>
    public bool IsRecent => DateTime.UtcNow - CreatedAt <= TimeSpan.FromDays(7);

    /// <summary>
    /// Recent indicator for UI
    /// </summary>
    public string RecentIndicator => IsRecent ? "🆕" : "";

    /// <summary>
    /// Full status display combining multiple indicators
    /// </summary>
    public string FullStatusDisplay
    {
        get
        {
            var status = StatusDisplay;
            if (IsSystemDefault) status += " • System";
            if (IsRecent) status += " • New";
            return status;
        }
    }
}