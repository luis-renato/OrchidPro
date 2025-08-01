using OrchidPro.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace OrchidPro.Models;

/// <summary>
/// Genus model following exact pattern from Family model
/// Represents botanical genera with family relationship
/// </summary>
public class Genus : IBaseEntity
{
    /// <summary>
    /// Unique identifier for the genus
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User who owns this genus record (null for system defaults)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Family this genus belongs to
    /// </summary>
    [Required(ErrorMessage = "Family is required")]
    public Guid FamilyId { get; set; }

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

    #region Navigation Properties

    /// <summary>
    /// Navigation property to the family this genus belongs to
    /// </summary>
    public Family? Family { get; set; }

    /// <summary>
    /// Family name for display purposes (loaded from related Family)
    /// </summary>
    public string FamilyName { get; set; } = string.Empty;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Computed property indicating if genus is system default
    /// Based on UserId == null following Family pattern
    /// </summary>
    public bool IsSystemDefault => UserId == null;

    /// <summary>
    /// Display name for UI purposes
    /// </summary>
    public string DisplayName => $"{Name}{(IsSystemDefault ? " (System)" : "")}{(IsFavorite ? " ⭐" : "")}";

    /// <summary>
    /// Status display text for UI
    /// </summary>
    public string StatusDisplay => IsActive ? "Active" : "Inactive";

    /// <summary>
    /// Extended status display with favorite indicator
    /// </summary>
    public string FullStatusDisplay
    {
        get
        {
            var status = StatusDisplay;
            if (IsSystemDefault) status += " • System";
            if (IsFavorite) status += " • Favorite";
            return status;
        }
    }

    /// <summary>
    /// Full display name with family context
    /// </summary>
    public string FullDisplayName => $"{Name} ({FamilyName})";

    #endregion

    #region IBaseEntity Implementation

    /// <summary>
    /// Validates the genus data
    /// </summary>
    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();

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
            UserId = this.UserId,
            FamilyId = this.FamilyId,
            Name = this.Name,
            Description = this.Description,
            IsActive = this.IsActive,
            IsFavorite = this.IsFavorite,
            CreatedAt = this.CreatedAt,
            UpdatedAt = this.UpdatedAt,
            FamilyName = this.FamilyName,
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
}