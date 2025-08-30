using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using OrchidPro.Models;

namespace OrchidPro.Services.Infrastructure.Supabase.Models;

[Table("event_types")]
public class SupabaseEventType : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("name_key")]
    public string? NameKey { get; set; }

    [Column("description_key")]
    public string? DescriptionKey { get; set; }

    [Column("display_name")]
    public string? DisplayName { get; set; }

    [Column("category_key")]
    public string CategoryKey { get; set; } = string.Empty;

    [Column("icon")]
    public string? Icon { get; set; }

    [Column("color")]
    public string? Color { get; set; }

    [Column("is_positive")]
    public bool IsPositive { get; set; } = true;

    [Column("requires_future_date")]
    public bool RequiresFutureDate { get; set; } = false;

    [Column("is_system_default")]
    public bool IsSystemDefault { get; set; } = false;

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
    /// Converts SupabaseEventType to domain EventType entity
    /// </summary>
    public EventType ToEventType() => new()
    {
        Id = Id,
        UserId = UserId,
        NameKey = NameKey,
        DescriptionKey = DescriptionKey,
        DisplayName = DisplayName,
        CategoryKey = CategoryKey,
        Icon = Icon,
        Color = Color,
        IsPositive = IsPositive,
        RequiresFutureDate = RequiresFutureDate,
        IsSystemDefault = IsSystemDefault,
        IsActive = IsActive,
        IsFavorite = IsFavorite,
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt,
        SyncHash = SyncHash
    };

    /// <summary>
    /// Creates SupabaseEventType from domain EventType entity
    /// </summary>
    public static SupabaseEventType FromEventType(EventType eventType) => new()
    {
        Id = eventType.Id,
        UserId = eventType.UserId,
        NameKey = eventType.NameKey,
        DescriptionKey = eventType.DescriptionKey,
        DisplayName = eventType.DisplayName,
        CategoryKey = eventType.CategoryKey,
        Icon = eventType.Icon,
        Color = eventType.Color,
        IsPositive = eventType.IsPositive,
        RequiresFutureDate = eventType.RequiresFutureDate,
        IsSystemDefault = eventType.IsSystemDefault,
        IsActive = eventType.IsActive,
        IsFavorite = eventType.IsFavorite,
        CreatedAt = eventType.CreatedAt,
        UpdatedAt = eventType.UpdatedAt,
        SyncHash = eventType.SyncHash
    };
}
