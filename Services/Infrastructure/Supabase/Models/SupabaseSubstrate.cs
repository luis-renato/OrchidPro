using OrchidPro.Models;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace OrchidPro.Services.Infrastructure.Supabase.Models;

[Table("substrates")]
public class SupabaseSubstrate : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("components")]
    public string? Components { get; set; }

    [Column("ph_range")]
    public string? PhRange { get; set; }

    [Column("drainage_level")]
    public string? DrainageLevel { get; set; }

    [Column("supplier")]
    public string? Supplier { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; } = true;

    [Column("is_favorite")]
    public bool? IsFavorite { get; set; } = false;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    public Substrate ToSubstrate()
    {
        return new Substrate
        {
            Id = this.Id,
            UserId = this.UserId,
            Name = this.Name ?? string.Empty,
            Description = this.Description,
            Components = this.Components,
            PhRange = this.PhRange,
            DrainageLevel = this.DrainageLevel,
            Supplier = this.Supplier,
            IsActive = this.IsActive ?? true,
            IsFavorite = this.IsFavorite ?? false,
            CreatedAt = this.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = this.UpdatedAt ?? DateTime.UtcNow
        };
    }

    public static SupabaseSubstrate FromSubstrate(Substrate substrate)
    {
        return new SupabaseSubstrate
        {
            Id = substrate.Id,
            UserId = substrate.UserId,
            Name = substrate.Name,
            Description = substrate.Description,
            Components = substrate.Components,
            PhRange = substrate.PhRange,
            DrainageLevel = substrate.DrainageLevel,
            Supplier = substrate.Supplier,
            IsActive = substrate.IsActive,
            IsFavorite = substrate.IsFavorite,
            CreatedAt = substrate.CreatedAt,
            UpdatedAt = substrate.UpdatedAt
        };
    }
}
