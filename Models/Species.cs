using System.ComponentModel.DataAnnotations;
using OrchidPro.Models.Base;
using OrchidPro.Services.Base;
using Supabase.Postgrest.Attributes;

namespace OrchidPro.Models;

/// <summary>
/// Species entity representing individual orchid species within the botanical hierarchy.
/// Extends base functionality with species-specific properties for detailed botanical information.
/// Hierarchical relationship: Family -> Genus -> Species
/// </summary>
[Table("species")]
public class Species : IBaseEntity, IHierarchicalEntity<Genus>
{
    #region Base Properties (IBaseEntity - Required)

    [PrimaryKey("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("genus_id")]
    public Guid GenusId { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("name")]
    [Required(ErrorMessage = "Species name is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Species name must be between 1 and 255 characters")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

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

    #endregion

    #region Species-Specific Properties (Optional - from schema analysis)

    [Column("scientific_name")]
    [StringLength(500, ErrorMessage = "Scientific name cannot exceed 500 characters")]
    public string? ScientificName { get; set; }

    [Column("common_name")]
    [StringLength(255, ErrorMessage = "Common name cannot exceed 255 characters")]
    public string? CommonName { get; set; }

    [Column("cultivation_notes")]
    public string? CultivationNotes { get; set; }

    [Column("habitat_info")]
    [StringLength(1000, ErrorMessage = "Habitat info cannot exceed 1000 characters")]
    public string? HabitatInfo { get; set; }

    [Column("flowering_season")]
    [StringLength(100, ErrorMessage = "Flowering season cannot exceed 100 characters")]
    public string? FloweringSeason { get; set; }

    [Column("flower_colors")]
    [StringLength(200, ErrorMessage = "Flower colors cannot exceed 200 characters")]
    public string? FlowerColors { get; set; }

    [Column("size_category")]
    [StringLength(50, ErrorMessage = "Size category cannot exceed 50 characters")]
    public string SizeCategory { get; set; } = "Medium";

    [Column("rarity_status")]
    [StringLength(50, ErrorMessage = "Rarity status cannot exceed 50 characters")]
    public string RarityStatus { get; set; } = "Common";

    [Column("fragrance")]
    public bool? Fragrance { get; set; } = false;

    [Column("temperature_preference")]
    [StringLength(50, ErrorMessage = "Temperature preference cannot exceed 50 characters")]
    public string? TemperaturePreference { get; set; }

    [Column("light_requirements")]
    [StringLength(50, ErrorMessage = "Light requirements cannot exceed 50 characters")]
    public string? LightRequirements { get; set; }

    [Column("humidity_preference")]
    [StringLength(50, ErrorMessage = "Humidity preference cannot exceed 50 characters")]
    public string? HumidityPreference { get; set; }

    [Column("growth_habit")]
    [StringLength(50, ErrorMessage = "Growth habit cannot exceed 50 characters")]
    public string? GrowthHabit { get; set; }

    [Column("bloom_duration")]
    [StringLength(50, ErrorMessage = "Bloom duration cannot exceed 50 characters")]
    public string? BloomDuration { get; set; }

    #endregion

    #region Navigation Properties (for hierarchical relationships)

    /// <summary>
    /// Navigation property to parent genus (optional, for eager loading)
    /// </summary>
    public Genus? Genus { get; set; }

    #endregion

    #region IBaseEntity Implementation (Following Family/Genus Pattern)

    /// <summary>
    /// System default flag - Species are typically not system defaults
    /// </summary>
    public bool IsSystemDefault => false;

    /// <summary>
    /// Display-friendly name combining scientific and common names when available
    /// </summary>
    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ScientificName) && !string.IsNullOrWhiteSpace(CommonName))
                return $"{ScientificName} ({CommonName})";

            if (!string.IsNullOrWhiteSpace(ScientificName))
                return ScientificName;

            if (!string.IsNullOrWhiteSpace(CommonName))
                return $"{Name} ({CommonName})";

            return $"{Name}{(Genus != null ? $" ({Genus.Name})" : "")}{(IsFavorite ? " ⭐" : "")}";
        }
    }

    /// <summary>
    /// Status display showing activity state and favorite status
    /// </summary>
    public string StatusDisplay
    {
        get
        {
            var status = IsActive ? "Active" : "Inactive";
            return IsFavorite ? $"{status} ⭐" : status;
        }
    }

    #endregion

    #region Species-Specific Display Properties

    /// <summary>
    /// Complete botanical name for scientific reference
    /// </summary>
    public string FullBotanicalName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ScientificName))
                return ScientificName;

            return Name;
        }
    }

    /// <summary>
    /// Cultivation difficulty indicator based on requirements complexity
    /// </summary>
    public string CultivationDifficulty
    {
        get
        {
            var complexityScore = 0;

            if (!string.IsNullOrWhiteSpace(TemperaturePreference)) complexityScore++;
            if (!string.IsNullOrWhiteSpace(LightRequirements)) complexityScore++;
            if (!string.IsNullOrWhiteSpace(HumidityPreference)) complexityScore++;
            if (!string.IsNullOrWhiteSpace(GrowthHabit)) complexityScore++;

            return complexityScore switch
            {
                >= 4 => "Advanced",
                >= 2 => "Intermediate",
                >= 1 => "Beginner",
                _ => "Unknown"
            };
        }
    }

    /// <summary>
    /// Summary of key characteristics for quick reference
    /// </summary>
    public string CharacteristicsSummary
    {
        get
        {
            var characteristics = new List<string>();

            if (SizeCategory != "Medium") characteristics.Add($"Size: {SizeCategory}");
            if (RarityStatus != "Common") characteristics.Add($"Rarity: {RarityStatus}");
            if (Fragrance == true) characteristics.Add("Fragrant");
            if (!string.IsNullOrWhiteSpace(FloweringSeason)) characteristics.Add($"Blooms: {FloweringSeason}");

            return characteristics.Count > 0 ? string.Join(" • ", characteristics) : "Standard species";
        }
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initialize new species with default values
    /// </summary>
    public Species()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsActive = true;
        IsFavorite = false;
        SizeCategory = "Medium";
        RarityStatus = "Common";
        Fragrance = false;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Update timestamp for change tracking
    /// </summary>
    public void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
        SyncHash = null; // Force resync
    }

    /// <summary>
    /// Validates the species data following IBaseEntity contract
    /// </summary>
    public bool IsValid(out List<string> errors)
    {
        errors = [];

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Species name is required");

        if (Name?.Length > 255)
            errors.Add("Species name cannot exceed 255 characters");

        if (Description?.Length > 2000)
            errors.Add("Description cannot exceed 2000 characters");

        if (GenusId == Guid.Empty)
            errors.Add("Genus is required");

        if (!string.IsNullOrWhiteSpace(ScientificName) && ScientificName.Length > 500)
            errors.Add("Scientific name cannot exceed 500 characters");

        if (!string.IsNullOrWhiteSpace(CommonName) && CommonName.Length > 255)
            errors.Add("Common name cannot exceed 255 characters");

        return errors.Count == 0;
    }

    /// <summary>
    /// Creates a copy of the species for editing
    /// </summary>
    public Species Clone()
    {
        return new Species
        {
            Id = this.Id,
            GenusId = this.GenusId,
            UserId = this.UserId,
            Name = this.Name,
            Description = this.Description,
            IsActive = this.IsActive,
            IsFavorite = this.IsFavorite,
            CreatedAt = this.CreatedAt,
            UpdatedAt = this.UpdatedAt,
            SyncHash = this.SyncHash,
            ScientificName = this.ScientificName,
            CommonName = this.CommonName,
            CultivationNotes = this.CultivationNotes,
            HabitatInfo = this.HabitatInfo,
            FloweringSeason = this.FloweringSeason,
            FlowerColors = this.FlowerColors,
            SizeCategory = this.SizeCategory,
            RarityStatus = this.RarityStatus,
            Fragrance = this.Fragrance,
            TemperaturePreference = this.TemperaturePreference,
            LightRequirements = this.LightRequirements,
            HumidityPreference = this.HumidityPreference,
            GrowthHabit = this.GrowthHabit,
            BloomDuration = this.BloomDuration
        };
    }

    /// <summary>
    /// Implementation of interface: Generic clone
    /// </summary>
    IBaseEntity IBaseEntity.Clone()
    {
        return Clone();
    }

    /// <summary>
    /// Toggle favorite status
    /// </summary>
    public void ToggleFavorite()
    {
        IsFavorite = !IsFavorite;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion

    #region IHierarchicalEntity<Genus> Implementation

    /// <summary>
    /// Parent identifier for hierarchical operations
    /// </summary>
    public Guid ParentId
    {
        get => GenusId;
        set => GenusId = value;
    }

    /// <summary>
    /// Parent entity for hierarchical operations
    /// </summary>
    public Genus? Parent
    {
        get => Genus;
        set => Genus = value;
    }

    /// <summary>
    /// Get the parent identifier value
    /// </summary>
    public Guid GetParentId() => GenusId;

    /// <summary>
    /// Set the parent identifier value
    /// </summary>
    public void SetParentId(Guid parentId) => GenusId = parentId;

    /// <summary>
    /// Get display name for the parent genus
    /// </summary>
    public string GetParentDisplayName() => Genus?.Name ?? "No Genus";

    /// <summary>
    /// Validate that the hierarchical relationship is properly configured
    /// </summary>
    public bool ValidateHierarchy()
    {
        return GenusId != Guid.Empty;
    }

    /// <summary>
    /// Get searchable text content for full-text search
    /// </summary>
    public string GetSearchableContent()
    {
        var content = new List<string>();

        // Add only non-null/non-empty strings to avoid CS8604 warnings
        if (!string.IsNullOrWhiteSpace(Name)) content.Add(Name);
        if (!string.IsNullOrWhiteSpace(ScientificName)) content.Add(ScientificName);
        if (!string.IsNullOrWhiteSpace(CommonName)) content.Add(CommonName);
        if (!string.IsNullOrWhiteSpace(Description)) content.Add(Description);
        if (!string.IsNullOrWhiteSpace(CultivationNotes)) content.Add(CultivationNotes);
        if (!string.IsNullOrWhiteSpace(HabitatInfo)) content.Add(HabitatInfo);
        if (!string.IsNullOrWhiteSpace(FloweringSeason)) content.Add(FloweringSeason);
        if (!string.IsNullOrWhiteSpace(FlowerColors)) content.Add(FlowerColors);
        if (!string.IsNullOrWhiteSpace(SizeCategory)) content.Add(SizeCategory);
        if (!string.IsNullOrWhiteSpace(RarityStatus)) content.Add(RarityStatus);
        if (!string.IsNullOrWhiteSpace(TemperaturePreference)) content.Add(TemperaturePreference);
        if (!string.IsNullOrWhiteSpace(LightRequirements)) content.Add(LightRequirements);
        if (!string.IsNullOrWhiteSpace(HumidityPreference)) content.Add(HumidityPreference);
        if (!string.IsNullOrWhiteSpace(GrowthHabit)) content.Add(GrowthHabit);
        if (!string.IsNullOrWhiteSpace(BloomDuration)) content.Add(BloomDuration);

        return string.Join(" ", content);
    }

    #endregion

    #region Object Overrides

    /// <summary>
    /// String representation for debugging and logging
    /// </summary>
    public override string ToString()
    {
        return $"Species: {DisplayName} (ID: {Id}, GenusId: {GenusId}, Active: {IsActive})";
    }

    /// <summary>
    /// Equality comparison based on Id
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is Species other && Id == other.Id;
    }

    /// <summary>
    /// Hash code based on Id for collections
    /// </summary>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    #endregion
}