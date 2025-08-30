using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
using OrchidPro.Models;

namespace OrchidPro.Services.Infrastructure.Supabase.Models;

[Table("event_properties")]
public class SupabaseEventProperty : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("event_id")]
    public Guid EventId { get; set; }

    [Column("property_key")]
    public string Key { get; set; } = string.Empty;

    [Column("property_value")]
    public string Value { get; set; } = string.Empty;

    [Column("data_type")]
    public string DataType { get; set; } = "text";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Converts SupabaseEventProperty to domain EventProperty entity
    /// </summary>
    public EventProperty ToEventProperty() => new()
    {
        Id = Id,
        EventId = EventId,
        Key = Key,
        Value = Value,
        DataType = DataType,
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt
    };

    /// <summary>
    /// Creates SupabaseEventProperty from domain EventProperty entity
    /// </summary>
    public static SupabaseEventProperty FromEventProperty(EventProperty property) => new()
    {
        Id = property.Id,
        EventId = property.EventId,
        Key = property.Key,
        Value = property.Value,
        DataType = property.DataType,
        CreatedAt = property.CreatedAt,
        UpdatedAt = property.UpdatedAt
    };
}