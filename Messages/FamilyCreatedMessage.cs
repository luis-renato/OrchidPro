namespace OrchidPro.Messages;

/// <summary>
/// Message sent when a new family is created to enable auto-selection workflows.
/// 
/// PURPOSE: When user creates a new family from within genus creation flow,
/// this message allows the genus form to automatically select the newly created family.
/// 
/// FLOW:
/// 1. User is creating genus → clicks "Create New Family" 
/// 2. Family created → FamilyCreatedMessage sent with ID + Name
/// 3. GenusEditViewModel receives message → auto-selects new family
/// 4. User returns to genus form with family pre-selected
/// 
/// TECHNICAL: Uses WeakReferenceMessenger pattern for loose coupling between ViewModels.
/// </summary>
public class FamilyCreatedMessage
{
    /// <summary>
    /// ID of the newly created family for auto-selection lookup
    /// </summary>
    public Guid FamilyId { get; init; }

    /// <summary>
    /// Name of the newly created family for logging and UI feedback
    /// </summary>
    public string FamilyName { get; init; } = string.Empty;

    /// <summary>
    /// Initialize message with created family details for auto-selection workflow
    /// </summary>
    /// <param name="familyId">ID of newly created family</param>
    /// <param name="familyName">Name of newly created family</param>
    public FamilyCreatedMessage(Guid familyId, string familyName)
    {
        FamilyId = familyId;
        FamilyName = familyName;
    }
}