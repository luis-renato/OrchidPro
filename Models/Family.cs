using System.ComponentModel.DataAnnotations;

namespace OrchidPro.Models;

/// <summary>
/// Represents a botanical family entity with user isolation and sync support
/// </summary>
public class Family
{
    /// <summary>
    /// Unique identifier for the family
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User who owns this family record (null for system defaults)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Name of the botanical family
    /// </summary>
    [Required(ErrorMessage = "Family name is required")]
    [StringLength(255, ErrorMessage = "Family name cannot exceed 255 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the family
    /// </summary>
    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Indicates if this is a system default family
    /// </summary>
    public bool IsSystemDefault { get; set; } = false;

    /// <summary>
    /// Indicates if the family is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the family was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the family was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Synchronization status with Supabase
    /// </summary>
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Local;

    /// <summary>
    /// When the family was last synchronized
    /// </summary>
    public DateTime? LastSyncAt { get; set; }

    /// <summary>
    /// Hash for conflict detection during sync
    /// </summary>
    public string? SyncHash { get; set; }

    /// <summary>
    /// Display name for UI purposes
    /// </summary>
    public string DisplayName => $"{Name}{(IsSystemDefault ? " (System)" : "")}";

    /// <summary>
    /// Status display text for UI
    /// </summary>
    public string StatusDisplay => IsActive ? "Active" : "Inactive";

    /// <summary>
    /// Sync status display text for UI
    /// </summary>
    public string SyncStatusDisplay => SyncStatus switch
    {
        SyncStatus.Synced => "✅ Synced",
        SyncStatus.Local => "📱 Local",
        SyncStatus.Pending => "⏳ Pending",
        SyncStatus.Error => "❌ Error",
        _ => "❓ Unknown"
    };

    /// <summary>
    /// Color for sync status indicator
    /// </summary>
    public Color SyncStatusColor => SyncStatus switch
    {
        SyncStatus.Synced => Colors.Green,
        SyncStatus.Local => Colors.Orange,
        SyncStatus.Pending => Colors.Blue,
        SyncStatus.Error => Colors.Red,
        _ => Colors.Gray
    };

    /// <summary>
    /// Validates the family data
    /// </summary>
    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Family name is required");

        if (Name?.Length > 255)
            errors.Add("Family name cannot exceed 255 characters");

        if (Description?.Length > 2000)
            errors.Add("Description cannot exceed 2000 characters");

        return errors.Count == 0;
    }

    /// <summary>
    /// Creates a copy of the family for editing
    /// </summary>
    public Family Clone()
    {
        return new Family
        {
            Id = this.Id,
            UserId = this.UserId,
            Name = this.Name,
            Description = this.Description,
            IsSystemDefault = this.IsSystemDefault,
            IsActive = this.IsActive,
            CreatedAt = this.CreatedAt,
            UpdatedAt = this.UpdatedAt,
            SyncStatus = this.SyncStatus,
            LastSyncAt = this.LastSyncAt,
            SyncHash = this.SyncHash
        };
    }
}

/// <summary>
/// Enumeration for synchronization status
/// </summary>
public enum SyncStatus
{
    /// <summary>
    /// Only exists locally, not yet synced
    /// </summary>
    Local = 0,

    /// <summary>
    /// Successfully synchronized with server
    /// </summary>
    Synced = 1,

    /// <summary>
    /// Waiting to be synchronized
    /// </summary>
    Pending = 2,

    /// <summary>
    /// Error occurred during synchronization
    /// </summary>
    Error = 3,

    /// <summary>
    /// Marked for deletion
    /// </summary>
    Deleted = 4
}