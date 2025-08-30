using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Models.Statistics;

/// <summary>
/// Contract for EventType repository with categorization operations
/// </summary>
public interface IEventTypeRepository : IBaseRepository<EventType>
{
    /// <summary>
    /// Gets event types by category (Care, Health, Growth, etc.)
    /// </summary>
    /// <param name="categoryKey">Category key to filter by</param>
    /// <returns>Event types in the specified category</returns>
    Task<IEnumerable<EventType>> GetByCategoryAsync(string categoryKey);

    /// <summary>
    /// Gets system default event types (built-in types)
    /// </summary>
    /// <returns>System default event types</returns>
    Task<IEnumerable<EventType>> GetSystemDefaultsAsync();

    /// <summary>
    /// Gets statistics about event types including category distribution
    /// </summary>
    /// <returns>Event type statistics</returns>
    Task<EventTypeStatistics> GetEventTypeStatisticsAsync();
}
