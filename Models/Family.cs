using OrchidPro.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace OrchidPro.Models;

/// <summary>
/// Family model - ATUALIZADO: Removido IsSystemDefault
/// ✅ Schema atualizado sem is_system_default
/// </summary>
public class Family : IBaseEntity
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
    /// ✅ REMOVIDO: IsSystemDefault - não existe mais no schema
    /// Agora identificamos dados do sistema por UserId == null
    /// </summary>

    /// <summary>
    /// Indicates if the family is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// ✅ Indicates if this family is marked as favorite by the user
    /// </summary>
    public bool IsFavorite { get; set; } = false;

    /// <summary>
    /// When the family was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the family was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ✅ ATUALIZADO: IsSystemDefault baseado em UserId
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
    /// ✅ Extended status display with favorite indicator
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
    /// Validates the family data
    /// </summary>
    public bool IsValid(out List<string> errors)
    {
        errors = [];

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
            // ✅ REMOVIDO: IsSystemDefault não é mais copiado (é computed property)
            IsActive = this.IsActive,
            IsFavorite = this.IsFavorite,
            CreatedAt = this.CreatedAt,
            UpdatedAt = this.UpdatedAt
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
    /// ✅ Toggle favorite status
    /// </summary>
    public void ToggleFavorite()
    {
        IsFavorite = !IsFavorite;
        UpdatedAt = DateTime.UtcNow;
    }
}