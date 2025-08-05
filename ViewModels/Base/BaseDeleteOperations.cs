using OrchidPro.Models.Base;
using OrchidPro.Services.Base;
using OrchidPro.Extensions;
using CommunityToolkit.Mvvm.Input;

namespace OrchidPro.ViewModels.Base;

/// <summary>
/// Generic delete operations pattern for hierarchical entities with cascade validation.
/// Eliminates delete logic duplication across Family, Genus, Species and future hierarchical entities.
/// Provides standardized cascade validation and user confirmation workflows.
/// </summary>
/// <typeparam name="TEntity">Entity being deleted</typeparam>
/// <typeparam name="TChild">Child entity type (if any)</typeparam>
/// <typeparam name="TItemViewModel">Item ViewModel type for UI operations</typeparam>
public static class BaseDeleteOperations<TEntity, TChild, TItemViewModel>
    where TEntity : class, IBaseEntity
    where TChild : class, IBaseEntity, IHierarchicalEntity<TEntity>
    where TItemViewModel : BaseItemViewModel<TEntity>
{
    /// <summary>
    /// Execute delete operation with hierarchical validation for single item
    /// </summary>
    /// <param name="item">Item to delete</param>
    /// <param name="repository">Repository for the entity being deleted</param>
    /// <param name="childRepository">Repository for child entities (optional)</param>
    /// <param name="entityName">Display name for the entity type</param>
    /// <param name="childName">Display name for child entity type</param>
    /// <param name="childNamePlural">Plural display name for child entity type</param>
    /// <param name="onSuccess">Callback for successful deletion</param>
    /// <param name="showConfirmation">Function to show confirmation dialog</param>
    /// <param name="showSuccess">Function to show success message</param>
    /// <returns>Task representing the async operation</returns>
    public static async Task ExecuteSingleDeleteWithValidationAsync(
        TItemViewModel item,
        IBaseRepository<TEntity> repository,
        IHierarchicalRepository<TChild, TEntity>? childRepository,
        string entityName,
        string childName,
        string childNamePlural,
        Func<Task> onSuccess,
        Func<string, string, string, string, Task<bool>> showConfirmation,
        Func<string, string, Task> showSuccess)
    {
        if (item?.Id == null) return;

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
                    $"{entityName} '{item.Name}' has {childCount} {childText}. Deleting will also remove all {childText}. Continue?",
                    "Delete",
                    "Cancel");
            }
            else
            {
                confirmed = await showConfirmation(
                    $"Delete {entityName}",
                    $"Are you sure you want to delete '{item.Name}'?",
                    "Delete",
                    "Cancel");
            }

            if (confirmed)
            {
                await repository.DeleteAsync(item.Id);
                await onSuccess();
                await showSuccess("Deleted", $"{entityName} deleted successfully");
            }

        }, $"DeleteSingle failed for {item?.Name}");
    }

    /// <summary>
    /// Execute bulk delete operation with hierarchical validation
    /// </summary>
    /// <param name="selectedItems">Items to delete</param>
    /// <param name="repository">Repository for the entity being deleted</param>
    /// <param name="childRepository">Repository for child entities (optional)</param>
    /// <param name="entityName">Display name for the entity type</param>
    /// <param name="entityNamePlural">Plural display name for the entity type</param>
    /// <param name="childName">Display name for child entity type</param>
    /// <param name="childNamePlural">Plural display name for child entity type</param>
    /// <param name="onSuccess">Callback for successful deletion</param>
    /// <param name="showConfirmation">Function to show confirmation dialog</param>
    /// <param name="showSuccess">Function to show success message</param>
    /// <returns>Task representing the async operation</returns>
    public static async Task ExecuteBulkDeleteWithValidationAsync(
        IEnumerable<TItemViewModel> selectedItems,
        IBaseRepository<TEntity> repository,
        IHierarchicalRepository<TChild, TEntity>? childRepository,
        string entityName,
        string entityNamePlural,
        string childName,
        string childNamePlural,
        Func<Task> onSuccess,
        Func<string, string, string, string, Task<bool>> showConfirmation,
        Func<string, string, Task> showSuccess)
    {
        var itemsList = selectedItems.Where(i => i != null).ToList();
        if (!itemsList.Any()) return;

        await SafeExecuteAsync(async () =>
        {
            var totalChildCount = 0;
            if (childRepository != null)
            {
                foreach (var item in itemsList)
                {
                    var count = await childRepository.GetCountByParentAsync(item.Id, includeInactive: true);
                    totalChildCount += count;
                }
            }

            var itemCount = itemsList.Count;
            var itemText = itemCount == 1 ? entityName.ToLower() : entityNamePlural.ToLower();

            bool confirmed;
            if (totalChildCount > 0)
            {
                var childText = totalChildCount == 1 ? childName.ToLower() : childNamePlural.ToLower();
                confirmed = await showConfirmation(
                    $"Delete {itemCount} {itemText} with {childNamePlural}",
                    $"Selected {itemText} have {totalChildCount} {childText}. Deleting will also remove all {childText}. Continue?",
                    "Delete All",
                    "Cancel");
            }
            else
            {
                confirmed = await showConfirmation(
                    $"Delete {itemCount} {itemText}",
                    $"Are you sure you want to delete {itemCount} selected {itemText}?",
                    "Delete All",
                    "Cancel");
            }

            if (confirmed)
            {
                var ids = itemsList.Select(i => i.Id).ToList();
                var deletedCount = await repository.DeleteMultipleAsync(ids);

                await onSuccess();
                await showSuccess("Deleted", $"{deletedCount} {itemText} deleted successfully");
            }

        }, $"BulkDelete failed for {itemsList.Count} items");
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
            // Use extension method for logging
            ex.LogError($"Delete operation failed: {operationName}");
            throw;
        }
    }
}

/// <summary>
/// Extension methods for simplified delete operations in ViewModels
/// </summary>
public static class DeleteOperationsExtensions
{
    /// <summary>
    /// Execute single delete with validation for hierarchical entities
    /// </summary>
    public static async Task DeleteSingleWithHierarchyAsync<TEntity, TChild, TItemViewModel>(
        this TItemViewModel item,
        IBaseRepository<TEntity> repository,
        IHierarchicalRepository<TChild, TEntity>? childRepository,
        string entityName,
        string childName,
        string childNamePlural,
        Func<Task> onSuccess,
        Func<string, string, string, string, Task<bool>> showConfirmation,
        Func<string, string, Task> showSuccess)
        where TEntity : class, IBaseEntity
        where TChild : class, IBaseEntity, IHierarchicalEntity<TEntity>
        where TItemViewModel : BaseItemViewModel<TEntity>
    {
        await BaseDeleteOperations<TEntity, TChild, TItemViewModel>.ExecuteSingleDeleteWithValidationAsync(
            item, repository, childRepository, entityName, childName, childNamePlural,
            onSuccess, showConfirmation, showSuccess);
    }

    /// <summary>
    /// Execute bulk delete with validation for hierarchical entities
    /// </summary>
    public static async Task DeleteBulkWithHierarchyAsync<TEntity, TChild, TItemViewModel>(
        this IEnumerable<TItemViewModel> selectedItems,
        IBaseRepository<TEntity> repository,
        IHierarchicalRepository<TChild, TEntity>? childRepository,
        string entityName,
        string entityNamePlural,
        string childName,
        string childNamePlural,
        Func<Task> onSuccess,
        Func<string, string, string, string, Task<bool>> showConfirmation,
        Func<string, string, Task> showSuccess)
        where TEntity : class, IBaseEntity
        where TChild : class, IBaseEntity, IHierarchicalEntity<TEntity>
        where TItemViewModel : BaseItemViewModel<TEntity>
    {
        await BaseDeleteOperations<TEntity, TChild, TItemViewModel>.ExecuteBulkDeleteWithValidationAsync(
            selectedItems, repository, childRepository, entityName, entityNamePlural, childName, childNamePlural,
            onSuccess, showConfirmation, showSuccess);
    }
}