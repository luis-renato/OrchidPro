using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Models.Statistics;
using OrchidPro.Services.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Infrastructure.Supabase.Models;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;

namespace OrchidPro.Services.Infrastructure.Supabase.Repositories;

public class SupabaseEventRepository(SupabaseService supabaseService) : BaseRepository<Event>(supabaseService), IEventRepository
{
    private readonly BaseSupabaseEntityService<Event, SupabaseEvent> _supabaseEntityService = new InternalSupabaseEventService(supabaseService);
    private readonly BaseSupabaseEntityService<EventProperty, SupabaseEventProperty> _propertyEntityService = new InternalSupabaseEventPropertyService(supabaseService);

    protected override string EntityTypeName => "Event";

    protected override async Task<IEnumerable<Event>> GetAllFromServiceAsync()
    {
        var events = await _supabaseEntityService.GetAllAsync();

        // Load properties for all events
        var eventsWithProperties = new List<Event>();
        foreach (var evt in events)
        {
            evt.Properties = await LoadPropertiesForEventAsync(evt.Id);
            eventsWithProperties.Add(evt);
        }

        return eventsWithProperties;
    }

    protected override async Task<Event?> GetByIdFromServiceAsync(Guid id)
    {
        var evt = await _supabaseEntityService.GetByIdAsync(id);
        if (evt != null)
        {
            evt.Properties = await LoadPropertiesForEventAsync(id);
        }
        return evt;
    }

    protected override async Task<Event?> CreateInServiceAsync(Event entity)
    {
        var createdEvent = await _supabaseEntityService.CreateAsync(entity);

        // Save properties if any
        if (createdEvent != null && entity.Properties?.Any() == true)
        {
            foreach (var property in entity.Properties)
            {
                property.EventId = createdEvent.Id;
                await _propertyEntityService.CreateAsync(property);
            }
            createdEvent.Properties = entity.Properties;
        }

        return createdEvent;
    }

    protected override async Task<Event?> UpdateInServiceAsync(Event entity)
        => await _supabaseEntityService.UpdateAsync(entity);

    protected override async Task<bool> DeleteInServiceAsync(Guid id)
        => await _supabaseEntityService.DeleteAsync(id);

    protected override async Task<bool> NameExistsInServiceAsync(string name, Guid? excludeId)
        => await _supabaseEntityService.NameExistsAsync(name, excludeId);

    private async Task<List<EventProperty>> LoadPropertiesForEventAsync(Guid eventId)
    {
        try
        {
            var properties = await _propertyEntityService.GetAllAsync();
            return properties.Where(p => p.EventId == eventId).ToList();
        }
        catch (Exception ex)
        {
            this.LogError(ex, $"Failed to load properties for event {eventId}");
            return new List<EventProperty>();
        }
    }

    public async Task<IEnumerable<Event>> GetByPlantIdAsync(Guid plantId)
    {
        var allEvents = await GetAllAsync();
        return allEvents.Where(e => e.PlantId == plantId).OrderByDescending(e => e.ActualDate ?? e.ScheduledDate);
    }

    public async Task<IEnumerable<Event>> GetUpcomingEventsAsync(int days = 7)
    {
        var allEvents = await GetAllAsync();
        var targetDate = DateTime.Today.AddDays(days);
        return allEvents.Where(e => !e.ActualDate.HasValue && e.ScheduledDate <= targetDate)
                       .OrderBy(e => e.ScheduledDate);
    }

    public async Task<bool> MarkEventAsCompletedAsync(Guid eventId, DateTime? completedDate = null)
    {
        var evt = await GetByIdAsync(eventId);
        if (evt != null)
        {
            evt.ActualDate = completedDate ?? DateTime.Now;
            evt.UpdatedAt = DateTime.UtcNow;
            var updated = await UpdateAsync(evt);
            return updated != null;
        }
        return false;
    }

    public async Task<EventStatistics> GetEventStatisticsAsync()
    {
        var baseStats = await GetStatisticsAsync();
        var allEvents = await GetAllAsync(includeInactive: true);

        return new EventStatistics
        {
            TotalCount = baseStats.TotalCount,
            ActiveCount = baseStats.ActiveCount,
            InactiveCount = baseStats.InactiveCount,
            SystemDefaultCount = baseStats.SystemDefaultCount,
            UserCreatedCount = baseStats.UserCreatedCount,
            LastRefreshTime = baseStats.LastRefreshTime,
            CompletedEventsCount = allEvents.Count(e => e.ActualDate.HasValue),
            PendingEventsCount = allEvents.Count(e => !e.ActualDate.HasValue),
            OverdueEventsCount = allEvents.Count(e => !e.ActualDate.HasValue && e.ScheduledDate < DateTime.Today),
            EventTypeDistribution = allEvents
                .Where(e => e.EventType != null)
                .GroupBy(e => e.EventType!.CategoryKey)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public override async Task<Event> ToggleFavoriteAsync(Guid eventId)
    {
        var evt = await GetByIdAsync(eventId) ?? throw new ArgumentException($"Event with ID {eventId} not found");
        evt.IsFavorite = !evt.IsFavorite;
        evt.UpdatedAt = DateTime.UtcNow;
        var updatedEvent = await UpdateAsync(evt);
        return updatedEvent ?? throw new InvalidOperationException("Failed to update event favorite status");
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
            this.LogError(ex, "Error refreshing event data");
            var endTime = DateTime.UtcNow;
            return OperationResult.Failure(1, [ex.Message], startTime, endTime);
        }
    }
}

internal class InternalSupabaseEventService(SupabaseService supabaseService)
    : BaseSupabaseEntityService<Event, SupabaseEvent>(supabaseService)
{
    protected override string EntityTypeName => "Event";
    protected override string EntityPluralName => "Events";

    protected override Event ConvertToEntity(SupabaseEvent supabaseModel)
        => supabaseModel.ToEvent();

    protected override SupabaseEvent ConvertFromEntity(Event entity)
        => SupabaseEvent.FromEvent(entity);
}