using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using OrchidPro.Models;

namespace OrchidPro.Services.Infrastructure.Supabase.Models;

[Table("plants")]
public class SupabasePlant : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("species_id")]
    public Guid SpeciesId { get; set; }

    [Column("variant_id")]
    public Guid? VariantId { get; set; }

    [Column("plant_code")]
    public string PlantCode { get; set; } = string.Empty;

    [Column("common_name")]
    public string? CommonName { get; set; }

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

    /// <summary>
    /// Converts SupabasePlant to domain Plant entity
    /// </summary>
    public Plant ToPlant() => new()
    {
        Id = Id,
        UserId = UserId,
        SpeciesId = SpeciesId,
        VariantId = VariantId,
        PlantCode = PlantCode,
        CommonName = CommonName,
        IsActive = IsActive,
        IsFavorite = IsFavorite,
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt,
        SyncHash = SyncHash
    };

    /// <summary>
    /// Creates SupabasePlant from domain Plant entity
    /// </summary>
    public static SupabasePlant FromPlant(Plant plant) => new()
    {
        Id = plant.Id,
        UserId = plant.UserId,
        SpeciesId = plant.SpeciesId,
        VariantId = plant.VariantId,
        PlantCode = plant.PlantCode,
        CommonName = plant.CommonName,
        IsActive = plant.IsActive,
        IsFavorite = plant.IsFavorite,
        CreatedAt = plant.CreatedAt,
        UpdatedAt = plant.UpdatedAt,
        SyncHash = plant.SyncHash
    };
}