using System.ComponentModel.DataAnnotations;
using OrchidPro.Models.Base;
using Supabase.Postgrest.Attributes;

namespace OrchidPro.Models;

[Table("substrates")]
public class Substrate : IBaseEntity
{
    #region Base Properties (IBaseEntity)

    [PrimaryKey("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("name")]
    [Required(ErrorMessage = "Substrate name is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Substrate name must be between 1 and 255 characters")]
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

    #region Substrate-Specific Properties

    [Column("components")]
    public string? Components { get; set; }

    [Column("ph_range")]
    [StringLength(50, ErrorMessage = "pH range cannot exceed 50 characters")]
    public string? PhRange { get; set; }

    [Column("drainage_level")]
    [StringLength(50, ErrorMessage = "Drainage level cannot exceed 50 characters")]
    public string? DrainageLevel { get; set; }

    [Column("supplier")]
    [StringLength(255, ErrorMessage = "Supplier cannot exceed 255 characters")]
    public string? Supplier { get; set; }

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
            errors.Add("Substrate name is required");

        if (Name?.Length > 255)
            errors.Add("Substrate name cannot exceed 255 characters");

        if (Description?.Length > 2000)
            errors.Add("Description cannot exceed 2000 characters");

        if (PhRange?.Length > 50)
            errors.Add("pH range cannot exceed 50 characters");

        if (DrainageLevel?.Length > 50)
            errors.Add("Drainage level cannot exceed 50 characters");

        if (Supplier?.Length > 255)
            errors.Add("Supplier cannot exceed 255 characters");

        return errors.Count == 0;
    }

    public Substrate Clone()
    {
        return new Substrate
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
            Components = this.Components,
            PhRange = this.PhRange,
            DrainageLevel = this.DrainageLevel,
            Supplier = this.Supplier
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