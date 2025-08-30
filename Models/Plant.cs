using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OrchidPro.Resources;
using OrchidPro.Models.Base;

namespace OrchidPro.Models
{
    /// <summary>
    /// Event-Sourced Plant Model - Events are the single source of truth
    /// All plant data computed from Events history
    /// </summary>
    [Table("plants")]
    public class Plant : IBaseEntity
    {
        // ===================================
        // CAMPOS BASE ESSENCIAIS (apenas identificação)
        // ===================================
        [Column("id")]
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("user_id")]
        [Required]
        public Guid UserId { get; set; }

        [Column("species_id")]
        [Required]
        public Guid SpeciesId { get; set; }

        [Column("variant_id")]
        public Guid? VariantId { get; set; }

        [Column("plant_code")]
        [Required]
        [StringLength(50)]
        public string PlantCode { get; set; } = string.Empty;

        [Column("common_name")]
        [StringLength(255)]
        public string? CommonName { get; set; }

        // CAMPOS BASE PADRÃO
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
        public Species? Species { get; set; }
        public Variant? Variant { get; set; }
        public List<Event> Events { get; set; } = new();

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
        /// Name for IBaseEntity - uses PlantCode
        /// </summary>
        public string Name
        {
            get => PlantCode;
            set => PlantCode = value;
        }

        /// <summary>
        /// Description for IBaseEntity - uses CommonName
        /// </summary>
        public string? Description
        {
            get => CommonName;
            set => CommonName = value;
        }

        /// <summary>
        /// Plants are never system defaults
        /// </summary>
        public bool IsSystemDefault => false;

        /// <summary>
        /// Display name for UI presentation
        /// </summary>
        public string DisplayName => $"{PlantCode}{(!string.IsNullOrEmpty(CommonName) ? $" ({CommonName})" : "")}{(IsFavorite ? " ⭐" : "")}";

        /// <summary>
        /// Status display for UI presentation
        /// </summary>
        public string StatusDisplay => HealthStatus;

        /// <summary>
        /// Validates the plant data
        /// </summary>
        public bool IsValid(out List<string> errors)
        {
            errors = [];

            if (string.IsNullOrWhiteSpace(PlantCode))
                errors.Add("Plant code is required");
            else if (PlantCode.Length > 50)
                errors.Add("Plant code cannot exceed 50 characters");

            if (SpeciesId == Guid.Empty)
                errors.Add("Species is required");

            if (!string.IsNullOrEmpty(CommonName) && CommonName.Length > 255)
                errors.Add("Common name cannot exceed 255 characters");

            return errors.Count == 0;
        }

        /// <summary>
        /// Creates a copy of the plant for editing
        /// </summary>
        public Plant Clone()
        {
            return new Plant
            {
                Id = this.Id,
                UserId = this.UserId,
                SpeciesId = this.SpeciesId,
                VariantId = this.VariantId,
                PlantCode = this.PlantCode,
                CommonName = this.CommonName,
                IsActive = this.IsActive,
                IsFavorite = this.IsFavorite,
                CreatedAt = this.CreatedAt,
                UpdatedAt = this.UpdatedAt,
                SyncHash = this.SyncHash
                // Note: Events are not cloned for performance reasons
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

        // ===================================
        // COMPUTED PROPERTIES - EVENT-SOURCED
        // ===================================

        // AQUISIÇÃO
        [NotMapped]
        public DateTime? AcquisitionDate => GetLatestEventByCategory("EventCategory.Acquisition")?.EffectiveDate;

        [NotMapped]
        public decimal? AcquisitionPrice => GetLatestEventByCategory("EventCategory.Acquisition")
            ?.GetDecimalProperty(EventPropertyKeys.PricePaid);

        [NotMapped]
        public string? AcquisitionSource => GetLatestEventByCategory("EventCategory.Acquisition")
            ?.GetTextProperty(EventPropertyKeys.SellerNotes);

        // SAÚDE
        [NotMapped]
        public string HealthStatus
        {
            get
            {
                var lastHealthEvent = GetLatestEventByCategory("EventCategory.Health");
                if (lastHealthEvent == null) return "Healthy";

                // Verificar se houve recuperação depois do problema
                var recoveryEvent = Events?
                    .Where(e => e.EventType?.NameKey == "EventType.RecoveryNoted")
                    .Where(e => e.IsCompleted && e.EffectiveDate > lastHealthEvent.EffectiveDate)
                    .OrderByDescending(e => e.EffectiveDate)
                    .FirstOrDefault();

                if (recoveryEvent != null) return "Recovering";

                // Baseado na severidade do último evento de saúde
                return lastHealthEvent.GetTextProperty(EventPropertyKeys.Severity) switch
                {
                    "Critical" => "Critical",
                    "Severe" => "Sick",
                    "Moderate" => "Stressed",
                    "Minor" => "Minor Issues",
                    _ => lastHealthEvent.EventType?.NameKey switch
                    {
                        "EventType.HealthIssue" => "Sick",
                        "EventType.RecoveryNoted" => "Recovering",
                        _ => "Healthy"
                    }
                };
            }
        }

        [NotMapped]
        public string HealthStatusColor => HealthStatus switch
        {
            "Healthy" => "#4CAF50",    // Green
            "Minor Issues" => "#8BC34A", // Light Green
            "Recovering" => "#2196F3", // Blue
            "Stressed" => "#FF9800",   // Orange  
            "Sick" => "#F44336",       // Red
            "Critical" => "#9C27B0",   // Purple
            _ => "#9E9E9E"             // Gray
        };

        [NotMapped]
        public bool HasHealthIssues => HealthStatus != "Healthy";

        // CUIDADOS
        [NotMapped]
        public DateTime? LastWatered => GetLatestEventByType("EventType.Watered")?.EffectiveDate;

        [NotMapped]
        public DateTime? LastFertilized => GetLatestEventByType("EventType.Fertilized")?.EffectiveDate;

        [NotMapped]
        public int DaysSinceLastWatering => LastWatered.HasValue
            ? (DateTime.Today - LastWatered.Value.Date).Days
            : int.MaxValue;

        [NotMapped]
        public int DaysSinceLastFertilizing => LastFertilized.HasValue
            ? (DateTime.Today - LastFertilized.Value.Date).Days
            : int.MaxValue;

        [NotMapped]
        public bool NeedsWatering => DaysSinceLastWatering > GetWateringInterval();

        [NotMapped]
        public bool NeedsFertilizing => DaysSinceLastFertilizing > GetFertilizingInterval();

        // LOCALIZAÇÃO E CONTAINER
        [NotMapped]
        public Guid? CurrentLocationId
        {
            get
            {
                var relocateEvent = GetLatestEventByType("EventType.Relocated");
                var locationIdStr = relocateEvent?.GetTextProperty("new_location_id");
                return Guid.TryParse(locationIdStr, out var locationId) ? locationId : null;
            }
        }

        [NotMapped]
        public Guid? CurrentContainerId
        {
            get
            {
                var repotEvent = GetLatestEventByType("EventType.Repotted");
                var containerIdStr = repotEvent?.GetTextProperty("new_container_id");
                return Guid.TryParse(containerIdStr, out var containerId) ? containerId : null;
            }
        }

        // FLORAÇÃO
        [NotMapped]
        public DateTime? LastFlowering => GetLatestEventByCategory("EventCategory.Flowering")?.EffectiveDate;

        [NotMapped]
        public List<DateTime> FloweringHistory => Events?
            .Where(e => e.EventType?.CategoryKey == "EventCategory.Flowering")
            .Where(e => e.IsCompleted)
            .Select(e => e.EffectiveDate)
            .OrderByDescending(d => d)
            .ToList() ?? new List<DateTime>();

        [NotMapped]
        public bool IsCurrentlyBlooming
        {
            get
            {
                var bloomStart = GetLatestEventByType("EventType.FirstBloom");
                var bloomEnd = GetLatestEventByType("EventType.BloomingEnded");

                if (bloomStart?.IsCompleted != true) return false;

                return bloomEnd == null ||
                       !bloomEnd.IsCompleted ||
                       bloomStart.EffectiveDate > bloomEnd.EffectiveDate;
            }
        }

        // CRESCIMENTO
        [NotMapped]
        public string? CurrentSize
        {
            get
            {
                var measureEvent = GetLatestEventByType("EventType.SizeMeasured");
                var value = measureEvent?.GetDecimalProperty(EventPropertyKeys.MeasurementValue);
                var unit = measureEvent?.GetTextProperty(EventPropertyKeys.MeasurementUnit);

                return value.HasValue && !string.IsNullOrEmpty(unit)
                    ? $"{value.Value} {unit}"
                    : null;
            }
        }

        // PREDIÇÕES INTELIGENTES
        [NotMapped]
        public DateTime? NextWateringDue
        {
            get
            {
                if (LastWatered == null) return DateTime.Today;
                return LastWatered.Value.AddDays(GetWateringInterval());
            }
        }

        [NotMapped]
        public DateTime? NextFertilizingDue
        {
            get
            {
                if (LastFertilized == null) return DateTime.Today;
                return LastFertilized.Value.AddDays(GetFertilizingInterval());
            }
        }

        // ===================================
        // HELPER METHODS
        // ===================================

        private Event? GetLatestEventByCategory(string categoryKey)
        {
            return Events?
                .Where(e => e.EventType?.CategoryKey == categoryKey)
                .Where(e => e.IsCompleted)
                .OrderByDescending(e => e.EffectiveDate)
                .FirstOrDefault();
        }

        private Event? GetLatestEventByType(string typeKey)
        {
            return Events?
                .Where(e => e.EventType?.NameKey == typeKey)
                .Where(e => e.IsCompleted)
                .OrderByDescending(e => e.EffectiveDate)
                .FirstOrDefault();
        }

        private int GetWateringInterval()
        {
            // Calcular baseado no histórico ou default 7 dias
            var waterEvents = Events?
                .Where(e => e.EventType?.NameKey == "EventType.Watered")
                .Where(e => e.IsCompleted)
                .OrderByDescending(e => e.EffectiveDate)
                .Take(5)
                .ToList();

            if (waterEvents?.Count >= 2)
            {
                var intervals = new List<int>();
                for (int i = 0; i < waterEvents.Count - 1; i++)
                {
                    var days = (waterEvents[i].EffectiveDate.Date - waterEvents[i + 1].EffectiveDate.Date).Days;
                    if (days > 0) intervals.Add(days);
                }

                if (intervals.Any())
                    return (int)Math.Round(intervals.Average());
            }

            return 7; // Default
        }

        private int GetFertilizingInterval()
        {
            // Similar ao watering, mas default 30 dias
            return 30; // Simplified for now
        }

        // ===================================
        // QUICK ACTION METHODS
        // ===================================

        public Event CreateWateringEvent(string? waterType = null, string? notes = null)
        {
            var waterEvent = new Event
            {
                PlantId = Id,
                Title = $"Watered {PlantCode}",
                ScheduledDate = DateTime.Today,
                ActualDate = DateTime.Now,
                Notes = notes,
                Name = $"Watered {PlantCode}" // IBaseEntity requirement
            };

            if (!string.IsNullOrEmpty(waterType))
            {
                waterEvent.SetProperty(EventPropertyKeys.WaterType, waterType, "text");
            }

            return waterEvent;
        }

        public Event CreateHealthIssueEvent(string severity, string symptoms)
        {
            var healthEvent = new Event
            {
                PlantId = Id,
                Title = $"Health issue detected - {PlantCode}",
                ScheduledDate = DateTime.Today,
                ActualDate = DateTime.Now,
                Name = $"Health issue - {PlantCode}",
                EventDescription = symptoms
            };

            healthEvent.SetProperty(EventPropertyKeys.Severity, severity, "text");
            healthEvent.SetProperty(EventPropertyKeys.Symptoms, symptoms, "text");

            return healthEvent;
        }

        public Event CreateFloweringEvent(int? spikeCount = null)
        {
            var flowerEvent = new Event
            {
                PlantId = Id,
                Title = $"First bloom - {PlantCode}",
                ScheduledDate = DateTime.Today,
                ActualDate = DateTime.Now,
                Name = $"First bloom - {PlantCode}",
                EventDescription = "Plant started blooming"
            };

            if (spikeCount.HasValue)
            {
                flowerEvent.SetProperty(EventPropertyKeys.SpikeCount, spikeCount.Value, "integer");
            }

            return flowerEvent;
        }

        public Event CreateRepottingEvent(Guid? newContainerId = null, string? newSubstrate = null)
        {
            var repotEvent = new Event
            {
                PlantId = Id,
                Title = $"Repotted {PlantCode}",
                ScheduledDate = DateTime.Today,
                ActualDate = DateTime.Now,
                Name = $"Repotted {PlantCode}",
                EventDescription = "Plant was repotted"
            };

            if (newContainerId.HasValue)
            {
                repotEvent.SetProperty("new_container_id", newContainerId.Value.ToString(), "text");
            }

            if (!string.IsNullOrEmpty(newSubstrate))
            {
                repotEvent.SetProperty("new_substrate", newSubstrate, "text");
            }

            return repotEvent;
        }
    }
}