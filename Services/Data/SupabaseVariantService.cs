using OrchidPro.Models;
using OrchidPro.Services.Base;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace OrchidPro.Services.Data;

/// <summary>
/// Supabase database model representing variants table.
/// Maps between database schema and application domain models.
/// </summary>
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

    /// <summary>
    /// Convert SupabaseVariant to domain Variant model
    /// </summary>
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

    /// <summary>
    /// Convert domain Variant model to SupabaseVariant
    /// </summary>
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

/// <summary>
/// REFACTORED: Variant service using BaseSupabaseEntityService.
/// Reduced from ~150 lines to minimal implementation focused on Variant-specific logic.
/// MODERNIZED: Uses primary constructor for cleaner initialization.
/// </summary>
public class SupabaseVariantService(SupabaseService supabaseService) : BaseSupabaseEntityService<Variant, SupabaseVariant>(supabaseService)
{
    protected override string EntityTypeName => "Variant";
    protected override string EntityPluralName => "Variants";

    protected override Variant ConvertToEntity(SupabaseVariant supabaseModel)
        => supabaseModel.ToVariant();

    protected override SupabaseVariant ConvertFromEntity(Variant entity)
        => SupabaseVariant.FromVariant(entity);
}