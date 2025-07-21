namespace OrchidPro.Models.Base;

/// <summary>
/// Result of batch or sync operations
/// </summary>
public class OperationResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int TotalProcessed { get; set; }
    public int Successful { get; set; }
    public int Failed { get; set; }
    public bool IsSuccess { get; set; }
    public List<string> ErrorMessages { get; set; } = new();

    /// <summary>
    /// Success rate as percentage
    /// </summary>
    public double SuccessRate => TotalProcessed > 0 ? (double)Successful / TotalProcessed * 100 : 0;

    /// <summary>
    /// Summary message
    /// </summary>
    public string Summary => $"Processed {TotalProcessed}, Success: {Successful}, Failed: {Failed} ({SuccessRate:F1}%)";
}