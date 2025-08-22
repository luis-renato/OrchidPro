using OrchidPro.Models.Base;
using OrchidPro.Services.Base;
using System.ComponentModel.DataAnnotations;

namespace OrchidPro.Models;

/// <summary>
/// Genus model implementing IHierarchicalEntity for automatic hierarchical repository support.
/// Represents a botanical genus within a family with full hierarchical functionality.
/// </summary>
public class Genus : IBaseEntity, IHierarchicalEntity<Family>
{
    /// <summary>
    /// Unique identifier for the genus
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the family this genus belongs to (implements IHierarchicalEntity)
    /// </summary>
    [Required(ErrorMessage = "Family is required")]
    public Guid FamilyId { get; set; }

    /// <summary>
    /// User who owns this genus record (null for system defaults)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Name of the botanical genus
    /// </summary>
    [Required(ErrorMessage = "Genus name is required")]
    [StringLength(255, ErrorMessage = "Genus name cannot exceed 255 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the genus
    /// </summary>
    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Indicates if the genus is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Indicates if this genus is marked as favorite by the user
    /// </summary>
    public bool IsFavorite { get; set; } = false;

    /// <summary>
    /// When the genus was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the genus was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the parent family (implements IHierarchicalEntity)
    /// </summary>
    public Family? Family { get; set; }

    #region IBaseEntity Implementation

    /// <summary>
    /// Computed property indicating if genus is system default
    /// </summary>
    public bool IsSystemDefault => UserId == null;

    /// <summary>
    /// Display name for UI presentation including family context
    /// </summary>
    public string DisplayName => $"{Name}{(Family != null ? $" ({Family.Name})" : "")}{(IsFavorite ? " ⭐" : "")}";

    /// <summary>
    /// Status display text for UI
    /// </summary>
    public string StatusDisplay => IsActive ? "Active" : "Inactive";

    /// <summary>
    /// Validates the genus data
    /// </summary>
    public bool IsValid(out List<string> errors)
    {
        errors = [];

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Genus name is required");

        if (Name?.Length > 255)
            errors.Add("Genus name cannot exceed 255 characters");

        if (Description?.Length > 2000)
            errors.Add("Description cannot exceed 2000 characters");

        if (FamilyId == Guid.Empty)
            errors.Add("Family is required");

        return errors.Count == 0;
    }

    /// <summary>
    /// Creates a copy of the genus for editing
    /// </summary>
    public Genus Clone()
    {
        return new Genus
        {
            Id = this.Id,
            FamilyId = this.FamilyId,
            UserId = this.UserId,
            Name = this.Name,
            Description = this.Description,
            IsActive = this.IsActive,
            IsFavorite = this.IsFavorite,
            CreatedAt = this.CreatedAt,
            UpdatedAt = this.UpdatedAt,
            Family = this.Family
        };
    }

    /// <summary>
    /// Implementation of interface: Generic clone
    /// </summary>
    IBaseEntity IBaseEntity.Clone()
    {
        return Clone();
    }

    /// <summary>
    /// Toggle favorite status
    /// </summary>
    public void ToggleFavorite()
    {
        IsFavorite = !IsFavorite;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion

    #region IHierarchicalEntity<Family> Implementation

    /// <summary>
    /// Parent identifier for hierarchical operations
    /// </summary>
    public Guid ParentId
    {
        get => FamilyId;
        set => FamilyId = value;
    }

    /// <summary>
    /// Parent entity for hierarchical operations
    /// </summary>
    public Family? Parent
    {
        get => Family;
        set => Family = value;
    }

    /// <summary>
    /// Get the parent identifier value
    /// </summary>
    public Guid GetParentId() => FamilyId;

    /// <summary>
    /// Set the parent identifier value
    /// </summary>
    public void SetParentId(Guid parentId) => FamilyId = parentId;

    /// <summary>
    /// Get display name for the parent family
    /// </summary>
    public string GetParentDisplayName() => Family?.Name ?? "Unknown Family";

    /// <summary>
    /// Validate that the hierarchical relationship is properly configured
    /// </summary>
    public bool ValidateHierarchy()
    {
        return FamilyId != Guid.Empty;
    }

    #endregion
}