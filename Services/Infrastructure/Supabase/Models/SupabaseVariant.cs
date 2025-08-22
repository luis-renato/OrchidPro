using OrchidPro.Models;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace OrchidPro.Services.Infrastructure.Supabase.Models;

[Table("variants")]
public class SupabaseVariant : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; } = true;

    [Column("is_favorite")]
    public bool? IsFavorite { get; set; } = false;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("sync_hash")]
    public string? SyncHash { get; set; }

    public Variant ToVariant()
    {
        return new Variant
        {
            Id = this.Id,
            UserId = this.UserId,
            Name = this.Name ?? string.Empty,
            Description = this.Description,
            IsActive = this.IsActive ?? true,
            IsFavorite = this.IsFavorite ?? false,
            CreatedAt = this.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = this.UpdatedAt ?? DateTime.UtcNow,
            SyncHash = this.SyncHash
        };
    }

    public static SupabaseVariant FromVariant(Variant variant)
    {
        return new SupabaseVariant
        {
            Id = variant.Id,
            UserId = variant.UserId,
            Name = variant.Name,
            Description = variant.Description,
            IsActive = variant.IsActive,
            IsFavorite = variant.IsFavorite,
            CreatedAt = variant.CreatedAt,
            UpdatedAt = variant.UpdatedAt,
            SyncHash = variant.SyncHash
        };
    }
}