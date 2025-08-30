using OrchidPro.Models.Base;

namespace OrchidPro.Models.Statistics;

/// <summary>
/// Event statistics with completion and category metrics
/// </summary>
public class EventStatistics : BaseStatistics
{
    /// <summary>Number of completed events</summary>
    public int CompletedEventsCount { get; set; }

    /// <summary>Number of pending events</summary>
    public int PendingEventsCount { get; set; }

    /// <summary>Number of overdue events</summary>
    public int OverdueEventsCount { get; set; }

    /// <summary>Distribution of events by category</summary>
    public Dictionary<string, int> EventTypeDistribution { get; set; } = new();

    /// <summary>Average days to complete events</summary>
    public double AverageCompletionDays { get; set; }

    /// <summary>Most common event types</summary>
    public Dictionary<string, int> MostCommonEventTypes { get; set; } = new();
}

/// <summary>
/// Event type statistics with category distribution
/// </summary>
public class EventTypeStatistics : BaseStatistics
{
    /// <summary>Distribution of event types by category</summary>
    public Dictionary<string, int> CategoryDistribution { get; set; } = new();

    /// <summary>Number of positive event types</summary>
    public int PositiveEventsCount { get; set; }

    /// <summary>Number of event types requiring future dates</summary>
    public int FutureEventsCount { get; set; }

    /// <summary>Most used event types with usage counts</summary>
    public Dictionary<string, int> UsageFrequency { get; set; } = new();
}

/// <summary>
/// Event property statistics with data type metrics
/// </summary>
public class EventPropertyStatistics : BaseStatistics
{
    /// <summary>Distribution of properties by data type</summary>
    public Dictionary<string, int> DataTypeDistribution { get; set; } = new();

    /// <summary>Number of unique property keys</summary>
    public int UniqueKeysCount { get; set; }

    /// <summary>Most commonly used property keys</summary>
    public Dictionary<string, int> MostCommonKeys { get; set; } = new();

    /// <summary>Average properties per event</summary>
    public double AveragePropertiesPerEvent { get; set; }
}