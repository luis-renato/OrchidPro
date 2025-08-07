using OrchidPro.Models.Base;
using OrchidPro.Services.Base;
using OrchidPro.Extensions;

namespace OrchidPro.ViewModels.Base;

/// <summary>
/// Generic toggle favorite pattern eliminating code duplication across all list ViewModels.
/// Provides standardized favorite toggle operations for any entity type.
/// </summary>
public static class BaseToggleFavoritePattern
{
    /// <summary>
    /// Execute toggle favorite with standard pattern used by all list ViewModels - CORRIGIDO SIMPLES
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TItemViewModel">Item ViewModel type</typeparam>
    /// <param name="item">Item to toggle favorite</param>
    /// <param name="repository">Repository with ToggleFavoriteAsync</param>
    /// <param name="items">Items collection to update</param>
    /// <param name="createItemViewModel">Function to create new item ViewModel</param>
    /// <param name="updateCounters">Action to update counters</param>
    /// <returns>Task representing the async operation</returns>
    public static async Task ExecuteToggleFavoriteAsync<TEntity, TItemViewModel>(
        TItemViewModel? item,
        IBaseRepository<TEntity> repository,
        IList<TItemViewModel> items,
        Func<TEntity, TItemViewModel> createItemViewModel,
        Action updateCounters)
        where TEntity : class, IBaseEntity, new()
        where TItemViewModel : BaseItemViewModel<TEntity>
    {
        if (item?.Id == null) return;

        await item.SafeExecuteAsync(async () =>
        {
            item.LogInfo($"Toggling favorite for: {item.Name}");

            // ✅ CORRIGIDO: Cast simples para BaseRepository
            var baseRepo = (BaseRepository<TEntity>)repository;
            var updatedEntity = await baseRepo.ToggleFavoriteAsync(item.Id);

            // Find and replace item in collection
            var index = items.IndexOf(item);
            if (index >= 0)
            {
                var newItem = createItemViewModel(updatedEntity);

                // Preserve selection state and callback
                newItem.IsSelected = item.IsSelected;
                newItem.SelectionChangedAction = item.SelectionChangedAction;

                items[index] = newItem;
                item.LogInfo($"Updated item at index {index} with new favorite status");
            }

            updateCounters();
            item.LogSuccess($"Favorite toggled: {item.Name} → {updatedEntity.IsFavorite}");

        }, $"ToggleFavorite failed for {item?.Name}");
    }
}