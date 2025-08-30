using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Models.Statistics;

/// <summary>
/// Contract for EventProperty repository with event-specific operations
/// </summary>
public interface IEventPropertyRepository : IBaseRepository<EventProperty>
{
    /// <summary>
    /// Gets all properties for a specific event
    /// </summary>
    /// <param name="eventId">Event ID to get properties for</param>
    /// <returns>Properties for the event</returns>
    Task<IEnumerable<EventProperty>> GetByEventIdAsync(Guid eventId);

    /// <summary>
    /// Deletes all properties for a specific event (cascade delete)
    /// </summary>
    /// <param name="eventId">Event ID to delete properties for</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteByEventIdAsync(Guid eventId);

    /// <summary>
    /// Gets statistics about event properties including data type distribution
    /// </summary>
    /// <returns>Event property statistics</returns>
    Task<EventPropertyStatistics> GetEventPropertyStatisticsAsync();
}