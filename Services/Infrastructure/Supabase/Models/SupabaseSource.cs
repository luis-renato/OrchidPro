using OrchidPro.Models;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace OrchidPro.Services.Infrastructure.Supabase.Models;

[Table("sources")]
public class SupabaseSource : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("supplier_type")]
    public string? SupplierType { get; set; }

    [Column("contact_info")]
    public string? ContactInfo { get; set; }

    [Column("website")]
    public string? Website { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; } = true;

    [Column("is_favorite")]
    public bool? IsFavorite { get; set; } = false;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    public Source ToSource()
    {
        return new Source
        {
            Id = this.Id,
            UserId = this.UserId,
            Name = this.Name ?? string.Empty,
            Description = this.Description,
            SupplierType = this.SupplierType,
            ContactInfo = this.ContactInfo,
            Website = this.Website,
            IsActive = this.IsActive ?? true,
            IsFavorite = this.IsFavorite ?? false,
            CreatedAt = this.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = this.UpdatedAt ?? DateTime.UtcNow
        };
    }

    public static SupabaseSource FromSource(Source source)
    {
        return new SupabaseSource
        {
            Id = source.Id,
            UserId = source.UserId,
            Name = source.Name,
            Description = source.Description,
            SupplierType = source.SupplierType,
            ContactInfo = source.ContactInfo,
            Website = source.Website,
            IsActive = source.IsActive,
            IsFavorite = source.IsFavorite,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt
        };
    }
}