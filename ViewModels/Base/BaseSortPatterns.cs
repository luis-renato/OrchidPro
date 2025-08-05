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
    /// Apply standard sort patterns used by all entities - CORRIGIDO CONSTRAINT
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
        where TItemViewModel : class // ✅ CORRIGIDO: Constraint mais flexível
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
            "Name A→Z" => filtered.OrderBy(item => GetName(item)),
            "Name Z→A" => filtered.OrderByDescending(item => GetName(item)),
            "Recent First" => filtered.OrderByDescending(item => GetUpdatedAt(item)),
            "Oldest First" => filtered.OrderBy(item => GetCreatedAt(item)),
            "Favorites First" => filtered.OrderByDescending(item => GetIsFavorite(item)).ThenBy(item => GetName(item)),
            _ => filtered.OrderBy(item => GetName(item))
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

    #region Helper Methods Using Reflection - SAFE

    /// <summary>
    /// Get name from item using reflection safely
    /// </summary>
    private static string GetName<TItemViewModel>(TItemViewModel item)
    {
        try
        {
            var property = item?.GetType().GetProperty("Name");
            return property?.GetValue(item) as string ?? "";
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Get IsFavorite from item using reflection safely
    /// </summary>
    private static bool GetIsFavorite<TItemViewModel>(TItemViewModel item)
    {
        try
        {
            var property = item?.GetType().GetProperty("IsFavorite");
            return property?.GetValue(item) as bool? ?? false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get UpdatedAt from item using reflection safely
    /// </summary>
    private static DateTime GetUpdatedAt<TItemViewModel>(TItemViewModel item)
    {
        try
        {
            var property = item?.GetType().GetProperty("UpdatedAt");
            return property?.GetValue(item) as DateTime? ?? DateTime.MinValue;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    /// <summary>
    /// Get CreatedAt from item using reflection safely
    /// </summary>
    private static DateTime GetCreatedAt<TItemViewModel>(TItemViewModel item)
    {
        try
        {
            var property = item?.GetType().GetProperty("CreatedAt");
            return property?.GetValue(item) as DateTime? ?? DateTime.MinValue;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    /// <summary>
    /// Get genus count from item using reflection safely (for Family items)
    /// </summary>
    private static int GetGenusCount<TItemViewModel>(TItemViewModel item)
    {
        try
        {
            var property = item?.GetType().GetProperty("GenusCount");
            return property?.GetValue(item) as int? ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    #endregion
}