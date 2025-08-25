using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Services.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Infrastructure.Supabase.Models;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;

namespace OrchidPro.Services.Infrastructure.Supabase.Repositories;

public class SupabaseLocationRepository(SupabaseService supabaseService) : BaseRepository<PlantLocation>(supabaseService), ILocationRepository
{
    private readonly BaseSupabaseEntityService<PlantLocation, SupabaseLocation> _supabaseEntityService = new InternalSupabaseLocationService(supabaseService);

    protected override string EntityTypeName => "Location";

    protected override async Task<IEnumerable<PlantLocation>> GetAllFromServiceAsync()
        => await _supabaseEntityService.GetAllAsync();

    protected override async Task<PlantLocation?> GetByIdFromServiceAsync(Guid id)
        => await _supabaseEntityService.GetByIdAsync(id);

    protected override async Task<PlantLocation?> CreateInServiceAsync(PlantLocation entity)
        => await _supabaseEntityService.CreateAsync(entity);

    protected override async Task<PlantLocation?> UpdateInServiceAsync(PlantLocation entity)
        => await _supabaseEntityService.UpdateAsync(entity);

    protected override async Task<bool> DeleteInServiceAsync(Guid id)
        => await _supabaseEntityService.DeleteAsync(id);

    protected override async Task<bool> NameExistsInServiceAsync(string name, Guid? excludeId)
        => await _supabaseEntityService.NameExistsAsync(name, excludeId);

    public async Task<LocationStatistics> GetLocationStatisticsAsync()
    {
        var baseStats = await GetStatisticsAsync();
        return new LocationStatistics
        {
            TotalCount = baseStats.TotalCount,
            ActiveCount = baseStats.ActiveCount,
            InactiveCount = baseStats.InactiveCount,
            SystemDefaultCount = baseStats.SystemDefaultCount,
            UserCreatedCount = baseStats.UserCreatedCount,
            LastRefreshTime = baseStats.LastRefreshTime
        };
    }

    public override async Task<PlantLocation> ToggleFavoriteAsync(Guid locationId)
    {
        var location = await GetByIdAsync(locationId) ?? throw new ArgumentException($"Location with ID {locationId} not found");
        location.IsFavorite = !location.IsFavorite;
        location.UpdatedAt = DateTime.UtcNow;
        var updatedLocation = await UpdateAsync(location);
        return updatedLocation ?? throw new InvalidOperationException("Failed to update location favorite status");
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
            this.LogError(ex, "Error refreshing location data");
            var endTime = DateTime.UtcNow;
            return OperationResult.Failure(1, [ex.Message], startTime, endTime);
        }
    }
}

internal class InternalSupabaseLocationService(SupabaseService supabaseService)
    : BaseSupabaseEntityService<PlantLocation, SupabaseLocation>(supabaseService)
{
    protected override string EntityTypeName => "Location";
    protected override string EntityPluralName => "Locations";

    protected override PlantLocation ConvertToEntity(SupabaseLocation supabaseModel)
        => supabaseModel.ToPlantLocation();

    protected override SupabaseLocation ConvertFromEntity(PlantLocation entity)
        => SupabaseLocation.FromPlantLocation(entity);
}
