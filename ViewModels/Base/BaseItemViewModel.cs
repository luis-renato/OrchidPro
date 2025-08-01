using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models.Base;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels;

/// <summary>
/// Base ViewModel representing individual list items with selection functionality.
/// Provides common properties and behaviors for all entity item ViewModels in the application.
/// </summary>
public abstract partial class BaseItemViewModel<T> : ObservableObject where T : class, IBaseEntity
{
    #region Observable Properties

    [ObservableProperty]
    private bool isSelected;

    #endregion

    #region Public Properties

    public Guid Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public bool IsActive { get; }
    public bool IsSystemDefault { get; }
    public string DisplayName { get; }
    public string StatusDisplay { get; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; }

    /// <summary>
    /// Action callback for selection state changes
    /// </summary>
    public Action<BaseItemViewModel<T>>? SelectionChangedAction { get; set; }

    private readonly T _model;

    /// <summary>
    /// Entity name for logging and display purposes - must be implemented by derived classes
    /// </summary>
    public abstract string EntityName { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initialize base item ViewModel with entity data
    /// </summary>
    protected BaseItemViewModel(T entity)
    {
        // Initialize readonly field and properties first
        _model = entity;
        Id = entity.Id;
        Name = entity.Name;
        Description = entity.Description;
        IsActive = entity.IsActive;
        IsSystemDefault = entity.IsSystemDefault;
        DisplayName = entity.DisplayName;
        StatusDisplay = entity.StatusDisplay;
        CreatedAt = entity.CreatedAt;
        UpdatedAt = entity.UpdatedAt;

        // Then safe logging
        this.SafeExecute(() =>
        {
            this.LogInfo($"Created for {EntityName}: {Name}");
        }, "BaseItemViewModel Constructor");
    }

    #endregion

    #region Data Access

    /// <summary>
    /// Get the underlying model entity
    /// </summary>
    public T ToModel() => _model;

    #endregion

    #region Selection Management

    /// <summary>
    /// Toggle selection state with proper logging and callback execution
    /// </summary>
    [RelayCommand]
    private void ToggleSelection()
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"ToggleSelection called for {EntityName}: {Name}");
            this.LogInfo($"Current IsSelected: {IsSelected}");

            IsSelected = !IsSelected;

            this.LogInfo($"New IsSelected: {IsSelected}");
            this.LogInfo($"SelectionChangedAction is null: {SelectionChangedAction == null}");

            if (SelectionChangedAction != null)
            {
                this.LogInfo($"Executing SelectionChangedAction for {EntityName}: {Name}");
                SelectionChangedAction.Invoke(this);
            }
            else
            {
                this.LogWarning($"SelectionChangedAction is NULL for {EntityName}: {Name}");
            }
        }, "ToggleSelection");
    }

    /// <summary>
    /// Handle IsSelected property changes with callback notification
    /// </summary>
    partial void OnIsSelectedChanged(bool value)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"OnIsSelectedChanged: {EntityName} {Name} -> {value}");

            if (SelectionChangedAction != null)
            {
                this.LogInfo($"Notifying SelectionChangedAction: {EntityName} {Name}");
                SelectionChangedAction.Invoke(this);
            }
        }, "OnIsSelectedChanged");
    }

    #endregion

    #region Virtual Properties for UI Binding

    /// <summary>
    /// Determine if item can be edited based on business rules
    /// </summary>
    public virtual bool CanEdit => true;

    /// <summary>
    /// Determine if item can be deleted - system defaults are typically protected
    /// </summary>
    public virtual bool CanDelete => !IsSystemDefault;

    /// <summary>
    /// Status badge color based on active state
    /// </summary>
    public virtual Color StatusBadgeColor => IsActive ? Colors.Green : Colors.Red;

    /// <summary>
    /// Status badge text for UI display
    /// </summary>
    public virtual string StatusBadge => IsActive ? "ACTIVE" : "INACTIVE";

    /// <summary>
    /// Truncated description preview for list display
    /// </summary>
    public virtual string DescriptionPreview
    {
        get
        {
            return this.SafeExecute(() =>
            {
                if (string.IsNullOrWhiteSpace(Description))
                    return "No description available";

                return Description.Length > 100
                    ? $"{Description.Substring(0, 97)}..."
                    : Description;
            }, fallbackValue: "Description unavailable", operationName: "DescriptionPreview");
        }
    }

    /// <summary>
    /// Formatted creation date for display
    /// </summary>
    public virtual string CreatedAtFormatted =>
        this.SafeExecute(() => CreatedAt.ToString("MMM dd, yyyy"),
                        fallbackValue: "Unknown date",
                        operationName: "CreatedAtFormatted");

    /// <summary>
    /// Determine if item is recent (created within last 7 days)
    /// </summary>
    public virtual bool IsRecent =>
        this.SafeExecute(() => DateTime.UtcNow - CreatedAt <= TimeSpan.FromDays(7),
                        fallbackValue: false,
                        operationName: "IsRecent");

    /// <summary>
    /// Recent indicator emoji for UI display
    /// </summary>
    public virtual string RecentIndicator => IsRecent ? "🆕" : "";

    /// <summary>
    /// Combined status display with multiple indicators
    /// </summary>
    public virtual string FullStatusDisplay
    {
        get
        {
            return this.SafeExecute(() =>
            {
                var status = StatusDisplay;
                if (IsSystemDefault) status += " • System";
                if (IsRecent) status += " • New";
                return status;
            }, fallbackValue: StatusDisplay, operationName: "FullStatusDisplay");
        }
    }

    #endregion

    #region Debug and Utility Methods

    /// <summary>
    /// Debug method to output current selection state and configuration
    /// </summary>
    public virtual void DebugSelection()
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"DEBUG SELECTION for {EntityName} {Name}:");
            this.LogInfo($"    IsSelected: {IsSelected}");
            this.LogInfo($"    SelectionChangedAction: {(SelectionChangedAction != null ? "EXISTS" : "NULL")}");
            this.LogInfo($"    CanEdit: {CanEdit}");
            this.LogInfo($"    CanDelete: {CanDelete}");
        }, "DebugSelection");
    }

    /// <summary>
    /// String representation for debugging purposes
    /// </summary>
    public override string ToString()
    {
        return this.SafeExecute(() =>
            $"{GetType().Name}: {Name} (ID: {Id}, Selected: {IsSelected})",
            fallbackValue: $"{GetType().Name}: [Error getting details]",
            operationName: "ToString");
    }

    #endregion
}