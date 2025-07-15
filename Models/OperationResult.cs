namespace OrchidPro.Models;

/// <summary>
/// LIMPO: Resultado de operações (substitui SyncResult)
/// Sem conceitos de sync - apenas operações diretas
/// </summary>
public class OperationResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int TotalProcessed { get; set; }
    public int Successful { get; set; }
    public int Failed { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
    public bool IsSuccess => Failed == 0 && ErrorMessages.Count == 0;
}

/// <summary>
/// LIMPO: Resultado de operação individual
/// </summary>
public class FamilyOperationResult
{
    public Guid FamilyId { get; set; }
    public string FamilyName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public OperationAction Action { get; set; }
    public DateTime OperationTime { get; set; }
}

/// <summary>
/// LIMPO: Tipo de operação (sem conceitos de sync)
/// </summary>
public enum OperationAction
{
    Created,
    Updated,
    Deleted,
    Loaded,
    Validated
}