using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Services.Base;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;

namespace OrchidPro.Services;

/// <summary>
/// MINIMAL Species repository - following GenusRepository pattern with BaseHierarchicalRepository.
/// BaseHierarchicalRepository handles ALL the heavy lifting - this just provides service connection and aliases.
/// Manages Species -> Genus hierarchical relationship following the same pattern as Genus -> Family.
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

    protected override async Task<IEnumerable<Species>> GetAllFromServiceAsync()
        => await _speciesService.GetAllAsync();

    protected override async Task<Species?> GetByIdFromServiceAsync(Guid id)
    {
        // SupabaseSpeciesService doesn't have GetByIdAsync - use base implementation
        var allSpecies = await GetAllFromServiceAsync();
        return allSpecies.FirstOrDefault(s => s.Id == id);
    }

    protected override async Task<Species?> CreateInServiceAsync(Species entity)
        => await _speciesService.CreateAsync(entity);

    protected override async Task<Species?> UpdateInServiceAsync(Species entity)
        => await _speciesService.UpdateAsync(entity);

    protected override async Task<bool> DeleteInServiceAsync(Guid id)
        => await _speciesService.DeleteAsync(id);

    protected override async Task<bool> NameExistsInServiceAsync(string name, Guid? excludeId)
    {
        // SupabaseSpeciesService doesn't have NameExistsAsync - use base implementation
        var allSpecies = await GetAllFromServiceAsync();
        return allSpecies.Any(s =>
            string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase) &&
            s.Id != excludeId);
    }

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
        this.LogInfo("SpeciesRepository initialized - BaseHierarchicalRepository handles hierarchical operations!");
    }

    #endregion

    #region ISpeciesRepository Hierarchical Aliases - Forward to Base Methods

    public async Task<List<Species>> GetByGenusAsync(Guid genusId, bool includeInactive = false)
        => await GetByParentIdAsync(genusId, includeInactive);

    public async Task<bool> NameExistsInGenusAsync(string name, Guid genusId, Guid? excludeId = null)
        => await NameExistsInParentAsync(name, genusId, excludeId);

    public async Task<int> GetCountByGenusAsync(Guid genusId, bool includeInactive = false)
        => await GetCountByParentAsync(genusId, includeInactive);

    #endregion

    #region Family Queries (Through Genus Relationship)

    /// <summary>
    /// Get all species belonging to a specific family (through genus relationship)
    /// </summary>
    public async Task<List<Species>> GetByFamilyAsync(Guid familyId, bool includeInactive = false)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            this.LogInfo($"Getting species by family ID: {familyId}");

            // Get all genera in the family first
            var genera = await _genusRepository.GetByFamilyIdAsync(familyId, includeInactive);
            if (!genera.Any())
            {
                this.LogInfo($"No genera found in family {familyId}");
                return new List<Species>();
            }

            // Get all species for all genera in the family
            var allSpecies = await GetAllAsync(includeInactive);
            var genusIds = genera.Select(g => g.Id).ToHashSet();

            var familySpecies = allSpecies
                .Where(s => genusIds.Contains(s.GenusId))
                .OrderBy(s => s.Name)
                .ToList();

            this.LogSuccess($"Retrieved {familySpecies.Count} species for family {familyId}");
            return familySpecies;

        }, "GetByFamilyAsync").ContinueWith(t => t.Result.Data ?? new List<Species>());
    }

    #endregion

    #region Botanical Queries

    /// <summary>
    /// Search species by scientific name across all genera
    /// </summary>
    public async Task<List<Species>> GetByScientificNameAsync(string scientificName, bool exactMatch = true)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            this.LogInfo($"Searching species by scientific name: {scientificName} (exact: {exactMatch})");

            var allSpecies = await GetAllAsync(true); // Include inactive for scientific searches

            var results = exactMatch
                ? allSpecies.Where(s => string.Equals(s.ScientificName, scientificName, StringComparison.OrdinalIgnoreCase))
                : allSpecies.Where(s => !string.IsNullOrWhiteSpace(s.ScientificName) &&
                                       s.ScientificName.Contains(scientificName, StringComparison.OrdinalIgnoreCase));

            var speciesList = results.OrderBy(s => s.ScientificName).ToList();
            this.LogSuccess($"Found {speciesList.Count} species matching scientific name search");
            return speciesList;

        }, "GetByScientificNameAsync").ContinueWith(t => t.Result.Data ?? new List<Species>());
    }

    /// <summary>
    /// Get species by rarity status for conservation tracking
    /// </summary>
    public async Task<List<Species>> GetByRarityStatusAsync(string rarityStatus, bool includeInactive = false)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            this.LogInfo($"Getting species by rarity status: {rarityStatus}");

            var allSpecies = await GetAllAsync(includeInactive);
            var raritySpecies = allSpecies
                .Where(s => string.Equals(s.RarityStatus, rarityStatus, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.Name)
                .ToList();

            this.LogSuccess($"Found {raritySpecies.Count} species with rarity status: {rarityStatus}");
            return raritySpecies;

        }, "GetByRarityStatusAsync").ContinueWith(t => t.Result.Data ?? new List<Species>());
    }

    /// <summary>
    /// Get species by size category for space planning
    /// </summary>
    public async Task<List<Species>> GetBySizeCategoryAsync(string sizeCategory, bool includeInactive = false)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            this.LogInfo($"Getting species by size category: {sizeCategory}");

            var allSpecies = await GetAllAsync(includeInactive);
            var sizeSpecies = allSpecies
                .Where(s => string.Equals(s.SizeCategory, sizeCategory, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.Name)
                .ToList();

            this.LogSuccess($"Found {sizeSpecies.Count} species with size category: {sizeCategory}");
            return sizeSpecies;

        }, "GetBySizeCategoryAsync").ContinueWith(t => t.Result.Data ?? new List<Species>());
    }

    /// <summary>
    /// Get species that bloom in a specific season
    /// </summary>
    public async Task<List<Species>> GetByFloweringSeasonAsync(string season, bool includeInactive = false)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            this.LogInfo($"Getting species by flowering season: {season}");

            var allSpecies = await GetAllAsync(includeInactive);
            var seasonSpecies = allSpecies
                .Where(s => !string.IsNullOrWhiteSpace(s.FloweringSeason) &&
                           s.FloweringSeason.Contains(season, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.Name)
                .ToList();

            this.LogSuccess($"Found {seasonSpecies.Count} species blooming in: {season}");
            return seasonSpecies;

        }, "GetByFloweringSeasonAsync").ContinueWith(t => t.Result.Data ?? new List<Species>());
    }

    /// <summary>
    /// Get fragrant species for specialized collections
    /// </summary>
    public async Task<List<Species>> GetFragrantSpeciesAsync(bool includeInactive = false)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            this.LogInfo("Getting fragrant species");

            var allSpecies = await GetAllAsync(includeInactive);
            var fragrantSpecies = allSpecies
                .Where(s => s.Fragrance == true)
                .OrderBy(s => s.Name)
                .ToList();

            this.LogSuccess($"Found {fragrantSpecies.Count} fragrant species");
            return fragrantSpecies;

        }, "GetFragrantSpeciesAsync").ContinueWith(t => t.Result.Data ?? new List<Species>());
    }

    #endregion

    #region Cultivation Queries

    /// <summary>
    /// Get species by temperature preference for greenhouse planning
    /// </summary>
    public async Task<List<Species>> GetByTemperaturePreferenceAsync(string temperaturePreference, bool includeInactive = false)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            this.LogInfo($"Getting species by temperature preference: {temperaturePreference}");

            var allSpecies = await GetAllAsync(includeInactive);
            var tempSpecies = allSpecies
                .Where(s => string.Equals(s.TemperaturePreference, temperaturePreference, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.Name)
                .ToList();

            this.LogSuccess($"Found {tempSpecies.Count} species with temperature preference: {temperaturePreference}");
            return tempSpecies;

        }, "GetByTemperaturePreferenceAsync").ContinueWith(t => t.Result.Data ?? new List<Species>());
    }

    /// <summary>
    /// Get species by growth habit for mounting and potting decisions
    /// </summary>
    public async Task<List<Species>> GetByGrowthHabitAsync(string growthHabit, bool includeInactive = false)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            this.LogInfo($"Getting species by growth habit: {growthHabit}");

            var allSpecies = await GetAllAsync(includeInactive);
            var habitSpecies = allSpecies
                .Where(s => string.Equals(s.GrowthHabit, growthHabit, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.Name)
                .ToList();

            this.LogSuccess($"Found {habitSpecies.Count} species with growth habit: {growthHabit}");
            return habitSpecies;

        }, "GetByGrowthHabitAsync").ContinueWith(t => t.Result.Data ?? new List<Species>());
    }

    /// <summary>
    /// Get species with specific light requirements
    /// </summary>
    public async Task<List<Species>> GetByLightRequirementsAsync(string lightRequirements, bool includeInactive = false)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            this.LogInfo($"Getting species by light requirements: {lightRequirements}");

            var allSpecies = await GetAllAsync(includeInactive);
            var lightSpecies = allSpecies
                .Where(s => string.Equals(s.LightRequirements, lightRequirements, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.Name)
                .ToList();

            this.LogSuccess($"Found {lightSpecies.Count} species with light requirements: {lightRequirements}");
            return lightSpecies;

        }, "GetByLightRequirementsAsync").ContinueWith(t => t.Result.Data ?? new List<Species>());
    }

    #endregion

    #region Analytics and Statistics

    /// <summary>
    /// Get species statistics for dashboard display
    /// </summary>
    public async Task<Dictionary<string, int>> GetStatisticsAsync()
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            this.LogInfo("Calculating species statistics");

            var allSpecies = await GetAllAsync(true); // Include inactive for full stats

            var stats = new Dictionary<string, int>
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

            this.LogSuccess($"Calculated statistics for {stats["Total"]} species");
            return stats;

        }, "GetStatisticsAsync").ContinueWith(t => t.Result.Data ?? new Dictionary<string, int>());
    }

    /// <summary>
    /// Get the most recently added species for "What's New" features
    /// </summary>
    public async Task<List<Species>> GetRecentlyAddedAsync(int count = 10)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            this.LogInfo($"Getting {count} most recently added species");

            var allSpecies = await GetAllAsync(false); // Only active species
            var recentSpecies = allSpecies
                .OrderByDescending(s => s.CreatedAt)
                .Take(count)
                .ToList();

            this.LogSuccess($"Retrieved {recentSpecies.Count} recently added species");
            return recentSpecies;

        }, "GetRecentlyAddedAsync").ContinueWith(t => t.Result.Data ?? new List<Species>());
    }

    /// <summary>
    /// Get species without sufficient cultivation information for data completion workflows
    /// </summary>
    public async Task<List<Species>> GetSpeciesNeedingCultivationInfoAsync()
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            this.LogInfo("Finding species needing cultivation information");

            var allSpecies = await GetAllAsync(false); // Only active species
            var incompleteSpecies = allSpecies
                .Where(s => string.IsNullOrWhiteSpace(s.CultivationNotes) ||
                           string.IsNullOrWhiteSpace(s.TemperaturePreference) ||
                           string.IsNullOrWhiteSpace(s.LightRequirements))
                .OrderBy(s => s.Name)
                .ToList();

            this.LogSuccess($"Found {incompleteSpecies.Count} species needing cultivation information");
            return incompleteSpecies;

        }, "GetSpeciesNeedingCultivationInfoAsync").ContinueWith(t => t.Result.Data ?? new List<Species>());
    }

    #endregion

    #region Advanced Search

    /// <summary>
    /// Advanced search with multiple criteria for complex filtering
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
        return await this.SafeDataExecuteAsync(async () =>
        {
            this.LogInfo($"Advanced search with multiple criteria - SearchText: {searchText}, Genus: {genusId}, Rarity: {rarityStatus}");

            var allSpecies = await GetAllAsync(includeInactive);
            var query = allSpecies.AsEnumerable();

            // Apply search text filter
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var searchLower = searchText.ToLowerInvariant();
                query = query.Where(s =>
                    s.Name.ToLowerInvariant().Contains(searchLower) ||
                    (!string.IsNullOrWhiteSpace(s.ScientificName) && s.ScientificName.ToLowerInvariant().Contains(searchLower)) ||
                    (!string.IsNullOrWhiteSpace(s.CommonName) && s.CommonName.ToLowerInvariant().Contains(searchLower)) ||
                    (!string.IsNullOrWhiteSpace(s.Description) && s.Description.ToLowerInvariant().Contains(searchLower)));
            }

            // Apply genus filter
            if (genusId.HasValue)
            {
                query = query.Where(s => s.GenusId == genusId.Value);
            }

            // Apply rarity status filter
            if (!string.IsNullOrWhiteSpace(rarityStatus))
            {
                query = query.Where(s => string.Equals(s.RarityStatus, rarityStatus, StringComparison.OrdinalIgnoreCase));
            }

            // Apply size category filter
            if (!string.IsNullOrWhiteSpace(sizeCategory))
            {
                query = query.Where(s => string.Equals(s.SizeCategory, sizeCategory, StringComparison.OrdinalIgnoreCase));
            }

            // Apply temperature preference filter
            if (!string.IsNullOrWhiteSpace(temperaturePreference))
            {
                query = query.Where(s => string.Equals(s.TemperaturePreference, temperaturePreference, StringComparison.OrdinalIgnoreCase));
            }

            // Apply growth habit filter
            if (!string.IsNullOrWhiteSpace(growthHabit))
            {
                query = query.Where(s => string.Equals(s.GrowthHabit, growthHabit, StringComparison.OrdinalIgnoreCase));
            }

            // Apply fragrance filter
            if (hasFragrance.HasValue)
            {
                query = query.Where(s => s.Fragrance == hasFragrance.Value);
            }

            var results = query.OrderBy(s => s.Name).ToList();
            this.LogSuccess($"Advanced search returned {results.Count} species");
            return results;

        }, "SearchAdvancedAsync").ContinueWith(t => t.Result.Data ?? new List<Species>());
    }

    #endregion
}