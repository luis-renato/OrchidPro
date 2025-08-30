using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OrchidPro.Resources;
using OrchidPro.Models.Base;

namespace OrchidPro.Models
{
    [Table("events")]
    public class Event : IBaseEntity
    {
        // CAMPOS BASE
        [Column("id")]
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("user_id")]
        [Required]
        public Guid UserId { get; set; }

        [Column("plant_id")]
        [Required]
        public Guid PlantId { get; set; }

        [Column("event_type_id")]
        [Required]
        public Guid EventTypeId { get; set; }

        [Column("title")]
        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        public string? EventDescription { get; set; } // Renamed to avoid conflict

        [Column("scheduled_date")]
        [Required]
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

        // Navigation properties
        public Plant? Plant { get; set; }
        public EventType? EventType { get; set; }
        public List<EventProperty> Properties { get; set; } = new();

        // ===================================
        // IBASEENTITY IMPLEMENTATION - COMPLETE
        // ===================================

        /// <summary>
        /// UserId as nullable for IBaseEntity compatibility
        /// </summary>
        Guid? IBaseEntity.UserId
        {
            get => UserId;
            set => UserId = value ?? Guid.Empty;
        }

        /// <summary>
        /// Name for IBaseEntity - uses Title
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description for IBaseEntity - uses EventDescription
        /// </summary>
        public string? Description
        {
            get => EventDescription;
            set => EventDescription = value;
        }

        /// <summary>
        /// Events are never system defaults
        /// </summary>
        public bool IsSystemDefault => false;

        /// <summary>
        /// Display name for UI presentation
        /// </summary>
        public string DisplayName => $"{Title}{(IsFavorite ? " ⭐" : "")}";

        /// <summary>
        /// Status display for UI presentation
        /// </summary>
        public string StatusDisplay => ActualDate.HasValue ? "Completed"
            : ScheduledDate.Date < DateTime.Today ? "Overdue"
            : ScheduledDate.Date == DateTime.Today ? "Due Today"
            : "Scheduled";

        /// <summary>
        /// Validates the event data
        /// </summary>
        public bool IsValid(out List<string> errors)
        {
            errors = [];

            if (string.IsNullOrWhiteSpace(Title))
                errors.Add("Event title is required");
            else if (Title.Length > 255)
                errors.Add("Event title cannot exceed 255 characters");

            if (PlantId == Guid.Empty)
                errors.Add("Plant is required");

            if (EventTypeId == Guid.Empty)
                errors.Add("Event type is required");

            return errors.Count == 0;
        }

        /// <summary>
        /// Creates a copy of the event for editing
        /// </summary>
        public Event Clone()
        {
            return new Event
            {
                Id = this.Id,
                UserId = this.UserId,
                PlantId = this.PlantId,
                EventTypeId = this.EventTypeId,
                Title = this.Title,
                EventDescription = this.EventDescription,
                Name = this.Name,
                ScheduledDate = this.ScheduledDate,
                ActualDate = this.ActualDate,
                Notes = this.Notes,
                PhotosCount = this.PhotosCount,
                IsActive = this.IsActive,
                IsFavorite = this.IsFavorite,
                CreatedAt = this.CreatedAt,
                UpdatedAt = this.UpdatedAt,
                SyncHash = this.SyncHash
                // Note: Properties are not cloned for performance reasons
            };
        }

        /// <summary>
        /// Implementation of interface: Generic clone
        /// </summary>
        IBaseEntity IBaseEntity.Clone() => Clone();

        // COMPUTED PROPERTIES
        [NotMapped]
        public DateTime EffectiveDate => ActualDate ?? ScheduledDate;

        [NotMapped]
        public bool IsCompleted => ActualDate.HasValue;

        [NotMapped]
        public bool IsOverdue => !ActualDate.HasValue && ScheduledDate.Date < DateTime.Today;

        [NotMapped]
        public bool IsDueToday => !ActualDate.HasValue && ScheduledDate.Date == DateTime.Today;

        [NotMapped]
        public bool IsScheduled => !ActualDate.HasValue && ScheduledDate.Date > DateTime.Today;

        // PROPERTY HELPERS
        public string? GetTextProperty(string key) =>
            Properties?.FirstOrDefault(p => p.Key == key && p.DataType == "text")?.Value;

        public decimal? GetDecimalProperty(string key)
        {
            var prop = Properties?.FirstOrDefault(p => p.Key == key && p.DataType == "decimal");
            return prop != null && decimal.TryParse(prop.Value, out var result) ? result : null;
        }

        public int? GetIntegerProperty(string key)
        {
            var prop = Properties?.FirstOrDefault(p => p.Key == key && p.DataType == "integer");
            return prop != null && int.TryParse(prop.Value, out var result) ? result : null;
        }

        public bool? GetBooleanProperty(string key)
        {
            var prop = Properties?.FirstOrDefault(p => p.Key == key && p.DataType == "boolean");
            return prop != null && bool.TryParse(prop.Value, out var result) ? result : null;
        }

        public void SetProperty(string key, object value, string dataType = "text")
        {
            var existing = Properties?.FirstOrDefault(p => p.Key == key);
            if (existing != null)
            {
                existing.Value = value?.ToString() ?? string.Empty;
                existing.DataType = dataType;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                Properties ??= new List<EventProperty>();
                Properties.Add(new EventProperty
                {
                    EventId = Id,
                    Key = key,
                    Value = value?.ToString() ?? string.Empty,
                    DataType = dataType
                });
            }
        }
    }
}