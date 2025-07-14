namespace OrchidPro.Models;

/// <summary>
/// Resultado de sincronização
/// </summary>
public class SyncResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int TotalProcessed { get; set; }
    public int Successful { get; set; }
    public int Failed { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
}

/// <summary>
/// Resultado de sincronização de uma família individual
/// </summary>
public class FamilySyncResult
{
    public Guid FamilyId { get; set; }
    public string FamilyName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public SyncAction Action { get; set; }
    public DateTime SyncTime { get; set; }
    public string? ConflictData { get; set; }
}

/// <summary>
/// Tipo de ação de sincronização
/// </summary>
public enum SyncAction
{
    Created,
    Updated,
    Deleted,
    Conflict,
    NoChange,
    Downloaded
}