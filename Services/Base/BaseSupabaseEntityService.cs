using OrchidPro.Models.Base;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;
using Supabase.Postgrest.Models;

namespace OrchidPro.Services.Base;

/// <summary>
/// Base class for Supabase entity services implementing ISupabaseEntityService interface.
/// Eliminates code duplication by providing common database operations for all entity types.
/// </summary>
/// <typeparam name="TEntity">Domain entity type that implements IBaseEntity</typeparam>
/// <typeparam name="TSupabaseModel">Supabase model type that inherits from BaseModel</typeparam>
public abstract class BaseSupabaseEntityService<TEntity, TSupabaseModel> : ISupabaseEntityService<TEntity>
    where TEntity : class, IBaseEntity
    where TSupabaseModel : BaseModel, new()
{
    #region Protected Fields

    protected readonly SupabaseService _supabaseService;

    #endregion

    #region Abstract Members

    /// <summary>
    /// Gets the entity type name for logging purposes
    /// </summary>
    protected abstract string EntityTypeName { get; }

    /// <summary>
    /// Gets the plural entity type name for logging purposes
    /// </summary>
    protected abstract string EntityPluralName { get; }

    /// <summary>
    /// Converts Supabase model to domain entity
    /// </summary>
    /// <param name="supabaseModel">Supabase model to convert</param>
    /// <returns>Domain entity</returns>
    protected abstract TEntity ConvertToEntity(TSupabaseModel supabaseModel);

    /// <summary>
    /// Converts domain entity to Supabase model
    /// </summary>
    /// <param name="entity">Domain entity to convert</param>
    /// <returns>Supabase model</returns>
    protected abstract TSupabaseModel ConvertFromEntity(TEntity entity);

    /// <summary>
    /// Applies entity-specific setup before create (e.g., setting foreign keys)
    /// </summary>
    /// <param name="entity">Entity to setup</param>
    protected virtual void SetupEntityForCreate(TEntity entity) { }

    #endregion

    #region Constructor

    protected BaseSupabaseEntityService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        this.LogInfo($"{EntityTypeName}Service initialized - implementing ISupabaseEntityService");
    }

    #endregion

    #region ISupabaseEntityService<TEntity> Implementation

    /// <summary>
    /// Gets all entities from database with user filtering
    /// </summary>
    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            if (_supabaseService.Client == null)
                return [];

            var currentUserId = GetCurrentUserId();
            var response = await _supabaseService.Client
                .From<TSupabaseModel>()
                .Select("*")
                .Get();

            if (response?.Models == null)
                return Enumerable.Empty<TEntity>();

            // Filter: user entities OR system defaults (UserId == null)
            var filteredModels = response.Models.Where(model =>
                GetModelUserId(model) == currentUserId || GetModelUserId(model) == null);

            return [.. filteredModels
                .Select(ConvertToEntity)
                .OrderBy(entity => GetEntityName(entity))];
        }, EntityPluralName);

        return result.Success && result.Data != null ? result.Data : [];
    }

    /// <summary>
    /// Gets entity by ID
    /// </summary>
    public virtual async Task<TEntity?> GetByIdAsync(Guid id)
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            if (_supabaseService.Client == null)
                return null;

            var response = await _supabaseService.Client
                .From<TSupabaseModel>()
                .Where(model => GetModelId(model) == id)
                .Single();

            return response != null ? ConvertToEntity(response) : null;
        }, EntityTypeName);

        return result.Success ? result.Data : null;
    }

    /// <summary>
    /// Creates new entity in database
    /// </summary>
    public virtual async Task<TEntity?> CreateAsync(TEntity entity)
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            if (_supabaseService.Client == null)
                return null;

            // Setup common properties
            SetEntityId(entity, Guid.NewGuid());
            SetEntityCreatedAt(entity, DateTime.UtcNow);
            SetEntityUpdatedAt(entity, DateTime.UtcNow);
            SetEntityUserId(entity, GetCurrentUserId());

            // Apply entity-specific setup
            SetupEntityForCreate(entity);

            var supabaseModel = ConvertFromEntity(entity);
            var response = await _supabaseService.Client
                .From<TSupabaseModel>()
                .Insert(supabaseModel);

            return response?.Models?.FirstOrDefault() != null
                ? ConvertToEntity(response.Models.First())
                : entity;
        }, EntityTypeName);

        return result.Success ? result.Data : null;
    }

    /// <summary>
    /// Updates existing entity in database
    /// </summary>
    public virtual async Task<TEntity?> UpdateAsync(TEntity entity)
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            if (_supabaseService.Client == null)
                return null;

            SetEntityUpdatedAt(entity, DateTime.UtcNow);
            var supabaseModel = ConvertFromEntity(entity);

            await _supabaseService.Client
                .From<TSupabaseModel>()
                .Where(model => GetModelId(model) == GetEntityId(entity))
                .Update(supabaseModel);

            return entity;
        }, EntityTypeName);

        return result.Success ? result.Data : null;
    }

    /// <summary>
    /// Deletes entity from database
    /// </summary>
    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            if (_supabaseService.Client == null)
                return false;

            await _supabaseService.Client
                .From<TSupabaseModel>()
                .Where(model => GetModelId(model) == id)
                .Delete();

            return true;
        }, EntityTypeName);

        return result.Success && result.Data;
    }

    /// <summary>
    /// Checks if entity name already exists
    /// </summary>
    public virtual async Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
    {
        var entities = await GetAllAsync();
        return entities.Any(entity =>
            string.Equals(GetEntityName(entity), name, StringComparison.OrdinalIgnoreCase) &&
            GetEntityId(entity) != excludeId);
    }

    #endregion

    #region Protected Helper Methods

    /// <summary>
    /// Gets current user ID from Supabase service
    /// </summary>
    protected Guid? GetCurrentUserId()
    {
        var userIdString = _supabaseService.GetCurrentUserId();
        return Guid.TryParse(userIdString, out var userId) ? userId : null;
    }

    #endregion

    #region Property Accessors (using dynamic for flexibility)

    protected virtual Guid GetModelId(TSupabaseModel model) => ((dynamic)model).Id;
    protected virtual Guid? GetModelUserId(TSupabaseModel model) => ((dynamic)model).UserId;
    protected virtual Guid GetEntityId(TEntity entity) => ((dynamic)entity).Id;
    protected virtual void SetEntityId(TEntity entity, Guid id) => ((dynamic)entity).Id = id;
    protected virtual string GetEntityName(TEntity entity) => ((dynamic)entity).Name ?? string.Empty;
    protected virtual void SetEntityUserId(TEntity entity, Guid? userId) => ((dynamic)entity).UserId = userId;
    protected virtual void SetEntityCreatedAt(TEntity entity, DateTime createdAt) => ((dynamic)entity).CreatedAt = createdAt;
    protected virtual void SetEntityUpdatedAt(TEntity entity, DateTime updatedAt) => ((dynamic)entity).UpdatedAt = updatedAt;

    #endregion
}