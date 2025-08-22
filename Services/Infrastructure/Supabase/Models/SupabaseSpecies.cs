using OrchidPro.Models;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace OrchidPro.Services.Infrastructure.Supabase.Models;

[Table("species")]
public class SupabaseSpecies : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("genus_id")]
    public Guid GenusId { get; set; }

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

    [Column("common_name")]
    public string? CommonName { get; set; }

    [Column("cultivation_notes")]
    public string? CultivationNotes { get; set; }

    [Column("habitat_info")]
    public string? HabitatInfo { get; set; }

    [Column("flowering_season")]
    public string? FloweringSeason { get; set; }

    [Column("flower_colors")]
    public string? FlowerColors { get; set; }

    [Column("size_category")]
    public string? SizeCategory { get; set; } = "Medium";

    [Column("rarity_status")]
    public string? RarityStatus { get; set; } = "Common";

    [Column("fragrance")]
    public bool? Fragrance { get; set; } = false;

    [Column("temperature_preference")]
    public string? TemperaturePreference { get; set; }

    [Column("light_requirements")]
    public string? LightRequirements { get; set; }

    [Column("humidity_preference")]
    public string? HumidityPreference { get; set; }

    [Column("growth_habit")]
    public string? GrowthHabit { get; set; }

    [Column("bloom_duration")]
    public string? BloomDuration { get; set; }

    public Species ToSpecies()
    {
        return new Species
        {
            Id = this.Id,
            GenusId = this.GenusId,
            UserId = this.UserId,
            Name = this.Name ?? string.Empty,
            Description = this.Description,
            IsActive = this.IsActive ?? true,
            IsFavorite = this.IsFavorite ?? false,
            CreatedAt = this.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = this.UpdatedAt ?? DateTime.UtcNow,
            CommonName = this.CommonName,
            CultivationNotes = this.CultivationNotes,
            HabitatInfo = this.HabitatInfo,
            FloweringSeason = this.FloweringSeason,
            FlowerColors = this.FlowerColors,
            SizeCategory = this.SizeCategory ?? "Medium",
            RarityStatus = this.RarityStatus ?? "Common",
            Fragrance = this.Fragrance ?? false,
            TemperaturePreference = this.TemperaturePreference,
            LightRequirements = this.LightRequirements,
            HumidityPreference = this.HumidityPreference,
            GrowthHabit = this.GrowthHabit,
            BloomDuration = this.BloomDuration
        };
    }

    public static SupabaseSpecies FromSpecies(Species species)
    {
        return new SupabaseSpecies
        {
            Id = species.Id,
            GenusId = species.GenusId,
            UserId = species.UserId,
            Name = species.Name,
            Description = species.Description,
            IsActive = species.IsActive,
            IsFavorite = species.IsFavorite,
            CreatedAt = species.CreatedAt,
            UpdatedAt = species.UpdatedAt,
            CommonName = species.CommonName,
            CultivationNotes = species.CultivationNotes,
            HabitatInfo = species.HabitatInfo,
            FloweringSeason = species.FloweringSeason,
            FlowerColors = species.FlowerColors,
            SizeCategory = species.SizeCategory,
            RarityStatus = species.RarityStatus,
            Fragrance = species.Fragrance,
            TemperaturePreference = species.TemperaturePreference,
            LightRequirements = species.LightRequirements,
            HumidityPreference = species.HumidityPreference,
            GrowthHabit = species.GrowthHabit,
            BloomDuration = species.BloomDuration
        };
    }
}