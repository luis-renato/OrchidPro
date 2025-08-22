using OrchidPro.Models;
using OrchidPro.Models.Base;

namespace OrchidPro.Services;

/// <summary>
/// Repository interface for Variant entity operations.
/// Follows exact pattern of IFamilyRepository since Variant is an independent entity (no hierarchy).
/// Provides standardized data access patterns using base repository functionality.
/// </summary>
public interface IVariantRepository : IBaseRepository<Variant>
{
    // No additional methods needed - Variant is independent like Family
    // All functionality comes from IBaseRepository<Variant>:
    // - GetAllAsync(bool includeInactive = false)
    // - GetFilteredAsync(string? searchText = null, bool? statusFilter = null)
    // - GetByIdAsync(Guid id)
    // - CreateAsync(Variant variant)
    // - UpdateAsync(Variant variant)
    // - DeleteAsync(Guid id)
    // - DeleteMultipleAsync(List<Guid> ids)
    // - GetFavoritesAsync()
    // - ToggleFavoriteAsync(Guid id)
    // - NameExistsAsync(string name, Guid? excludeId = null)
    // - RefreshCacheAsync()
    // - TestConnectionAsync()
}