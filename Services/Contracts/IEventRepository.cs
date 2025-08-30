using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Models.Statistics;

/// <summary>
/// Contract for Event repository with Event-Sourcing specific operations
/// </summary>
public interface IEventRepository : IBaseRepository<Event>
{
    /// <summary>
    /// Gets all events for a specific plant, ordered by date
    /// </summary>
    /// <param name="plantId">Plant ID to get events for</param>
    /// <returns>Events for the plant, ordered by effective date descending</returns>
    Task<IEnumerable<Event>> GetByPlantIdAsync(Guid plantId);

    /// <summary>
    /// Gets upcoming events within specified number of days
    /// </summary>
    /// <param name="days">Number of days to look ahead (default 7)</param>
    /// <returns>Upcoming events ordered by scheduled date</returns>
    Task<IEnumerable<Event>> GetUpcomingEventsAsync(int days = 7);

    /// <summary>
    /// Marks an event as completed with optional completion date
    /// </summary>
    /// <param name="eventId">Event ID to mark as completed</param>
    /// <param name="completedDate">Completion date (defaults to now)</param>
    /// <returns>True if successful, false if event not found</returns>
    Task<bool> MarkEventAsCompletedAsync(Guid eventId, DateTime? completedDate = null);

    /// <summary>
    /// Gets comprehensive statistics about events and their completion status
    /// </summary>
    /// <returns>Event statistics including completion metrics</returns>
    Task<EventStatistics> GetEventStatisticsAsync();
}