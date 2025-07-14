using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;

namespace OrchidPro.ViewModels;

/// <summary>
/// ViewModel for individual family items in the list
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
    public string SyncStatusDisplay { get; }
    public Color SyncStatusColor { get; }
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
        SyncStatusDisplay = family.SyncStatusDisplay;
        SyncStatusColor = family.SyncStatusColor;
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
}