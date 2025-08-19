using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Services.Base;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;

namespace OrchidPro.Services;

/// <summary>
/// CONSISTENT Species repository - follows same pattern as Genus
/// BaseHierarchicalRepository handles the heavy lifting, this provides service connection and species-specific methods.
/// Uses background genus hydration for optimal performance without over-engineering.
/// </summary>
public class SpeciesRepository : BaseHierarchicalRepository<Species, Genus>, ISpeciesRepository
{
    #region Private Fields

    private readonly SupabaseSpeciesService _speciesService;
    private readonly IGenusRepository _genusRepository;

    #endregion

    #region Required Base Implementation

    protected override string EntityTypeName => "Species";
    protected override string ParentEntityTypeName => "Genus";

    /// <summary>
    /// FIXED: Load species WITH genus data immediately (like Genus loads with Family)
    /// Eliminates "Unknown Genus" delays by populating parent data upfront
    /// </summary>
    protected override async Task<IEnumerable<Species>> GetAllFromServiceAsync()
    {
        var rawSpecies = await _speciesService.GetAllAsync();
        // FIXED IDE0305: Simplified collection initialization
        var speciesWithGenus = await PopulateParentDataAsync([.. rawSpecies]);
        this.LogInfo($"✅ IMMEDIATE: Loaded {speciesWithGenus.Count} species WITH genus data (no delay like Genus)");
        return speciesWithGenus;
    }

    protected override async Task<Species?> GetByIdFromServiceAsync(Guid id)
        => await _speciesService.GetByIdAsync(id);

    protected override async Task<Species?> CreateInServiceAsync(Species entity)
        => await _speciesService.CreateAsync(entity);

    protected override async Task<Species?> UpdateInServiceAsync(Species entity)
        => await _speciesService.UpdateAsync(entity);

    protected override async Task<bool> DeleteInServiceAsync(Guid id)
        => await _speciesService.DeleteAsync(id);

    protected override async Task<bool> NameExistsInServiceAsync(string name, Guid? excludeId)
        => await _speciesService.NameExistsAsync(name, excludeId);

    #endregion

    #region Constructor

    public SpeciesRepository(
        SupabaseService supabaseService,
        SupabaseSpeciesService speciesService,
        IGenusRepository genusRepository)
        : base(supabaseService, genusRepository)
    {
        _speciesService = speciesService ?? throw new ArgumentNullException(nameof(speciesService));
        _genusRepository = genusRepository ?? throw new ArgumentNullException(nameof(genusRepository));
        this.LogInfo("CONSISTENT SpeciesRepository - background genus hydration enabled!");
    }

    #endregion

    #region ISpeciesRepository - Required Interface Methods

    /// <summary>
    /// Get species by genus - uses base GetByParentIdAsync
    /// </summary>
    public async Task<List<Species>> GetByGenusAsync(Guid genusId, bool includeInactive = false)
        => await GetByParentIdAsync(genusId, includeInactive);

    /// <summary>
    /// Check name exists in genus - uses base NameExistsInParentAsync
    /// </summary>
    public async Task<bool> NameExistsInGenusAsync(string name, Guid genusId, Guid? excludeId = null)
        => await NameExistsInParentAsync(name, genusId, excludeId);

    /// <summary>
    /// Get count by genus - uses base GetCountByParentAsync
    /// </summary>
    public async Task<int> GetCountByGenusAsync(Guid genusId, bool includeInactive = false)
        => await GetCountByParentAsync(genusId, includeInactive);

    /// <summary>
    /// Get species by family - hierarchical query through genus
    /// </summary>
    public async Task<List<Species>> GetByFamilyAsync(Guid familyId, bool includeInactive = false)
    {
        var genera = await _genusRepository.GetByFamilyIdAsync(familyId, includeInactive);
        // FIXED IDE0305: Simplified collection initialization
        var genusIds = genera.Select(g => g.Id).ToHashSet();

        var allSpecies = await GetAllAsync(includeInactive);
        return [.. allSpecies.Where(s => s.GenusId != Guid.Empty && genusIds.Contains(s.GenusId)).OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Get species by scientific name
    /// </summary>
    public async Task<List<Species>> GetByScientificNameAsync(string scientificName, bool exactMatch = true)
    {
        var allSpecies = await GetAllAsync(true);
        return [.. allSpecies.Where(s =>
            exactMatch
                ? string.Equals(s.ScientificName, scientificName, StringComparison.OrdinalIgnoreCase)
                : !string.IsNullOrWhiteSpace(s.ScientificName) && s.ScientificName.Contains(scientificName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.ScientificName, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Get species by rarity status
    /// </summary>
    public async Task<List<Species>> GetByRarityStatusAsync(string rarityStatus, bool includeInactive = false)
    {
        var allSpecies = await GetAllAsync(includeInactive);
        // FIXED IDE0305: Simplified collection initialization
        return [.. allSpecies.Where(s => string.Equals(s.RarityStatus, rarityStatus, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Get species by size category
    /// </summary>
    public async Task<List<Species>> GetBySizeCategoryAsync(string sizeCategory, bool includeInactive = false)
    {
        var allSpecies = await GetAllAsync(includeInactive);
        // FIXED IDE0305: Simplified collection initialization
        return [.. allSpecies.Where(s => string.Equals(s.SizeCategory, sizeCategory, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Get species by flowering season
    /// </summary>
    public async Task<List<Species>> GetByFloweringSeasonAsync(string season, bool includeInactive = false)
    {
        var allSpecies = await GetAllAsync(includeInactive);
        // FIXED IDE0305: Simplified collection initialization
        return [.. allSpecies.Where(s => !string.IsNullOrWhiteSpace(s.FloweringSeason) &&
                                   s.FloweringSeason.Contains(season, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Get species by temperature preference
    /// </summary>
    public async Task<List<Species>> GetByTemperaturePreferenceAsync(string temperaturePreference, bool includeInactive = false)
    {
        var allSpecies = await GetAllAsync(includeInactive);
        // FIXED IDE0305: Simplified collection initialization
        return [.. allSpecies.Where(s => string.Equals(s.TemperaturePreference, temperaturePreference, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Get species by growth habit
    /// </summary>
    public async Task<List<Species>> GetByGrowthHabitAsync(string growthHabit, bool includeInactive = false)
    {
        var allSpecies = await GetAllAsync(includeInactive);
        // FIXED IDE0305: Simplified collection initialization
        return [.. allSpecies.Where(s => string.Equals(s.GrowthHabit, growthHabit, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Get species by light requirements
    /// </summary>
    public async Task<List<Species>> GetByLightRequirementsAsync(string lightRequirements, bool includeInactive = false)
    {
        var allSpecies = await GetAllAsync(includeInactive);
        // FIXED IDE0305: Simplified collection initialization
        return [.. allSpecies.Where(s => string.Equals(s.LightRequirements, lightRequirements, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Get fragrant species
    /// </summary>
    public async Task<List<Species>> GetFragrantSpeciesAsync(bool includeInactive = false)
    {
        var allSpecies = await GetAllAsync(includeInactive);
        // FIXED IDE0305: Simplified collection initialization
        return [.. allSpecies.Where(s => s.Fragrance == true)
                        .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Get recently added species
    /// </summary>
    public async Task<List<Species>> GetRecentlyAddedAsync(int count = 10)
    {
        var allSpecies = await GetAllAsync(false);
        // FIXED IDE0305: Simplified collection initialization
        return [.. allSpecies.OrderByDescending(s => s.CreatedAt)
                        .Take(count)];
    }

    /// <summary>
    /// Get species needing cultivation info
    /// </summary>
    public async Task<List<Species>> GetSpeciesNeedingCultivationInfoAsync()
    {
        var allSpecies = await GetAllAsync(false);
        // FIXED IDE0305: Simplified collection initialization
        return [.. allSpecies.Where(s => string.IsNullOrWhiteSpace(s.CultivationNotes) ||
                                   string.IsNullOrWhiteSpace(s.TemperaturePreference) ||
                                   string.IsNullOrWhiteSpace(s.LightRequirements))
                        .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Advanced search with multiple criteria
    /// FIXED CA1862: Using StringComparison.OrdinalIgnoreCase instead of ToLowerInvariant for performance
    /// </summary>
    public async Task<List<Species>> SearchAdvancedAsync(
        string? searchText = null,
        Guid? genusId = null,
        string? rarityStatus = null,
        string? sizeCategory = null,
        string? temperaturePreference = null,
        string? growthHabit = null,
        bool? hasFragrance = null,
        bool includeInactive = false)
    {
        var allSpecies = await GetAllAsync(includeInactive);
        var query = allSpecies.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            // FIXED CA1862: Using StringComparison.OrdinalIgnoreCase instead of ToLowerInvariant
            query = query.Where(s =>
                s.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(s.ScientificName) && s.ScientificName.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(s.CommonName) && s.CommonName.Contains(searchText, StringComparison.OrdinalIgnoreCase)));
        }

        if (genusId.HasValue)
            query = query.Where(s => s.GenusId == genusId.Value);

        if (!string.IsNullOrWhiteSpace(rarityStatus))
            query = query.Where(s => string.Equals(s.RarityStatus, rarityStatus, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(sizeCategory))
            query = query.Where(s => string.Equals(s.SizeCategory, sizeCategory, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(temperaturePreference))
            query = query.Where(s => string.Equals(s.TemperaturePreference, temperaturePreference, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(growthHabit))
            query = query.Where(s => string.Equals(s.GrowthHabit, growthHabit, StringComparison.OrdinalIgnoreCase));

        if (hasFragrance.HasValue)
            query = query.Where(s => s.Fragrance == hasFragrance.Value);

        return [.. query.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Get species statistics for dashboard display
    /// FIXED CS0114: Renamed from GetStatisticsAsync to GetSpeciesStatisticsAsync to match interface
    /// </summary>
    public async Task<Dictionary<string, int>> GetSpeciesStatisticsAsync()
    {
        var allSpecies = await GetAllAsync(true);

        // FIXED IDE0305: Simplified collection initialization
        return new Dictionary<string, int>
        {
            ["Total"] = allSpecies.Count,
            ["Active"] = allSpecies.Count(s => s.IsActive),
            ["Inactive"] = allSpecies.Count(s => !s.IsActive),
            ["Favorites"] = allSpecies.Count(s => s.IsFavorite),
            ["WithScientificName"] = allSpecies.Count(s => !string.IsNullOrWhiteSpace(s.ScientificName)),
            ["Fragrant"] = allSpecies.Count(s => s.Fragrance == true),
            ["Rare"] = allSpecies.Count(s => s.RarityStatus != "Common"),
            ["WithCultivationNotes"] = allSpecies.Count(s => !string.IsNullOrWhiteSpace(s.CultivationNotes))
        };
    }

    #endregion
}