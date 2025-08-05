using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Services.Data;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using OrchidPro.Extensions;

namespace OrchidPro.Services;

/// <summary>
/// Supabase database model representing families table.
/// Maps between database schema and application domain models.
/// </summary>
[Table("families")]
public class SupabaseFamily : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; } = true;

    [Column("is_favorite")]
    public bool? IsFavorite { get; set; } = false;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Convert SupabaseFamily to domain Family model
    /// </summary>
    public Family ToFamily()
    {
        return new Family
        {
            Id = this.Id,
            UserId = this.UserId,
            Name = this.Name ?? string.Empty,
            Description = this.Description,
            IsActive = this.IsActive ?? true,
            IsFavorite = this.IsFavorite ?? false,
            CreatedAt = this.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = this.UpdatedAt ?? DateTime.UtcNow
        };
    }

    /// <summary>
    /// Convert domain Family model to SupabaseFamily
    /// </summary>
    public static SupabaseFamily FromFamily(Family family)
    {
        return new SupabaseFamily
        {
            Id = family.Id,
            UserId = family.UserId,
            Name = family.Name,
            Description = family.Description,
            IsActive = family.IsActive,
            IsFavorite = family.IsFavorite,
            CreatedAt = family.CreatedAt,
            UpdatedAt = family.UpdatedAt
        };
    }
}

/// <summary>
/// REFACTORED: Family service implementing ISupabaseEntityService<Family> interface.
/// Reduced from ~500 lines to minimal implementation focused on Family-specific logic.
/// </summary>
public class SupabaseFamilyService : ISupabaseEntityService<Family>
{
    #region Private Fields

    private readonly SupabaseService _supabaseService;

    #endregion

    #region Constructor

    public SupabaseFamilyService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        this.LogInfo("SupabaseFamilyService initialized - implementing ISupabaseEntityService");
    }

    #endregion

    #region ISupabaseEntityService<Family> Implementation

    public async Task<IEnumerable<Family>> GetAllAsync()
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            if (_supabaseService.Client == null)
                return new List<Family>();

            var currentUserId = GetCurrentUserId();
            var response = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Select("*")
                .Get();

            if (response?.Models == null)
                return new List<Family>();

            // Filter: user families OR system defaults (UserId == null)
            var filteredFamilies = response.Models.Where(sf =>
                sf.UserId == currentUserId || sf.UserId == null);

            return filteredFamilies.Select(sf => sf.ToFamily()).OrderBy(f => f.Name).ToList();
        }, "Families");

        return result.Success && result.Data != null ? result.Data : new List<Family>();
    }

    public async Task<Family?> GetByIdAsync(Guid id)
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            if (_supabaseService.Client == null)
                return null;

            var response = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Where(f => f.Id == id)
                .Single();

            return response?.ToFamily();
        }, "Family");

        return result.Success ? result.Data : null;
    }

    public async Task<Family?> CreateAsync(Family entity)
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            if (_supabaseService.Client == null)
                return null;

            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UserId = GetCurrentUserId();

            var supabaseFamily = SupabaseFamily.FromFamily(entity);
            var response = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Insert(supabaseFamily);

            return response?.Models?.FirstOrDefault()?.ToFamily() ?? entity;
        }, "Family");

        return result.Success ? result.Data : null;
    }

    public async Task<Family?> UpdateAsync(Family entity)
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            if (_supabaseService.Client == null)
                return null;

            entity.UpdatedAt = DateTime.UtcNow;
            var supabaseFamily = SupabaseFamily.FromFamily(entity);

            await _supabaseService.Client
                .From<SupabaseFamily>()
                .Where(f => f.Id == entity.Id)
                .Update(supabaseFamily);

            return entity;
        }, "Family");

        return result.Success ? result.Data : null;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            if (_supabaseService.Client == null)
                return false;

            await _supabaseService.Client
                .From<SupabaseFamily>()
                .Where(f => f.Id == id)
                .Delete();

            return true;
        }, "Family");

        return result.Success && result.Data;
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
    {
        var families = await GetAllAsync();
        return families.Any(f =>
            string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase) &&
            f.Id != excludeId);
    }

    #endregion

    #region Private Helper Methods

    private Guid? GetCurrentUserId()
    {
        var userIdString = _supabaseService.GetCurrentUserId();
        return Guid.TryParse(userIdString, out var userId) ? userId : null;
    }

    #endregion
}