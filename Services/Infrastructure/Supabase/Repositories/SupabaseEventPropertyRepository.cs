using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Models.Statistics;
using OrchidPro.Services.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Infrastructure.Supabase.Models;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;

namespace OrchidPro.Services.Infrastructure.Supabase.Repositories;

public class SupabaseEventPropertyRepository(SupabaseService supabaseService) : BaseRepository<EventProperty>(supabaseService), IEventPropertyRepository
{
    private readonly BaseSupabaseEntityService<EventProperty, SupabaseEventProperty> _supabaseEntityService = new InternalSupabaseEventPropertyService(supabaseService);

    protected override string EntityTypeName => "EventProperty";

    protected override async Task<IEnumerable<EventProperty>> GetAllFromServiceAsync()
        => await _supabaseEntityService.GetAllAsync();

    protected override async Task<EventProperty?> GetByIdFromServiceAsync(Guid id)
        => await _supabaseEntityService.GetByIdAsync(id);

    protected override async Task<EventProperty?> CreateInServiceAsync(EventProperty entity)
        => await _supabaseEntityService.CreateAsync(entity);

    protected override async Task<EventProperty?> UpdateInServiceAsync(EventProperty entity)
        => await _supabaseEntityService.UpdateAsync(entity);

    protected override async Task<bool> DeleteInServiceAsync(Guid id)
        => await _supabaseEntityService.DeleteAsync(id);

    protected override async Task<bool> NameExistsInServiceAsync(string name, Guid? excludeId)
        => await _supabaseEntityService.NameExistsAsync(name, excludeId);

    public async Task<IEnumerable<EventProperty>> GetByEventIdAsync(Guid eventId)
    {
        var allProperties = await GetAllAsync();
        return allProperties.Where(p => p.EventId == eventId);
    }

    public async Task<bool> DeleteByEventIdAsync(Guid eventId)
    {
        try
        {
            var properties = await GetByEventIdAsync(eventId);
            foreach (var property in properties)
            {
                await DeleteAsync(property.Id);
            }
            return true;
        }
        catch (Exception ex)
        {
            this.LogError(ex, $"Failed to delete properties for event {eventId}");
            return false;
        }
    }

    public async Task<EventPropertyStatistics> GetEventPropertyStatisticsAsync()
    {
        var baseStats = await GetStatisticsAsync();
        var allProperties = await GetAllAsync(includeInactive: true);

        return new EventPropertyStatistics
        {
            TotalCount = baseStats.TotalCount,
            ActiveCount = baseStats.ActiveCount,
            InactiveCount = baseStats.InactiveCount,
            SystemDefaultCount = baseStats.SystemDefaultCount,
            UserCreatedCount = baseStats.UserCreatedCount,
            LastRefreshTime = baseStats.LastRefreshTime,
            DataTypeDistribution = allProperties
                .GroupBy(p => p.DataType)
                .ToDictionary(g => g.Key, g => g.Count()),
            UniqueKeysCount = allProperties.Select(p => p.Key).Distinct().Count()
        };
    }

    public override async Task<EventProperty> ToggleFavoriteAsync(Guid propertyId)
    {
        var property = await GetByIdAsync(propertyId) ?? throw new ArgumentException($"EventProperty with ID {propertyId} not found");
        // Note: EventProperty doesn't have IsFavorite, but implementing for interface consistency
        property.UpdatedAt = DateTime.UtcNow;
        var updatedProperty = await UpdateAsync(property);
        return updatedProperty ?? throw new InvalidOperationException("Failed to update event property");
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
            this.LogError(ex, "Error refreshing event property data");
            var endTime = DateTime.UtcNow;
            return OperationResult.Failure(1, [ex.Message], startTime, endTime);
        }
    }
}

internal class InternalSupabaseEventPropertyService(SupabaseService supabaseService)
    : BaseSupabaseEntityService<EventProperty, SupabaseEventProperty>(supabaseService)
{
    protected override string EntityTypeName => "EventProperty";
    protected override string EntityPluralName => "EventProperties";

    protected override EventProperty ConvertToEntity(SupabaseEventProperty supabaseModel)
        => supabaseModel.ToEventProperty();

    protected override SupabaseEventProperty ConvertFromEntity(EventProperty entity)
        => SupabaseEventProperty.FromEventProperty(entity);
}