using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using System.Diagnostics;

namespace OrchidPro.ViewModels;

/// <summary>
/// CORRIGIDO: ViewModel para itens individuais com debug de seleção
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

        Debug.WriteLine($"🔨 [FAMILY_ITEM_VM] Created for: {Name}");
    }

    /// <summary>
    /// Gets the underlying model
    /// </summary>
    public Family ToModel() => _model;

    /// <summary>
    /// ✅ CORRIGIDO: Toggles selection state com debug detalhado
    /// </summary>
    [RelayCommand]
    private void ToggleSelection()
    {
        Debug.WriteLine($"🔘 [FAMILY_ITEM_VM] ToggleSelection called for: {Name}");
        Debug.WriteLine($"🔘 [FAMILY_ITEM_VM] Current IsSelected: {IsSelected}");

        IsSelected = !IsSelected;

        Debug.WriteLine($"🔘 [FAMILY_ITEM_VM] New IsSelected: {IsSelected}");
        Debug.WriteLine($"🔘 [FAMILY_ITEM_VM] SelectionChangedCommand is null: {SelectionChangedCommand == null}");

        if (SelectionChangedCommand != null)
        {
            Debug.WriteLine($"🔘 [FAMILY_ITEM_VM] Executing SelectionChangedCommand for: {Name}");
            SelectionChangedCommand.Execute(this);
        }
        else
        {
            Debug.WriteLine($"❌ [FAMILY_ITEM_VM] SelectionChangedCommand is NULL for: {Name}");
        }
    }

    /// <summary>
    /// ✅ NOVO: Observer da propriedade IsSelected
    /// </summary>
    partial void OnIsSelectedChanged(bool value)
    {
        Debug.WriteLine($"🔄 [FAMILY_ITEM_VM] OnIsSelectedChanged: {Name} -> {value}");

        // Notificar comando se existir
        if (SelectionChangedCommand != null)
        {
            Debug.WriteLine($"🔄 [FAMILY_ITEM_VM] Notifying SelectionChangedCommand: {Name}");
            SelectionChangedCommand.Execute(this);
        }
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

    /// <summary>
    /// ✅ NOVO: Método para debug de seleção
    /// </summary>
    public void DebugSelection()
    {
        Debug.WriteLine($"🔍 [FAMILY_ITEM_VM] DEBUG SELECTION for {Name}:");
        Debug.WriteLine($"    IsSelected: {IsSelected}");
        Debug.WriteLine($"    SelectionChangedCommand: {(SelectionChangedCommand != null ? "EXISTS" : "NULL")}");
        Debug.WriteLine($"    CanEdit: {CanEdit}");
        Debug.WriteLine($"    CanDelete: {CanDelete}");
    }
}