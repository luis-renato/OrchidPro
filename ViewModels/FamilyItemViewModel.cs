using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;

namespace OrchidPro.ViewModels;

/// <summary>
/// MIGRADO: ViewModel para itens individuais na lista de famílias
/// Simplificado para arquitetura direta Supabase
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

    // SIMPLIFICADO: Status de sync sempre "synced" na nova arquitetura
    public string SyncStatusDisplay => "✅ Synced";
    public Color SyncStatusColor => Colors.Green;

    // NOVO: Indicadores de conectividade
    public string ConnectivityStatus => "🌐 Online";
    public Color ConnectivityColor => Colors.Green;

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
    /// NOVO: Indicates if item can be edited (requires connection)
    /// </summary>
    public bool CanEdit => true; // Na nova arquitetura sempre conectado quando visível

    /// <summary>
    /// NOVO: Indicates if item can be deleted
    /// </summary>
    public bool CanDelete => !IsSystemDefault;

    /// <summary>
    /// NOVO: Status badge color based on active state
    /// </summary>
    public Color StatusBadgeColor => IsActive ? Colors.Green : Colors.Red;

    /// <summary>
    /// NOVO: Status badge text
    /// </summary>
    public string StatusBadge => IsActive ? "ACTIVE" : "INACTIVE";

    /// <summary>
    /// NOVO: Description preview for UI (truncated)
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
    /// NOVO: Formatted creation date
    /// </summary>
    public string CreatedAtFormatted => CreatedAt.ToString("MMM dd, yyyy");

    /// <summary>
    /// NOVO: Indicates if this is a recent item (created in last 7 days)
    /// </summary>
    public bool IsRecent => DateTime.UtcNow - CreatedAt <= TimeSpan.FromDays(7);

    /// <summary>
    /// NOVO: Recent indicator for UI
    /// </summary>
    public string RecentIndicator => IsRecent ? "🆕" : "";

    /// <summary>
    /// NOVO: Full status display combining multiple indicators
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