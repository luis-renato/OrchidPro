using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Services.Data;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using OrchidPro.Extensions;

namespace OrchidPro.Services;

/// <summary>
/// Supabase database model representing genera table with family relationship.
/// Maps between database schema and application domain models.
/// </summary>
[Table("genera")]
public class SupabaseGenus : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("family_id")]
    public Guid FamilyId { get; set; }

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
    /// Convert SupabaseGenus to domain Genus model
    /// </summary>
    public Genus ToGenus()
    {
        return new Genus
        {
            Id = this.Id,
            FamilyId = this.FamilyId,
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
    /// Convert domain Genus model to SupabaseGenus
    /// </summary>
    public static SupabaseGenus FromGenus(Genus genus)
    {
        return new SupabaseGenus
        {
            Id = genus.Id,
            FamilyId = genus.FamilyId,
            UserId = genus.UserId,
            Name = genus.Name,
            Description = genus.Description,
            IsActive = genus.IsActive,
            IsFavorite = genus.IsFavorite,
            CreatedAt = genus.CreatedAt,
            UpdatedAt = genus.UpdatedAt
        };
    }
}

/// <summary>
/// REFACTORED: Genus service implementing ISupabaseEntityService<Genus> interface.
/// Reduced from ~500 lines to minimal implementation focused on Genus-specific logic.
/// </summary>
public class SupabaseGenusService : ISupabaseEntityService<Genus>
{
    #region Private Fields

    private readonly SupabaseService _supabaseService;

    #endregion

    #region Constructor

    public SupabaseGenusService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        this.LogInfo("SupabaseGenusService initialized - implementing ISupabaseEntityService");
    }

    #endregion

    #region ISupabaseEntityService<Genus> Implementation

    public async Task<IEnumerable<Genus>> GetAllAsync()
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            if (_supabaseService.Client == null)
                return new List<Genus>();

            var currentUserId = GetCurrentUserId();
            var response = await _supabaseService.Client
                .From<SupabaseGenus>()
                .Select("*")
                .Get();

            if (response?.Models == null)
                return new List<Genus>();

            // Filter: user genera OR system defaults (UserId == null)
            var filteredGenera = response.Models.Where(sg =>
                sg.UserId == currentUserId || sg.UserId == null);

            return filteredGenera.Select(sg => sg.ToGenus()).OrderBy(g => g.Name).ToList();
        }, "Genera");

        return result.Success && result.Data != null ? result.Data : new List<Genus>();
    }

    public async Task<Genus?> GetByIdAsync(Guid id)
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            if (_supabaseService.Client == null)
                return null;

            var response = await _supabaseService.Client
                .From<SupabaseGenus>()
                .Where(g => g.Id == id)
                .Single();

            return response?.ToGenus();
        }, "Genus");

        return result.Success ? result.Data : null;
    }

    public async Task<Genus?> CreateAsync(Genus entity)
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            if (_supabaseService.Client == null)
                return null;

            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UserId = GetCurrentUserId();

            var supabaseGenus = SupabaseGenus.FromGenus(entity);
            var response = await _supabaseService.Client
                .From<SupabaseGenus>()
                .Insert(supabaseGenus);

            return response?.Models?.FirstOrDefault()?.ToGenus() ?? entity;
        }, "Genus");

        return result.Success ? result.Data : null;
    }

    public async Task<Genus?> UpdateAsync(Genus entity)
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            if (_supabaseService.Client == null)
                return null;

            entity.UpdatedAt = DateTime.UtcNow;
            var supabaseGenus = SupabaseGenus.FromGenus(entity);

            await _supabaseService.Client
                .From<SupabaseGenus>()
                .Where(g => g.Id == entity.Id)
                .Update(supabaseGenus);

            return entity;
        }, "Genus");

        return result.Success ? result.Data : null;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var result = await this.SafeDataExecuteAsync(async () =>
        {
            if (_supabaseService.Client == null)
                return false;

            await _supabaseService.Client
                .From<SupabaseGenus>()
                .Where(g => g.Id == id)
                .Delete();

            return true;
        }, "Genus");

        return result.Success && result.Data;
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
    {
        var genera = await GetAllAsync();
        return genera.Any(g =>
            string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase) &&
            g.Id != excludeId);
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