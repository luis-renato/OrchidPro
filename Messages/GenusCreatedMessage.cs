namespace OrchidPro.Messages;

/// <summary>
/// Message sent when a new genus is created, allowing other ViewModels to react.
/// Used for auto-selection of newly created genus in species creation flow.
/// EXACT pattern from FamilyCreatedMessage.
/// </summary>
public class GenusCreatedMessage
{
    /// <summary>
    /// The ID of the newly created genus
    /// </summary>
    public Guid GenusId { get; init; }

    /// <summary>
    /// The name of the newly created genus
    /// </summary>
    public string GenusName { get; init; } = string.Empty;

    /// <summary>
    /// Initialize message with created genus ID and name
    /// </summary>
    /// <param name="genusId">The ID of the newly created genus</param>
    /// <param name="genusName">The name of the newly created genus</param>
    public GenusCreatedMessage(Guid genusId, string genusName)
    {
        GenusId = genusId;
        GenusName = genusName;
    }
}