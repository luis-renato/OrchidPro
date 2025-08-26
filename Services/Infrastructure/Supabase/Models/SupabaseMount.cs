using OrchidPro.Models;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.ComponentModel;

namespace OrchidPro.Services.Infrastructure.Supabase.Models;

[Table("mounts")]
public class SupabaseMount : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("material")]
    public string? Material { get; set; }

    [Column("size")]
    public string? Size { get; set; }

    [Column("drainage_type")]
    public string? DrainageType { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; } = true;

    [Column("is_favorite")]
    public bool? IsFavorite { get; set; } = false;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    public Mount ToMount()
    {
        return new Mount
        {
            Id = this.Id,
            UserId = this.UserId,
            Name = this.Name ?? string.Empty,
            Description = this.Description,
            Material = this.Material,
            Size = this.Size,
            DrainageType = this.DrainageType,
            IsActive = this.IsActive ?? true,
            IsFavorite = this.IsFavorite ?? false,
            CreatedAt = this.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = this.UpdatedAt ?? DateTime.UtcNow
        };
    }

    public static SupabaseMount FromMount(Mount Mount)
    {
        return new SupabaseMount
        {
            Id = Mount.Id,
            UserId = Mount.UserId,
            Name = Mount.Name,
            Description = Mount.Description,
            Material = Mount.Material,
            Size = Mount.Size,
            DrainageType = Mount.DrainageType,
            IsActive = Mount.IsActive,
            IsFavorite = Mount.IsFavorite,
            CreatedAt = Mount.CreatedAt,
            UpdatedAt = Mount.UpdatedAt
        };
    }
}