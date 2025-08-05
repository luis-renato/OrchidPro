namespace OrchidPro.Models.Base;

/// <summary>
/// Interface for entities that participate in hierarchical relationships.
/// Defines the contract for parent-child relationships in taxonomic or organizational structures.
/// </summary>
/// <typeparam name="TParent">The parent entity type in the hierarchy</typeparam>
public interface IHierarchicalEntity<TParent> : IBaseEntity
    where TParent : class, IBaseEntity
{
    /// <summary>
    /// Identifier of the parent entity in the hierarchy
    /// </summary>
    Guid ParentId { get; set; }

    /// <summary>
    /// Navigation property to the parent entity (optional, for eager loading)
    /// </summary>
    TParent? Parent { get; set; }

    /// <summary>
    /// Get the parent identifier value - used for generic hierarchical operations
    /// </summary>
    /// <returns>The parent entity identifier</returns>
    Guid GetParentId();

    /// <summary>
    /// Set the parent identifier value - used for generic hierarchical operations
    /// </summary>
    /// <param name="parentId">The parent entity identifier to set</param>
    void SetParentId(Guid parentId);

    /// <summary>
    /// Get display name for the parent entity (for UI purposes)
    /// </summary>
    /// <returns>Parent display name or fallback text</returns>
    string GetParentDisplayName();

    /// <summary>
    /// Validate that the hierarchical relationship is properly configured
    /// </summary>
    /// <returns>True if the relationship is valid</returns>
    bool ValidateHierarchy();
}