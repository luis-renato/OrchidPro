namespace OrchidPro.Models.Base;

/// <summary>
/// Represents the result of batch operations, sync processes, or bulk data manipulations
/// </summary>
public class OperationResult
{
    /// <summary>
    /// When the operation started
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// When the operation completed
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Total duration of the operation
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Total number of items processed
    /// </summary>
    public int TotalProcessed { get; set; }

    /// <summary>
    /// Number of items processed successfully
    /// </summary>
    public int Successful { get; set; }

    /// <summary>
    /// Number of items that failed processing
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// Overall operation success status
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Collection of error messages encountered during processing
    /// </summary>
    public List<string> ErrorMessages { get; set; } = new();

    /// <summary>
    /// Success rate as percentage (0-100)
    /// </summary>
    public double SuccessRate => TotalProcessed > 0 ? (double)Successful / TotalProcessed * 100 : 0;

    /// <summary>
    /// Failure rate as percentage (0-100)
    /// </summary>
    public double FailureRate => TotalProcessed > 0 ? (double)Failed / TotalProcessed * 100 : 0;

    /// <summary>
    /// Indicates if operation had partial success (some items failed)
    /// </summary>
    public bool IsPartialSuccess => TotalProcessed > 0 && Successful > 0 && Failed > 0;

    /// <summary>
    /// Indicates if operation was completed without any processing
    /// </summary>
    public bool IsEmpty => TotalProcessed == 0;

    /// <summary>
    /// Average processing time per item
    /// </summary>
    public TimeSpan AverageProcessingTime => TotalProcessed > 0 ?
        TimeSpan.FromMilliseconds(Duration.TotalMilliseconds / TotalProcessed) :
        TimeSpan.Zero;

    /// <summary>
    /// Comprehensive summary message with statistics
    /// </summary>
    public string Summary => $"Processed {TotalProcessed}, Success: {Successful}, Failed: {Failed} ({SuccessRate:F1}%)";

    /// <summary>
    /// Detailed summary including timing information
    /// </summary>
    public string DetailedSummary =>
        $"{Summary} in {Duration.TotalSeconds:F1}s (avg: {AverageProcessingTime.TotalMilliseconds:F0}ms/item)";

    /// <summary>
    /// User-friendly status message
    /// </summary>
    public string StatusMessage
    {
        get
        {
            if (IsEmpty)
                return "No items to process";

            if (IsSuccess && Failed == 0)
                return $"Successfully processed all {TotalProcessed} items";

            if (IsPartialSuccess)
                return $"Partially completed: {Successful} succeeded, {Failed} failed";

            if (Failed == TotalProcessed)
                return $"All {TotalProcessed} items failed to process";

            return "Operation completed";
        }
    }

    /// <summary>
    /// Creates a successful operation result
    /// </summary>
    public static OperationResult Success(int totalProcessed, DateTime startTime, DateTime endTime)
    {
        return new OperationResult
        {
            StartTime = startTime,
            EndTime = endTime,
            Duration = endTime - startTime,
            TotalProcessed = totalProcessed,
            Successful = totalProcessed,
            Failed = 0,
            IsSuccess = true
        };
    }

    /// <summary>
    /// Creates a failed operation result
    /// </summary>
    public static OperationResult Failure(int totalProcessed, List<string> errors, DateTime startTime, DateTime endTime)
    {
        return new OperationResult
        {
            StartTime = startTime,
            EndTime = endTime,
            Duration = endTime - startTime,
            TotalProcessed = totalProcessed,
            Successful = 0,
            Failed = totalProcessed,
            IsSuccess = false,
            ErrorMessages = errors ?? new List<string>()
        };
    }

    /// <summary>
    /// Creates a mixed result operation
    /// </summary>
    public static OperationResult Mixed(int successful, int failed, List<string> errors, DateTime startTime, DateTime endTime)
    {
        var total = successful + failed;
        return new OperationResult
        {
            StartTime = startTime,
            EndTime = endTime,
            Duration = endTime - startTime,
            TotalProcessed = total,
            Successful = successful,
            Failed = failed,
            IsSuccess = failed == 0, // Only success if no failures
            ErrorMessages = errors ?? new List<string>()
        };
    }

    /// <summary>
    /// Creates an empty operation result
    /// </summary>
    public static OperationResult Empty(DateTime startTime, DateTime endTime)
    {
        return new OperationResult
        {
            StartTime = startTime,
            EndTime = endTime,
            Duration = endTime - startTime,
            TotalProcessed = 0,
            Successful = 0,
            Failed = 0,
            IsSuccess = true // Empty is considered successful
        };
    }

    /// <summary>
    /// Adds an error message to the result
    /// </summary>
    public void AddError(string errorMessage)
    {
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            ErrorMessages.Add(errorMessage);
        }
    }

    /// <summary>
    /// Gets all error messages as a single concatenated string
    /// </summary>
    public string GetErrorSummary()
    {
        return ErrorMessages.Count > 0 ? string.Join("; ", ErrorMessages) : "No errors";
    }

    /// <summary>
    /// Determines operation priority level for logging/notification
    /// </summary>
    public OperationPriority GetPriority()
    {
        if (Failed == 0)
            return OperationPriority.Success;

        if (IsPartialSuccess)
            return OperationPriority.Warning;

        return OperationPriority.Error;
    }
}

/// <summary>
/// Priority levels for operation results
/// </summary>
public enum OperationPriority
{
    Success,
    Warning,
    Error
}