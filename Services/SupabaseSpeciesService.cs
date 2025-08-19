using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Services.Data;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using OrchidPro.Extensions;

namespace OrchidPro.Services;

/// <summary>
/// Supabase database model representing species table with genus relationship.
/// Maps between database schema and application domain models.
/// </summary>
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

    /// <summary>
    /// Convert SupabaseSpecies to domain Species model
    /// </summary>
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

    /// <summary>
    /// Convert domain Species model to SupabaseSpecies
    /// </summary>
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

/// <summary>
/// MINIMAL Species service implementing ISupabaseEntityService interface. 
/// Following the exact pattern of SupabaseFamilyService and SupabaseGenusService.
/// Handles direct database operations for species entities.
/// </summary>
public class SupabaseSpeciesService : ISupabaseEntityService<Species>
{
    #region Private Fields

    private readonly SupabaseService _supabaseService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initialize species service with Supabase connection
    /// </summary>
    public SupabaseSpeciesService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        this.LogInfo("SupabaseSpeciesService initialized - implementing ISupabaseEntityService");
    }

    #endregion

    #region ISupabaseEntityService<Species> Implementation

    /// <summary>
    /// Get all species from database
    /// </summary>
    public async Task<IEnumerable<Species>> GetAllAsync()
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            if (_supabaseService.Client == null)
                return [];

            var currentUserId = GetCurrentUserId();
            var response = await _supabaseService.Client
                .From<SupabaseSpecies>()
                .Select("*")
                .Get();

            if (response?.Models == null)
                return [];

            // Filter: user species OR system defaults (UserId == null)
            var filteredSpecies = response.Models.Where(ss =>
                ss.UserId == currentUserId || ss.UserId == null);

            return filteredSpecies.Select(ss => ss.ToSpecies()).OrderBy(s => s.Name).ToList();
        }, "Species");

        return result.Success && result.Data != null ? result.Data : [];
    }

    public async Task<Species?> GetByIdAsync(Guid id)
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            if (_supabaseService.Client == null)
                return null;

            var response = await _supabaseService.Client
                .From<SupabaseSpecies>()
                .Where(s => s.Id == id)
                .Single();

            return response?.ToSpecies();
        }, "Species");

        return result.Success ? result.Data : null;
    }

    /// <summary>
    /// Create new species in database
    /// </summary>
    public async Task<Species?> CreateAsync(Species entity)
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            if (_supabaseService.Client == null)
                return null;

            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UserId = GetCurrentUserId();

            var supabaseSpecies = SupabaseSpecies.FromSpecies(entity);
            var response = await _supabaseService.Client
                .From<SupabaseSpecies>()
                .Insert(supabaseSpecies);

            return response?.Models?.FirstOrDefault()?.ToSpecies() ?? entity;
        }, "Species");

        return result.Success ? result.Data : null;
    }

    /// <summary>
    /// Update existing species in database
    /// </summary>
    public async Task<Species?> UpdateAsync(Species entity)
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            if (_supabaseService.Client == null)
                return null;

            entity.UpdatedAt = DateTime.UtcNow;
            var supabaseSpecies = SupabaseSpecies.FromSpecies(entity);

            await _supabaseService.Client
                .From<SupabaseSpecies>()
                .Where(s => s.Id == entity.Id)
                .Update(supabaseSpecies);

            return entity;
        }, "Species");

        return result.Success ? result.Data : null;
    }

    /// <summary>
    /// Delete species from database
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            if (_supabaseService.Client == null)
                return false;

            await _supabaseService.Client
                .From<SupabaseSpecies>()
                .Where(s => s.Id == id)
                .Delete();

            return true;
        }, "Species");

        return result.Success && result.Data;
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
    {
        var species = await GetAllAsync();
        return species.Any(s =>
            string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase) &&
            s.Id != excludeId);
    }

    #endregion

    #region Private Helper Methods

    private Guid? GetCurrentUserId()
    {
        var userIdString = _supabaseService.GetCurrentUserId();
        return Guid.TryParse(userIdString, out var userId) ? userId : null;
    }

    #endregion
}