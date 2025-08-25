using System.ComponentModel.DataAnnotations;
using OrchidPro.Models.Base;
using Supabase.Postgrest.Attributes;

namespace OrchidPro.Models;

[Table("mounts")]
public class Mount : IBaseEntity
{
    #region Base Properties (IBaseEntity)

    [PrimaryKey("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("name")]
    [Required(ErrorMessage = "Mount name is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Mount name must be between 1 and 255 characters")]
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

    #region Mount-Specific Properties

    [Column("material")]
    [StringLength(100, ErrorMessage = "Material cannot exceed 100 characters")]
    public string? Material { get; set; }

    [Column("size")]
    [StringLength(50, ErrorMessage = "Size cannot exceed 50 characters")]
    public string? Size { get; set; }

    [Column("drainage_type")]
    [StringLength(100, ErrorMessage = "Drainage type cannot exceed 100 characters")]
    public string? DrainageType { get; set; }

    #endregion

    #region IBaseEntity Implementation

    public bool IsSystemDefault => UserId == null;

    public string DisplayName => $"{Name}{(IsSystemDefault ? " (System)" : "")}{(IsFavorite ? " ⭐" : "")}";

    public string StatusDisplay => IsActive ? "Active" : "Inactive";

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

    public bool IsValid(out List<string> errors)
    {
        errors = [];

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Mount name is required");

        if (Name?.Length > 255)
            errors.Add("Mount name cannot exceed 255 characters");

        if (Description?.Length > 2000)
            errors.Add("Description cannot exceed 2000 characters");

        if (Material?.Length > 100)
            errors.Add("Material cannot exceed 100 characters");

        if (Size?.Length > 50)
            errors.Add("Size cannot exceed 50 characters");

        if (DrainageType?.Length > 100)
            errors.Add("Drainage type cannot exceed 100 characters");

        return errors.Count == 0;
    }

    public Mount Clone()
    {
        return new Mount
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
            Material = this.Material,
            Size = this.Size,
            DrainageType = this.DrainageType
        };
    }

    IBaseEntity IBaseEntity.Clone() => Clone();

    public void ToggleFavorite()
    {
        IsFavorite = !IsFavorite;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion
}