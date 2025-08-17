namespace OrchidPro.Messages;

/// <summary>
/// Message sent when a new genus is created to enable auto-selection workflows.
/// 
/// PURPOSE: When user creates a new genus from within species creation flow,
/// this message allows the species form to automatically select the newly created genus.
/// 
/// FLOW:
/// 1. User is creating species → clicks "Create New Genus"
/// 2. Genus created → GenusCreatedMessage sent with ID + Name
/// 3. SpeciesEditViewModel receives message → auto-selects new genus
/// 4. User returns to species form with genus pre-selected
/// 
/// PATTERN: Same as FamilyCreatedMessage but for Genus→Species relationship.
/// TECHNICAL: Uses WeakReferenceMessenger for decoupled ViewModel communication.
/// </summary>
public class GenusCreatedMessage
{
    /// <summary>
    /// ID of the newly created genus for auto-selection lookup in species form
    /// </summary>
    public Guid GenusId { get; init; }

    /// <summary>
    /// Name of the newly created genus for logging and UI feedback
    /// </summary>
    public string GenusName { get; init; } = string.Empty;

    /// <summary>
    /// Initialize message with created genus details for auto-selection workflow
    /// </summary>
    /// <param name="genusId">ID of newly created genus for parent selection</param>
    /// <param name="genusName">Name of newly created genus for logging</param>
    public GenusCreatedMessage(Guid genusId, string genusName)
    {
        GenusId = genusId;
        GenusName = genusName;
    }
}