using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using OrchidPro.Models;

namespace OrchidPro.Services.Infrastructure.Supabase.Models;

[Table("events")]
public class SupabaseEvent : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("plant_id")]
    public Guid PlantId { get; set; }

    [Column("event_type_id")]
    public Guid EventTypeId { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("scheduled_date")]
    public DateTime ScheduledDate { get; set; }

    [Column("actual_date")]
    public DateTime? ActualDate { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("photos_count")]
    public int PhotosCount { get; set; } = 0;

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
    /// Converts SupabaseEvent to domain Event entity
    /// </summary>
    public Event ToEvent() => new()
    {
        Id = Id,
        UserId = UserId,
        PlantId = PlantId,
        EventTypeId = EventTypeId,
        Title = Title,
        Description = Description,
        ScheduledDate = ScheduledDate,
        ActualDate = ActualDate,
        Notes = Notes,
        PhotosCount = PhotosCount,
        IsActive = IsActive,
        IsFavorite = IsFavorite,
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt,
        SyncHash = SyncHash
    };

    /// <summary>
    /// Creates SupabaseEvent from domain Event entity
    /// </summary>
    public static SupabaseEvent FromEvent(Event eventEntity) => new()
    {
        Id = eventEntity.Id,
        UserId = eventEntity.UserId,
        PlantId = eventEntity.PlantId,
        EventTypeId = eventEntity.EventTypeId,
        Title = eventEntity.Title,
        Description = eventEntity.Description,
        ScheduledDate = eventEntity.ScheduledDate,
        ActualDate = eventEntity.ActualDate,
        Notes = eventEntity.Notes,
        PhotosCount = eventEntity.PhotosCount,
        IsActive = eventEntity.IsActive,
        IsFavorite = eventEntity.IsFavorite,
        CreatedAt = eventEntity.CreatedAt,
        UpdatedAt = eventEntity.UpdatedAt,
        SyncHash = eventEntity.SyncHash
    };
}
