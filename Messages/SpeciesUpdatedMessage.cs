namespace OrchidPro.Messages;

/// <summary>
/// Message sent when a species is updated, created, or deleted to trigger list refreshes.
/// 
/// PURPOSE: Notifies all SpeciesListViewModels to refresh their data when any species changes.
/// Species is the leaf entity in the taxonomy hierarchy, so only needs update notifications.
/// 
/// SCENARIOS:
/// 1. Species updated → SpeciesUpdatedMessage → all species lists refresh
/// 2. Species deleted → SpeciesUpdatedMessage → lists remove deleted item  
/// 3. Species created → SpeciesUpdatedMessage → lists show new item
/// 
/// HIERARCHY PATTERN:
/// - Family: FamilyCreatedMessage only (for genus auto-selection)
/// - Genus: GenusCreatedMessage + GenusUpdatedMessage (for species auto-selection + refresh)
/// - Species: SpeciesUpdatedMessage only (leaf entity, no children need auto-selection)
/// 
/// TECHNICAL: Parameterless broadcast message following same pattern as GenusUpdatedMessage.
/// NO CREATED MESSAGE: Species is bottom of hierarchy - no child entities need auto-selection.
/// </summary>
public class SpeciesUpdatedMessage
{
    /// <summary>
    /// Initialize the species updated message for broadcast refresh
    /// No parameters needed - this is a simple "refresh your data" signal
    /// </summary>
    public SpeciesUpdatedMessage()
    {
    }
}