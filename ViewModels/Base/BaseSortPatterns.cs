using OrchidPro.Models.Base;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Base;

/// <summary>
/// Generic sort patterns eliminating code duplication across all list ViewModels.
/// Provides standardized sorting operations with entity-specific extensions.
/// </summary>
public static class BaseSortPatterns
{
    /// <summary>
    /// Apply standard sort patterns used by all entities
    /// </summary>
    /// <typeparam name="TItemViewModel">Item ViewModel type</typeparam>
    /// <param name="filtered">Filtered items to sort</param>
    /// <param name="sortOrder">Sort order string</param>
    /// <param name="entitySpecificSort">Optional entity-specific sort function</param>
    /// <returns>Sorted items</returns>
    public static IOrderedEnumerable<TItemViewModel> ApplyStandardSort<TItemViewModel>(
        IEnumerable<TItemViewModel> filtered,
        string sortOrder,
        Func<IEnumerable<TItemViewModel>, string, IOrderedEnumerable<TItemViewModel>?>? entitySpecificSort = null)
        where TItemViewModel : BaseItemViewModel<IBaseEntity>
    {
        // Try entity-specific sort first
        if (entitySpecificSort != null)
        {
            var specificResult = entitySpecificSort(filtered, sortOrder);
            if (specificResult != null)
                return specificResult;
        }

        // Standard sort patterns used by all entities
        return sortOrder switch
        {
            "Name A→Z" => filtered.OrderBy(item => item.Name),
            "Name Z→A" => filtered.OrderByDescending(item => item.Name),
            "Recent First" => filtered.OrderByDescending(item => item.UpdatedAt),
            "Oldest First" => filtered.OrderBy(item => item.CreatedAt),
            "Favorites First" => filtered.OrderByDescending(item => item.IsFavorite).ThenBy(item => item.Name),
            _ => filtered.OrderBy(item => item.Name)
        };
    }

    /// <summary>
    /// Family-specific sort patterns
    /// </summary>
    public static IOrderedEnumerable<TItemViewModel>? ApplyFamilySpecificSort<TItemViewModel>(
        IEnumerable<TItemViewModel> filtered,
        string sortOrder)
        where TItemViewModel : class
    {
        return sortOrder switch
        {
            "Most Genera" => filtered.OrderByDescending(item => GetGenusCount(item)).ThenBy(item => GetName(item)),
            "Fewest Genera" => filtered.OrderBy(item => GetGenusCount(item)).ThenBy(item => GetName(item)),
            _ => null // Let standard sort handle it
        };
    }

    /// <summary>
    /// Get genus count from item using reflection (for Family items)
    /// </summary>
    private static int GetGenusCount<TItemViewModel>(TItemViewModel item)
    {
        var property = item?.GetType().GetProperty("GenusCount");
        return property?.GetValue(item) as int? ?? 0;
    }

    /// <summary>
    /// Get name from item using reflection
    /// </summary>
    private static string GetName<TItemViewModel>(TItemViewModel item)
    {
        var property = item?.GetType().GetProperty("Name");
        return property?.GetValue(item) as string ?? "";
    }
}