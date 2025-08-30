using OrchidPro.Models.Base;
using OrchidPro.Resources;
using OrchidPro.Resources.Strings;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrchidPro.Models
{
    [Table("event_types")]
    public class EventType : IBaseEntity
    {
        [Column("id")]
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("user_id")]
        public Guid? UserId { get; set; }

        [Column("name_key")]
        [StringLength(100)]
        public string? NameKey { get; set; }

        [Column("description_key")]
        [StringLength(100)]
        public string? DescriptionKey { get; set; }

        [Column("display_name")]
        [StringLength(255)]
        public string? DisplayName { get; set; }

        [Column("category_key")]
        [Required]
        [StringLength(50)]
        public string CategoryKey { get; set; } = string.Empty;

        [Column("icon")]
        [StringLength(50)]
        public string? Icon { get; set; }

        [Column("color")]
        [StringLength(7)]
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

        // Navigation properties
        public List<Event> Events { get; set; } = new();

        // ===================================
        // IBASEENTITY IMPLEMENTATION - COMPLETE
        // ===================================

        /// <summary>
        /// Name for IBaseEntity - computed from localization or DisplayName
        /// </summary>
        public string Name
        {
            get
            {
                if (!string.IsNullOrEmpty(DisplayName))
                    return DisplayName;

                if (!string.IsNullOrEmpty(NameKey))
                    return AppStrings.ResourceManager.GetString(NameKey) ?? NameKey;

                return "Unknown Event Type";
            }
            set => DisplayName = value; // Allow setting through DisplayName
        }

        /// <summary>
        /// Description for IBaseEntity - computed from localization
        /// </summary>
        public string? Description
        {
            get
            {
                if (!string.IsNullOrEmpty(DescriptionKey))
                    return AppStrings.ResourceManager.GetString(DescriptionKey) ?? DescriptionKey;

                return Name;
            }
            set => DescriptionKey = value; // Allow setting, though not commonly used
        }

        /// <summary>
        /// Display name for UI presentation
        /// </summary>
        string IBaseEntity.DisplayName => $"{Name}{(IsSystemDefault ? " (System)" : "")}{(IsFavorite ? " ⭐" : "")}";

        /// <summary>
        /// Status display for UI presentation
        /// </summary>
        public string StatusDisplay => IsActive ? "Active" : "Inactive";

        /// <summary>
        /// Validates the event type data
        /// </summary>
        public bool IsValid(out List<string> errors)
        {
            errors = [];

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Event type name is required");

            if (string.IsNullOrWhiteSpace(CategoryKey))
                errors.Add("Category is required");

            return errors.Count == 0;
        }

        /// <summary>
        /// Creates a copy of the event type for editing
        /// </summary>
        public EventType Clone()
        {
            return new EventType
            {
                Id = this.Id,
                UserId = this.UserId,
                NameKey = this.NameKey,
                DescriptionKey = this.DescriptionKey,
                DisplayName = this.DisplayName,
                CategoryKey = this.CategoryKey,
                Icon = this.Icon,
                Color = this.Color,
                IsPositive = this.IsPositive,
                RequiresFutureDate = this.RequiresFutureDate,
                IsSystemDefault = this.IsSystemDefault,
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