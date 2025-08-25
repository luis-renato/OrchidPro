using System.ComponentModel.DataAnnotations;
using OrchidPro.Models.Base;
using Supabase.Postgrest.Attributes;

namespace OrchidPro.Models;

/// <summary>
/// Source entity representing plant acquisition sources and suppliers.
/// CORRIGIDO: Implementa completamente IBaseEntity seguindo padrão Family.cs
/// </summary>
[Table("sources")]
public class Source : IBaseEntity
{
    #region Base Properties (IBaseEntity)

    [PrimaryKey("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("name")]
    [Required(ErrorMessage = "Source name is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Source name must be between 1 and 255 characters")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_favorite")]
    public bool IsFavorite { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("sync_hash")]
    public string? SyncHash { get; set; }

    #endregion

    #region Source-Specific Properties

    [Column("supplier_type")]
    [StringLength(100, ErrorMessage = "Supplier type cannot exceed 100 characters")]
    public string? SupplierType { get; set; }

    [Column("contact_info")]
    public string? ContactInfo { get; set; }

    [Column("website")]
    [StringLength(255, ErrorMessage = "Website cannot exceed 255 characters")]
    [Url(ErrorMessage = "Please enter a valid URL")]
    public string? Website { get; set; }

    #endregion

    #region IBaseEntity Implementation - CORRIGIDO seguindo Family.cs

    /// <summary>
    /// Computed property indicating if entity is system default
    /// </summary>
    public bool IsSystemDefault => UserId == null;

    /// <summary>
    /// Display name for UI presentation - IGUAL ao Family.cs
    /// </summary>
    public string DisplayName => $"{Name}{(IsSystemDefault ? " (System)" : "")}{(IsFavorite ? " ⭐" : "")}";

    /// <summary>
    /// Status display for UI presentation - IGUAL ao Family.cs
    /// </summary>
    public string StatusDisplay => IsActive ? "Active" : "Inactive";

    /// <summary>
    /// Extended status display with favorite indicator - IGUAL ao Family.cs
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
    /// Validates the entity and returns list of errors - IGUAL ao Family.cs
    /// </summary>
    public bool IsValid(out List<string> errors)
    {
        errors = [];

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Source name is required");

        if (Name?.Length > 255)
            errors.Add("Source name cannot exceed 255 characters");

        if (Description?.Length > 2000)
            errors.Add("Description cannot exceed 2000 characters");

        if (SupplierType?.Length > 100)
            errors.Add("Supplier type cannot exceed 100 characters");

        if (!string.IsNullOrEmpty(Website) && !Uri.IsWellFormedUriString(Website, UriKind.Absolute))
            errors.Add("Website must be a valid URL");

        return errors.Count == 0;
    }

    /// <summary>
    /// Creates a copy of the entity for editing purposes - IGUAL ao Family.cs
    /// </summary>
    public Source Clone()
    {
        return new Source
        {
            Id = this.Id,
            UserId = this.UserId,
            Name = this.Name,
            Description = this.Description,
            IsActive = this.IsActive,
            IsFavorite = this.IsFavorite,
            CreatedAt = this.CreatedAt,
            UpdatedAt = this.UpdatedAt,
            SyncHash = this.SyncHash,
            SupplierType = this.SupplierType,
            ContactInfo = this.ContactInfo,
            Website = this.Website
        };
    }

    /// <summary>
    /// Implementation of interface: Generic clone - IGUAL ao Family.cs
    /// </summary>
    IBaseEntity IBaseEntity.Clone()
    {
        return Clone();
    }

    /// <summary>
    /// Toggle favorite status - IGUAL ao Family.cs
    /// </summary>
    public void ToggleFavorite()
    {
        IsFavorite = !IsFavorite;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion
}