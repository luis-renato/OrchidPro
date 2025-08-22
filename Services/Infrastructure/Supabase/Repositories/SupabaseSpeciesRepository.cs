using OrchidPro.Extensions;
using OrchidPro.Models;
using OrchidPro.Services.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Data;
using OrchidPro.Services.Infrastructure.Supabase.Models;
using static Supabase.Postgrest.Constants;

namespace OrchidPro.Services.Infrastructure.Supabase.Repositories;

public class SupabaseSpeciesRepository(SupabaseService supabaseService, IGenusRepository genusRepository)
    : BaseHierarchicalRepository<Species, Genus>(supabaseService, genusRepository), ISpeciesRepository
{
    private readonly InternalSupabaseSpeciesService _supabaseEntityService = new(supabaseService);
    private readonly IGenusRepository _genusRepository = genusRepository;

    protected override string EntityTypeName => "Species";
    protected override string ParentEntityTypeName => "Genus";

    protected override async Task<IEnumerable<Species>> GetAllFromServiceAsync()
    {
        var rawSpecies = await _supabaseEntityService.GetAllWithBatchLoadingAsync();
        var speciesList = rawSpecies.ToList();
        var speciesWithGenus = speciesList.Count(s => s.Genus != null);

        this.LogInfo($"JOIN returned {speciesList.Count} species, {speciesWithGenus} with Genus data");

        if (speciesWithGenus > 0)
        {
            this.LogInfo("OPTIMIZED: JOIN worked! Skipping PopulateParentDataAsync");
            return speciesList;
        }
        else
        {
            this.LogInfo("JOIN failed, using fallback PopulateParentDataAsync");
            var speciesWithGenusPopulated = await PopulateParentDataAsync([.. rawSpecies]);
            return speciesWithGenusPopulated;
        }
    }

    protected override async Task<Species?> GetByIdFromServiceAsync(Guid id)
        => await _supabaseEntityService.GetByIdAsync(id);

    protected override async Task<Species?> CreateInServiceAsync(Species entity)
        => await _supabaseEntityService.CreateAsync(entity);

    protected override async Task<Species?> UpdateInServiceAsync(Species entity)
        => await _supabaseEntityService.UpdateAsync(entity);

    protected override async Task<bool> DeleteInServiceAsync(Guid id)
        => await _supabaseEntityService.DeleteAsync(id);

    protected override async Task<bool> NameExistsInServiceAsync(string name, Guid? excludeId)
        => await _supabaseEntityService.NameExistsAsync(name, excludeId);

    // ISpeciesRepository implementations
    public async Task<List<Species>> GetByGenusAsync(Guid genusId, bool includeInactive = false)
        => await GetByParentIdAsync(genusId, includeInactive);

    public async Task<bool> NameExistsInGenusAsync(string name, Guid genusId, Guid? excludeId = null)
        => await NameExistsInParentAsync(name, genusId, excludeId);

    public async Task<int> GetCountByGenusAsync(Guid genusId, bool includeInactive = false)
        => await GetCountByParentAsync(genusId, includeInactive);

    public async Task<List<Species>> GetByFamilyAsync(Guid familyId, bool includeInactive = false)
    {
        var genera = await _genusRepository.GetByFamilyIdAsync(familyId, includeInactive);
        var genusIds = genera.Select(g => g.Id).ToHashSet();

        var allSpecies = await GetAllAsync(includeInactive);
        return [.. allSpecies.Where(s => s.GenusId != Guid.Empty && genusIds.Contains(s.GenusId)).OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)];
    }

    public async Task<List<Species>> GetByScientificNameAsync(string scientificName, bool exactMatch = true)
    {
        var allSpecies = await GetAllAsync(true);
        return [.. allSpecies.Where(s =>
            exactMatch
                ? string.Equals(s.ScientificName, scientificName, StringComparison.OrdinalIgnoreCase)
                : !string.IsNullOrWhiteSpace(s.ScientificName) && s.ScientificName.Contains(scientificName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.ScientificName, StringComparer.OrdinalIgnoreCase)];
    }

    public async Task<List<Species>> GetByRarityStatusAsync(string rarityStatus, bool includeInactive = false)
    {
        var allSpecies = await GetAllAsync(includeInactive);
        return [.. allSpecies.Where(s => string.Equals(s.RarityStatus, rarityStatus, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)];
    }

    public async Task<List<Species>> GetBySizeCategoryAsync(string sizeCategory, bool includeInactive = false)
    {
        var allSpecies = await GetAllAsync(includeInactive);
        return [.. allSpecies.Where(s => string.Equals(s.SizeCategory, sizeCategory, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)];
    }

    public async Task<List<Species>> GetByFloweringSeasonAsync(string season, bool includeInactive = false)
    {
        var allSpecies = await GetAllAsync(includeInactive);
        return [.. allSpecies.Where(s => !string.IsNullOrWhiteSpace(s.FloweringSeason) &&
                               s.FloweringSeason.Contains(season, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)];
    }

    public async Task<List<Species>> GetByTemperaturePreferenceAsync(string temperaturePreference, bool includeInactive = false)
    {
        var allSpecies = await GetAllAsync(includeInactive);
        return [.. allSpecies.Where(s => string.Equals(s.TemperaturePreference, temperaturePreference, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)];
    }

    public async Task<List<Species>> GetByGrowthHabitAsync(string growthHabit, bool includeInactive = false)
    {
        var allSpecies = await GetAllAsync(includeInactive);
        return [.. allSpecies.Where(s => string.Equals(s.GrowthHabit, growthHabit, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)];
    }

    public async Task<List<Species>> GetByLightRequirementsAsync(string lightRequirements, bool includeInactive = false)
    {
        var allSpecies = await GetAllAsync(includeInactive);
        return [.. allSpecies.Where(s => string.Equals(s.LightRequirements, lightRequirements, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)];
    }

    public async Task<List<Species>> GetFragrantSpeciesAsync(bool includeInactive = false)
    {
        var allSpecies = await GetAllAsync(includeInactive);
        return [.. allSpecies.Where(s => s.Fragrance == true)
                        .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)];
    }

    public async Task<List<Species>> GetRecentlyAddedAsync(int count = 10)
    {
        var allSpecies = await GetAllAsync(false);
        return [.. allSpecies.OrderByDescending(s => s.CreatedAt)
                        .Take(count)];
    }

    public async Task<List<Species>> GetSpeciesNeedingCultivationInfoAsync()
    {
        var allSpecies = await GetAllAsync(false);
        return [.. allSpecies.Where(s => string.IsNullOrWhiteSpace(s.CultivationNotes) ||
                               string.IsNullOrWhiteSpace(s.TemperaturePreference) ||
                               string.IsNullOrWhiteSpace(s.LightRequirements))
                        .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)];
    }

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

    public async Task<Dictionary<string, int>> GetSpeciesStatisticsAsync()
    {
        var allSpecies = await GetAllAsync(true);

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
}

internal class InternalSupabaseSpeciesService(SupabaseService supabaseService)
    : BaseSupabaseEntityService<Species, SupabaseSpecies>(supabaseService)
{
    protected override string EntityTypeName => "Species";
    protected override string EntityPluralName => "Species";

    protected override Species ConvertToEntity(SupabaseSpecies supabaseModel)
        => supabaseModel.ToSpecies();

    protected override SupabaseSpecies ConvertFromEntity(Species entity)
        => SupabaseSpecies.FromSpecies(entity);

    public async Task<IEnumerable<Species>> GetAllWithBatchLoadingAsync()
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            if (_supabaseService.Client == null)
                return [];

            this.LogInfo("BATCH: Loading species with 3-query batch approach");

            var currentUserId = GetCurrentUserId();

            var speciesResponse = await _supabaseService.Client
                .From<SupabaseSpecies>()
                .Select("*")
                .Get();

            if (speciesResponse?.Models == null)
                return Enumerable.Empty<Species>();

            var filteredSpecies = speciesResponse.Models.Where(model =>
                GetModelUserId(model) == currentUserId || GetModelUserId(model) == null);

            var species = filteredSpecies.Select(ConvertToEntity).ToList();

            if (species.Count == 0)
                return species;

            this.LogInfo($"BATCH: Got {species.Count} species, now getting genus data");

            var genusIds = species.Select(s => s.GenusId).Distinct().ToArray();

            var genusResponse = await _supabaseService.Client
                .From<SupabaseGenus>()
                .Select("*")
                .Filter("id", Operator.In, genusIds)
                .Get();

            if (genusResponse?.Models != null)
            {
                this.LogInfo($"BATCH: Got {genusResponse.Models.Count} genus records");

                var genusLookup = genusResponse.Models.ToDictionary(
                    g => Guid.Parse(g.Id.ToString()!),
                    g => new Genus
                    {
                        Id = Guid.Parse(g.Id.ToString()!),
                        FamilyId = Guid.Parse(g.FamilyId.ToString()!),
                        Name = g.Name?.ToString() ?? "",
                        Description = g.Description?.ToString(),
                        IsActive = g.IsActive ?? true,
                        IsFavorite = g.IsFavorite ?? false,
                        CreatedAt = g.CreatedAt ?? DateTime.UtcNow,
                        UpdatedAt = g.UpdatedAt ?? DateTime.UtcNow,
                        UserId = g.UserId != null ? Guid.Parse(g.UserId.ToString()!) : null
                    }
                );

                foreach (var spec in species)
                {
                    if (genusLookup.TryGetValue(spec.GenusId, out var genus))
                    {
                        spec.Genus = genus;
                    }
                }

                var populatedCount = species.Count(s => s.Genus != null);
                this.LogInfo($"BATCH: Populated {populatedCount}/{species.Count} species with genus data");
            }

            return species.OrderBy(s => s.Name);

        }, EntityPluralName);

        return result.Success && result.Data != null ? result.Data : [];
    }
}