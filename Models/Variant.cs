using OrchidPro.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace OrchidPro.Models;

/// <summary>
/// Variant entity representing independent plant variations that can be applied to any plant.
/// Follows exact pattern of Family/Genus/Species models with base entity functionality.
/// Independent entity - not hierarchical, just like Family model.
/// </summary>
public class Variant : IBaseEntity
{
    #region Base Properties (IBaseEntity - Required)

    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? UserId { get; set; }

    [Required(ErrorMessage = "Variant name is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Variant name must be between 1 and 255 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsFavorite { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? SyncHash { get; set; }

    #endregion

    #region IBaseEntity Implementation

    /// <summary>
    /// Computed property indicating if variant is system default
    /// </summary>
    public bool IsSystemDefault => UserId == null;

    /// <summary>
    /// Display name for UI presentation
    /// </summary>
    public string DisplayName => $"{Name}{(IsFavorite ? " ⭐" : "")}";

    /// <summary>
    /// Status display text for UI
    /// </summary>
    public string StatusDisplay => IsActive ? "Active" : "Inactive";

    /// <summary>
    /// Validates the variant entity and returns list of errors
    /// </summary>
    public bool IsValid(out List<string> errors)
    {
        errors = [];

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Variant name is required");
        else if (Name.Length > 255)
            errors.Add("Variant name cannot exceed 255 characters");

        if (!string.IsNullOrEmpty(Description) && Description.Length > 2000)
            errors.Add("Description cannot exceed 2000 characters");

        return errors.Count == 0;
    }

    /// <summary>
    /// Creates a copy of the variant for editing purposes
    /// </summary>
    public Variant Clone()
    {
        return new Variant
        {
            Id = this.Id,
            UserId = this.UserId,
            Name = this.Name,
            Description = this.Description,
            IsActive = this.IsActive,
            IsFavorite = this.IsFavorite,
            CreatedAt = this.CreatedAt,
            UpdatedAt = this.UpdatedAt,
            SyncHash = this.SyncHash
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

    #region Utility Methods

    /// <summary>
    /// Updates the entity's updated timestamp
    /// </summary>
    public void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns a string representation of the variant
    /// </summary>
    public override string ToString()
    {
        return $"Variant: {Name} (Active: {IsActive})";
    }

    #endregion
}