using OrchidPro.Models;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace OrchidPro.Services.Infrastructure.Supabase.Models;

[Table("locations")]
public class SupabaseLocation : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("location_type")]
    public string? LocationType { get; set; }

    [Column("environment_notes")]
    public string? EnvironmentNotes { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; } = true;

    [Column("is_favorite")]
    public bool? IsFavorite { get; set; } = false;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    public PlantLocation ToPlantLocation()
    {
        return new PlantLocation
        {
            Id = this.Id,
            UserId = this.UserId,
            Name = this.Name ?? string.Empty,
            Description = this.Description,
            LocationType = this.LocationType,
            EnvironmentNotes = this.EnvironmentNotes,
            IsActive = this.IsActive ?? true,
            IsFavorite = this.IsFavorite ?? false,
            CreatedAt = this.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = this.UpdatedAt ?? DateTime.UtcNow
        };
    }

    public static SupabaseLocation FromPlantLocation(PlantLocation location)
    {
        return new SupabaseLocation
        {
            Id = location.Id,
            UserId = location.UserId,
            Name = location.Name,
            Description = location.Description,
            LocationType = location.LocationType,
            EnvironmentNotes = location.EnvironmentNotes,
            IsActive = location.IsActive,
            IsFavorite = location.IsFavorite,
            CreatedAt = location.CreatedAt,
            UpdatedAt = location.UpdatedAt
        };
    }
}