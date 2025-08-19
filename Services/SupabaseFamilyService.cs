using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Services.Data;
using OrchidPro.Services.Base;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using OrchidPro.Extensions;

namespace OrchidPro.Services;

/// <summary>
/// Supabase database model representing families table.
/// Maps between database schema and application domain models.
/// </summary>
[Table("families")]
public class SupabaseFamily : BaseModel
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

    /// <summary>
    /// Convert SupabaseFamily to domain Family model
    /// </summary>
    public Family ToFamily()
    {
        return new Family
        {
            Id = this.Id,
            UserId = this.UserId,
            Name = this.Name ?? string.Empty,
            Description = this.Description,
            IsActive = this.IsActive ?? true,
            IsFavorite = this.IsFavorite ?? false,
            CreatedAt = this.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = this.UpdatedAt ?? DateTime.UtcNow
        };
    }

    /// <summary>
    /// Convert domain Family model to SupabaseFamily
    /// </summary>
    public static SupabaseFamily FromFamily(Family family)
    {
        return new SupabaseFamily
        {
            Id = family.Id,
            UserId = family.UserId,
            Name = family.Name,
            Description = family.Description,
            IsActive = family.IsActive,
            IsFavorite = family.IsFavorite,
            CreatedAt = family.CreatedAt,
            UpdatedAt = family.UpdatedAt
        };
    }
}

/// <summary>
/// REFACTORED: Family service using BaseSupabaseEntityService.
/// Reduced from ~150 lines to minimal implementation focused on Family-specific logic.
/// MODERNIZED: Uses primary constructor for cleaner initialization.
/// </summary>
public class SupabaseFamilyService(SupabaseService supabaseService) : BaseSupabaseEntityService<Family, SupabaseFamily>(supabaseService)
{
    protected override string EntityTypeName => "Family";
    protected override string EntityPluralName => "Families";

    protected override Family ConvertToEntity(SupabaseFamily supabaseModel)
        => supabaseModel.ToFamily();

    protected override SupabaseFamily ConvertFromEntity(Family entity)
        => SupabaseFamily.FromFamily(entity);
}