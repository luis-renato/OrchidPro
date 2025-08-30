using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OrchidPro.Models.Base;

namespace OrchidPro.Models
{
    [Table("event_properties")]
    public class EventProperty : IBaseEntity
    {
        [Column("id")]
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("event_id")]
        [Required]
        public Guid EventId { get; set; }

        [Column("property_key")]
        [Required]
        [StringLength(50)]
        public string Key { get; set; } = string.Empty;

        [Column("property_value")]
        [Required]
        public string Value { get; set; } = string.Empty;

        [Column("data_type")]
        [Required]
        [StringLength(20)]
        public string DataType { get; set; } = "text";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Event? Event { get; set; }

        // ===================================
        // IBASEENTITY IMPLEMENTATION - COMPLETE
        // ===================================

        /// <summary>
        /// UserId - EventProperty doesn't have direct user ownership
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Name for IBaseEntity - uses Key
        /// </summary>
        public string Name
        {
            get => Key;
            set => Key = value;
        }

        /// <summary>
        /// Description for IBaseEntity - formatted key-value description
        /// </summary>
        public string? Description
        {
            get => $"{Key}: {Value} ({DataType})";
            set { } // Read-only computed description
        }

        /// <summary>
        /// EventProperty is always active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// EventProperty can be favorited
        /// </summary>
        public bool IsFavorite { get; set; } = false;

        /// <summary>
        /// Sync hash for offline sync
        /// </summary>
        public string? SyncHash { get; set; }

        /// <summary>
        /// EventProperties are never system defaults
        /// </summary>
        public bool IsSystemDefault => false;

        /// <summary>
        /// Display name for UI presentation
        /// </summary>
        public string DisplayName => $"{Key} = {Value}{(IsFavorite ? " ⭐" : "")}";

        /// <summary>
        /// Status display for UI presentation
        /// </summary>
        public string StatusDisplay => DataType.ToUpper();

        /// <summary>
        /// Validates the event property data
        /// </summary>
        public bool IsValid(out List<string> errors)
        {
            errors = [];

            if (string.IsNullOrWhiteSpace(Key))
                errors.Add("Property key is required");
            else if (Key.Length > 50)
                errors.Add("Property key cannot exceed 50 characters");

            if (string.IsNullOrWhiteSpace(Value))
                errors.Add("Property value is required");

            if (EventId == Guid.Empty)
                errors.Add("Event is required");

            var validDataTypes = new[] { "text", "integer", "decimal", "boolean", "date" };
            if (!validDataTypes.Contains(DataType.ToLower()))
                errors.Add("Data type must be one of: text, integer, decimal, boolean, date");

            return errors.Count == 0;
        }

        /// <summary>
        /// Creates a copy of the event property for editing
        /// </summary>
        public EventProperty Clone()
        {
            return new EventProperty
            {
                Id = this.Id,
                EventId = this.EventId,
                Key = this.Key,
                Value = this.Value,
                DataType = this.DataType,
                UserId = this.UserId,
                IsActive = this.IsActive,
                IsFavorite = this.IsFavorite,
                CreatedAt = this.CreatedAt,
                UpdatedAt = this.UpdatedAt,
                SyncHash = this.SyncHash
            };
        }

        /// <summary>
        /// Implementation of interface: Generic clone
        /// </summary>
        IBaseEntity IBaseEntity.Clone() => Clone();

        /// <summary>
        /// Toggle favorite status
        /// </summary>
        public void ToggleFavorite()
        {
            IsFavorite = !IsFavorite;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}