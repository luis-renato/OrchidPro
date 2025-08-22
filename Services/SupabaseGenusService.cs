using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Services.Data;
using OrchidPro.Services.Base;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using OrchidPro.Extensions;

namespace OrchidPro.Services;

/// <summary>
/// Supabase database model representing genera table with family relationship.
/// Maps between database schema and application domain models.
/// </summary>
[Table("genera")]
public class SupabaseGenus : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("family_id")]
    public Guid FamilyId { get; set; }

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
    /// Convert SupabaseGenus to domain Genus model
    /// </summary>
    public Genus ToGenus()
    {
        return new Genus
        {
            Id = this.Id,
            FamilyId = this.FamilyId,
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
    /// Convert domain Genus model to SupabaseGenus
    /// </summary>
    public static SupabaseGenus FromGenus(Genus genus)
    {
        return new SupabaseGenus
        {
            Id = genus.Id,
            FamilyId = genus.FamilyId,
            UserId = genus.UserId,
            Name = genus.Name,
            Description = genus.Description,
            IsActive = genus.IsActive,
            IsFavorite = genus.IsFavorite,
            CreatedAt = genus.CreatedAt,
            UpdatedAt = genus.UpdatedAt
        };
    }
}

/// <summary>
/// REFACTORED: Genus service using BaseSupabaseEntityService.
/// Reduced from ~150 lines to minimal implementation focused on Genus-specific logic.
/// </summary>
public class SupabaseGenusService(SupabaseService supabaseService) : BaseSupabaseEntityService<Genus, SupabaseGenus>(supabaseService)
{
    protected override string EntityTypeName => "Genus";
    protected override string EntityPluralName => "Genera";

    protected override Genus ConvertToEntity(SupabaseGenus supabaseModel)
        => supabaseModel.ToGenus();

    protected override SupabaseGenus ConvertFromEntity(Genus entity)
        => SupabaseGenus.FromGenus(entity);
}