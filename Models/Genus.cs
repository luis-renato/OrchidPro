using OrchidPro.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace OrchidPro.Models;

/// <summary>
/// Genus model - seguindo exatamente o padrão de Family
/// Representa um gênero botânico dentro de uma família
/// </summary>
public class Genus : IBaseEntity
{
    /// <summary>
    /// Unique identifier for the genus
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the family this genus belongs to
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
    /// Navigation property to the parent family
    /// </summary>
    public Family? Family { get; set; }

    /// <summary>
    /// Computed property indicating if genus is system default
    /// Based on UserId == null instead of database field
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
    /// Validates the genus and returns list of errors
    /// </summary>
    /// <param name="errors">List of validation errors found</param>
    /// <returns>True if valid, false if validation errors exist</returns>
    public bool IsValid(out List<string> errors)
    {
        errors = new List<string>();

        // Name validation
        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Genus name is required");
        }
        else if (Name.Length > 255)
        {
            errors.Add("Genus name cannot exceed 255 characters");
        }

        // FamilyId validation
        if (FamilyId == Guid.Empty)
        {
            errors.Add("Family is required");
        }

        // Description validation
        if (!string.IsNullOrEmpty(Description) && Description.Length > 2000)
        {
            errors.Add("Description cannot exceed 2000 characters");
        }

        return errors.Count == 0;
    }

    /// <summary>
    /// Creates a copy of the genus for editing purposes
    /// </summary>
    /// <returns>Cloned genus instance</returns>
    public IBaseEntity Clone()
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
    /// Updates this genus with values from another genus
    /// </summary>
    /// <param name="other">Source genus to copy values from</param>
    public void UpdateFrom(Genus other)
    {
        if (other == null) return;

        Name = other.Name;
        Description = other.Description;
        FamilyId = other.FamilyId;
        IsActive = other.IsActive;
        IsFavorite = other.IsFavorite;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets genus summary for display purposes
    /// </summary>
    public string GetSummary()
    {
        var parts = new List<string> { Name };

        if (Family != null)
        {
            parts.Add($"Family: {Family.Name}");
        }

        if (!string.IsNullOrEmpty(Description))
        {
            var shortDesc = Description.Length > 50
                ? Description.Substring(0, 47) + "..."
                : Description;
            parts.Add(shortDesc);
        }

        return string.Join(" • ", parts);
    }

    /// <summary>
    /// Override ToString for debugging
    /// </summary>
    public override string ToString()
    {
        return $"Genus: {Name} (Family: {Family?.Name ?? "Unknown"}, Active: {IsActive})";
    }
}