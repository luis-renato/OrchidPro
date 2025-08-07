namespace OrchidPro.Messages;

/// <summary>
/// Message sent when a new family is created
/// </summary>
public class FamilyCreatedMessage
{
    public Guid FamilyId { get; init; }
    public string FamilyName { get; init; } = string.Empty;

    public FamilyCreatedMessage(Guid familyId, string familyName)
    {
        FamilyId = familyId;
        FamilyName = familyName;
    }
}