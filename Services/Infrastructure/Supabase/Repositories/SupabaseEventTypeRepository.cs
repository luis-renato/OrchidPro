using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Models.Statistics;
using OrchidPro.Services.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Infrastructure.Supabase.Models;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;

namespace OrchidPro.Services.Infrastructure.Supabase.Repositories;

public class SupabaseEventTypeRepository(SupabaseService supabaseService) : BaseRepository<EventType>(supabaseService), IEventTypeRepository
{
    private readonly BaseSupabaseEntityService<EventType, SupabaseEventType> _supabaseEntityService = new InternalSupabaseEventTypeService(supabaseService);

    protected override string EntityTypeName => "EventType";

    protected override async Task<IEnumerable<EventType>> GetAllFromServiceAsync()
        => await _supabaseEntityService.GetAllAsync();

    protected override async Task<EventType?> GetByIdFromServiceAsync(Guid id)
        => await _supabaseEntityService.GetByIdAsync(id);

    protected override async Task<EventType?> CreateInServiceAsync(EventType entity)
        => await _supabaseEntityService.CreateAsync(entity);

    protected override async Task<EventType?> UpdateInServiceAsync(EventType entity)
        => await _supabaseEntityService.UpdateAsync(entity);

    protected override async Task<bool> DeleteInServiceAsync(Guid id)
        => await _supabaseEntityService.DeleteAsync(id);

    protected override async Task<bool> NameExistsInServiceAsync(string name, Guid? excludeId)
        => await _supabaseEntityService.NameExistsAsync(name, excludeId);

    public async Task<IEnumerable<EventType>> GetByCategoryAsync(string categoryKey)
    {
        var allEventTypes = await GetAllAsync();
        return allEventTypes.Where(et => et.CategoryKey == categoryKey);
    }

    public async Task<IEnumerable<EventType>> GetSystemDefaultsAsync()
    {
        var allEventTypes = await GetAllAsync();
        return allEventTypes.Where(et => et.IsSystemDefault);
    }

    public async Task<EventTypeStatistics> GetEventTypeStatisticsAsync()
    {
        var baseStats = await GetStatisticsAsync();
        var allEventTypes = await GetAllAsync(includeInactive: true);

        return new EventTypeStatistics
        {
            TotalCount = baseStats.TotalCount,
            ActiveCount = baseStats.ActiveCount,
            InactiveCount = baseStats.InactiveCount,
            SystemDefaultCount = baseStats.SystemDefaultCount,
            UserCreatedCount = baseStats.UserCreatedCount,
            LastRefreshTime = baseStats.LastRefreshTime,
            CategoryDistribution = allEventTypes
                .GroupBy(et => et.CategoryKey)
                .ToDictionary(g => g.Key, g => g.Count()),
            PositiveEventsCount = allEventTypes.Count(et => et.IsPositive),
            FutureEventsCount = allEventTypes.Count(et => et.RequiresFutureDate)
        };
    }

    public override async Task<EventType> ToggleFavoriteAsync(Guid eventTypeId)
    {
        var eventType = await GetByIdAsync(eventTypeId) ?? throw new ArgumentException($"EventType with ID {eventTypeId} not found");
        eventType.IsFavorite = !eventType.IsFavorite;
        eventType.UpdatedAt = DateTime.UtcNow;
        var updatedEventType = await UpdateAsync(eventType);
        return updatedEventType ?? throw new InvalidOperationException("Failed to update event type favorite status");
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
            this.LogError(ex, "Error refreshing event type data");
            var endTime = DateTime.UtcNow;
            return OperationResult.Failure(1, [ex.Message], startTime, endTime);
        }
    }
}

internal class InternalSupabaseEventTypeService(SupabaseService supabaseService)
    : BaseSupabaseEntityService<EventType, SupabaseEventType>(supabaseService)
{
    protected override string EntityTypeName => "EventType";
    protected override string EntityPluralName => "EventTypes";

    protected override EventType ConvertToEntity(SupabaseEventType supabaseModel)
        => supabaseModel.ToEventType();

    protected override SupabaseEventType ConvertFromEntity(EventType entity)
        => SupabaseEventType.FromEventType(entity);
}