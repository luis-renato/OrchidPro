namespace OrchidPro.Messages;

/// <summary>
/// Message sent when a genus is updated, created, or deleted to trigger list refreshes.
/// 
/// PURPOSE: Notifies all GenusListViewModels to refresh their data when any genus changes.
/// Unlike GenusCreatedMessage (specific workflow), this is a general "something changed" signal.
/// 
/// SCENARIOS:
/// 1. Genus updated → GenusUpdatedMessage → all genus lists refresh
/// 2. Genus deleted → GenusUpdatedMessage → lists remove deleted item
/// 3. Genus created → GenusUpdatedMessage → lists show new item
/// 
/// DIFFERENCE FROM GenusCreatedMessage:
/// - GenusCreatedMessage: Specific auto-selection workflow (carries ID + Name)
/// - GenusUpdatedMessage: General refresh signal (no data, just "refresh now")
/// 
/// TECHNICAL: Parameterless message for broadcast refresh pattern.
/// PERFORMANCE: Triggers lazy refresh - lists only reload if currently visible.
/// </summary>
public class GenusUpdatedMessage
{
    /// <summary>
    /// Initialize the genus updated message for broadcast refresh
    /// No parameters needed - this is a simple "refresh your data" signal
    /// </summary>
    public GenusUpdatedMessage()
    {
    }
}