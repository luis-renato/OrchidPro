using System.ComponentModel.DataAnnotations;

namespace OrchidPro.Models;

/// <summary>
/// LIMPO: Modelo Family sem conceitos de sync (arquitetura Supabase direta)
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
    /// Display name for UI purposes
    /// </summary>
    public string DisplayName => $"{Name}{(IsSystemDefault ? " (System)" : "")}";

    /// <summary>
    /// Status display text for UI
    /// </summary>
    public string StatusDisplay => IsActive ? "Active" : "Inactive";

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
            UpdatedAt = this.UpdatedAt
        };
    }
}