using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Models.Statistics;
using OrchidPro.Services.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Infrastructure.Supabase.Models;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;

namespace OrchidPro.Services.Infrastructure.Supabase.Repositories;

public class SupabasePlantRepository(SupabaseService supabaseService) : BaseRepository<Plant>(supabaseService), IPlantRepository
{
    private readonly BaseSupabaseEntityService<Plant, SupabasePlant> _supabaseEntityService = new InternalSupabasePlantService(supabaseService);
    private readonly BaseSupabaseEntityService<Event, SupabaseEvent> _eventEntityService = new InternalSupabaseEventService(supabaseService);

    protected override string EntityTypeName => "Plant";

    protected override async Task<IEnumerable<Plant>> GetAllFromServiceAsync()
    {
        var plants = await _supabaseEntityService.GetAllAsync();

        // Load events for all plants for computed properties
        var plantsWithEvents = new List<Plant>();
        foreach (var plant in plants)
        {
            plant.Events = await LoadEventsForPlantAsync(plant.Id);
            plantsWithEvents.Add(plant);
        }

        return plantsWithEvents;
    }

    protected override async Task<Plant?> GetByIdFromServiceAsync(Guid id)
    {
        var plant = await _supabaseEntityService.GetByIdAsync(id);
        if (plant != null)
        {
            plant.Events = await LoadEventsForPlantAsync(id);
        }
        return plant;
    }

    protected override async Task<Plant?> CreateInServiceAsync(Plant entity)
        => await _supabaseEntityService.CreateAsync(entity);

    protected override async Task<Plant?> UpdateInServiceAsync(Plant entity)
        => await _supabaseEntityService.UpdateAsync(entity);

    protected override async Task<bool> DeleteInServiceAsync(Guid id)
        => await _supabaseEntityService.DeleteAsync(id);

    protected override async Task<bool> NameExistsInServiceAsync(string name, Guid? excludeId)
        => await _supabaseEntityService.NameExistsAsync(name, excludeId);

    // Event-Sourced specific methods
    private async Task<List<Event>> LoadEventsForPlantAsync(Guid plantId)
    {
        try
        {
            var events = await _eventEntityService.GetAllAsync();
            return events.Where(e => e.PlantId == plantId).OrderByDescending(e => e.ActualDate ?? e.ScheduledDate).ToList();
        }
        catch (Exception ex)
        {
            this.LogError(ex, $"Failed to load events for plant {plantId}");
            return new List<Event>();
        }
    }

    public async Task<PlantStatistics> GetPlantStatisticsAsync()
    {
        var baseStats = await GetStatisticsAsync();
        var allPlants = await GetAllAsync(includeInactive: true);

        return new PlantStatistics
        {
            TotalCount = baseStats.TotalCount,
            ActiveCount = baseStats.ActiveCount,
            InactiveCount = baseStats.InactiveCount,
            SystemDefaultCount = baseStats.SystemDefaultCount,
            UserCreatedCount = baseStats.UserCreatedCount,
            LastRefreshTime = baseStats.LastRefreshTime,
            HealthyPlantsCount = allPlants.Count(p => p.HealthStatus == "Healthy"),
            PlantsWithIssuesCount = allPlants.Count(p => p.HasHealthIssues),
            BloomingPlantsCount = allPlants.Count(p => p.IsCurrentlyBlooming),
            PlantsNeedingWaterCount = allPlants.Count(p => p.NeedsWatering),
            PlantsNeedingFertilizerCount = allPlants.Count(p => p.NeedsFertilizing)
        };
    }

    public async Task<IEnumerable<Plant>> GetByHealthStatusAsync(string healthStatus)
    {
        var allPlants = await GetAllAsync();
        return allPlants.Where(p => p.HealthStatus == healthStatus);
    }

    public async Task<IEnumerable<Plant>> GetPlantsNeedingCareAsync()
    {
        var allPlants = await GetAllAsync();
        return allPlants.Where(p => p.NeedsWatering || p.NeedsFertilizing || p.HasHealthIssues)
                       .OrderBy(p => Math.Min(p.DaysSinceLastWatering, p.DaysSinceLastFertilizing));
    }

    public async Task<IEnumerable<Plant>> GetBloomingPlantsAsync()
    {
        var allPlants = await GetAllAsync();
        return allPlants.Where(p => p.IsCurrentlyBlooming);
    }

    public async Task<bool> IsPlantCodeUniqueAsync(string plantCode, Guid? excludeId = null)
    {
        var allPlants = await GetAllAsync();
        return !allPlants.Any(p => p.PlantCode == plantCode && p.Id != excludeId);
    }

    public override async Task<Plant> ToggleFavoriteAsync(Guid plantId)
    {
        var plant = await GetByIdAsync(plantId) ?? throw new ArgumentException($"Plant with ID {plantId} not found");
        plant.IsFavorite = !plant.IsFavorite;
        plant.UpdatedAt = DateTime.UtcNow;
        var updatedPlant = await UpdateAsync(plant);
        return updatedPlant ?? throw new InvalidOperationException("Failed to update plant favorite status");
    }

    public async Task<OperationResult> RefreshAllDataAsync()
    {
        var startTime = DateTime.UtcNow;
        try
        {
            await RefreshCacheAsync();
            var endTime = DateTime.UtcNow;
            return OperationResult.Success(1, startTime, endTime);
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error refreshing plant data");
            var endTime = DateTime.UtcNow;
            return OperationResult.Failure(1, [ex.Message], startTime, endTime);
        }
    }
        public async Task<IEnumerable<Plant>> GetPlantsWithHealthIssuesAsync()
    {
        var allPlants = await GetAllAsync();
        return allPlants.Where(p => p.HasHealthIssues);
    }
}

internal class InternalSupabasePlantService(SupabaseService supabaseService)
    : BaseSupabaseEntityService<Plant, SupabasePlant>(supabaseService)
{
    protected override string EntityTypeName => "Plant";
    protected override string EntityPluralName => "Plants";

    protected override Plant ConvertToEntity(SupabasePlant supabaseModel)
        => supabaseModel.ToPlant();

    protected override SupabasePlant ConvertFromEntity(Plant entity)
        => SupabasePlant.FromPlant(entity);
}
