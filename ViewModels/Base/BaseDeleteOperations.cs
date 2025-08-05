using OrchidPro.Models.Base;
using OrchidPro.Services.Base;
using OrchidPro.Extensions;
using CommunityToolkit.Mvvm.Input;

namespace OrchidPro.ViewModels.Base;

/// <summary>
/// Generic delete operations pattern for all entities with optional cascade validation.
/// Eliminates delete logic duplication across all list ViewModels.
/// Provides standardized cascade validation and user confirmation workflows.
/// </summary>
public static class BaseDeleteOperations
{
    /// <summary>
    /// Execute simple delete operation for entities without children
    /// </summary>
    /// <typeparam name="TEntity">Entity being deleted</typeparam>
    /// <typeparam name="TItemViewModel">Item ViewModel type</typeparam>
    /// <param name="item">Item to delete</param>
    /// <param name="repository">Repository for the entity being deleted</param>
    /// <param name="entityName">Display name for the entity type</param>
    /// <param name="items">Items collection to update</param>
    /// <param name="updateCounters">Action to update counters</param>
    /// <param name="showConfirmation">Function to show confirmation dialog</param>
    /// <param name="showSuccess">Function to show success message</param>
    /// <returns>Task representing the async operation</returns>
    public static async Task ExecuteSimpleDeleteAsync<TEntity, TItemViewModel>(
        TItemViewModel? item,
        IBaseRepository<TEntity> repository,
        string entityName,
        IList<TItemViewModel> items,
        Action updateCounters,
        Func<string, string, Task<bool>> showConfirmation,
        Func<string, Task> showSuccess)
        where TEntity : class, IBaseEntity
        where TItemViewModel : BaseItemViewModel<TEntity>
    {
        if (item == null) return;

        await SafeExecuteAsync(async () =>
        {
            var confirmed = await showConfirmation(
                $"Delete {entityName}",
                $"Are you sure you want to delete '{item.Name}'?");

            if (confirmed)
            {
                await repository.DeleteAsync(item.Id);
                items.Remove(item);
                updateCounters();
                await showSuccess($"'{item.Name}' deleted successfully");
            }

        }, $"SimpleDelete failed for {item?.Name}");
    }

    /// <summary>
    /// Execute delete operation with hierarchical validation for entities with children
    /// </summary>
    /// <typeparam name="TEntity">Entity being deleted</typeparam>
    /// <typeparam name="TChild">Child entity type</typeparam>
    /// <typeparam name="TItemViewModel">Item ViewModel type</typeparam>
    /// <param name="item">Item to delete</param>
    /// <param name="repository">Repository for the entity being deleted</param>
    /// <param name="childRepository">Repository for child entities</param>
    /// <param name="entityName">Display name for the entity type</param>
    /// <param name="childName">Display name for child entity type</param>
    /// <param name="childNamePlural">Plural display name for child entity type</param>
    /// <param name="items">Items collection to update</param>
    /// <param name="updateCounters">Action to update counters</param>
    /// <param name="showConfirmation">Function to show confirmation dialog</param>
    /// <param name="showSuccess">Function to show success message</param>
    /// <returns>Task representing the async operation</returns>
    public static async Task ExecuteHierarchicalDeleteAsync<TEntity, TChild, TItemViewModel>(
        TItemViewModel? item,
        IBaseRepository<TEntity> repository,
        IHierarchicalRepository<TChild, TEntity>? childRepository,
        string entityName,
        string childName,
        string childNamePlural,
        IList<TItemViewModel> items,
        Action updateCounters,
        Func<string, string, Task<bool>> showConfirmation,
        Func<string, Task> showSuccess)
        where TEntity : class, IBaseEntity
        where TChild : class, IBaseEntity, IHierarchicalEntity<TEntity>
        where TItemViewModel : BaseItemViewModel<TEntity>
    {
        if (item == null) return;

        await SafeExecuteAsync(async () =>
        {
            var childCount = 0;
            if (childRepository != null)
            {
                childCount = await childRepository.GetCountByParentAsync(item.Id, includeInactive: true);
            }

            bool confirmed;
            if (childCount > 0)
            {
                var childText = childCount == 1 ? childName.ToLower() : childNamePlural.ToLower();
                confirmed = await showConfirmation(
                    $"Delete {entityName} with {childNamePlural}",
                    $"{entityName} '{item.Name}' has {childCount} {childText}. Deleting will also remove all {childText}. Continue?");
            }
            else
            {
                confirmed = await showConfirmation(
                    $"Delete {entityName}",
                    $"Are you sure you want to delete '{item.Name}'?");
            }

            if (confirmed)
            {
                await repository.DeleteAsync(item.Id);
                items.Remove(item);
                updateCounters();
                await showSuccess($"'{item.Name}' deleted successfully");
            }

        }, $"HierarchicalDelete failed for {item?.Name}");
    }

    /// <summary>
    /// Safe execution wrapper with logging
    /// </summary>
    private static async Task SafeExecuteAsync(Func<Task> operation, string operationName)
    {
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            ex.LogError($"Delete operation failed: {operationName}");
            throw;
        }
    }
}