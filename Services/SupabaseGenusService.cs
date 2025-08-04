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
/// Service for managing genus entities in Supabase database.
/// Provides CRUD operations and business logic for genus management with family relationships.
/// Follows the exact same pattern as SupabaseFamilyService.
/// </summary>
public class SupabaseGenusService
{
    #region Private Fields

    private readonly SupabaseService _supabaseService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initialize genus service with Supabase connection
    /// </summary>
    public SupabaseGenusService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService ?? throw new ArgumentNullException(nameof(supabaseService));
        this.LogInfo("SupabaseGenusService initialized");
    }

    #endregion

    #region Main CRUD Operations

    /// <summary>
    /// Retrieve all genera accessible to current user including system defaults
    /// </summary>
    public async Task<List<Genus>> GetAllAsync(bool includeInactive = false)
    {
        using (this.LogPerformance("Get All Genera"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                if (_supabaseService.Client == null)
                {
                    this.LogError("Supabase client not available");
                    return new List<Genus>();
                }

                this.LogInfo("Starting GetAllAsync operation");

                var currentUserIdString = _supabaseService.GetCurrentUserId();
                this.LogInfo($"Current user ID: '{currentUserIdString}'");

                // Validate and convert userId
                Guid? currentUserId = null;
                if (Guid.TryParse(currentUserIdString, out Guid parsedUserId))
                {
                    currentUserId = parsedUserId;
                    this.LogInfo($"Parsed user ID: {currentUserId}");
                }
                else
                {
                    this.LogWarning("Could not parse user ID, will only get system genera");
                }

                if (currentUserId.HasValue)
                {
                    // Query all genera and filter on client side
                    var response = await _supabaseService.Client
                        .From<SupabaseGenus>()
                        .Select("*")
                        .Get();

                    this.LogInfo("Querying all genera");

                    if (response?.Models == null || !response.Models.Any())
                    {
                        this.LogWarning("No genera found in database");
                        return new List<Genus>();
                    }

                    // Filter: user genera OR system defaults (UserId == null)
                    var filteredGenera = response.Models.Where(sg =>
                        sg.UserId == currentUserId || sg.UserId == null
                    ).ToList();

                    this.LogInfo($"Found {response.Models.Count()} total genera in database");
                    this.LogInfo($"Filtered to {filteredGenera.Count} genera for user");

                    var genera = filteredGenera
                        .Select(sg => sg.ToGenus())
                        .Where(g => includeInactive || g.IsActive)
                        .OrderBy(g => g.Name)
                        .ToList();

                    this.LogDataOperation("Retrieved", "Genera", $"{genera.Count} items");

                    return genera;
                }
                else
                {
                    // Only system genera if no authenticated user
                    var response = await _supabaseService.Client
                        .From<SupabaseGenus>()
                        .Select("*")
                        .Where(g => g.UserId == null)
                        .Get();

                    this.LogInfo("Querying only system genera (no authenticated user)");

                    if (response?.Models == null || !response.Models.Any())
                    {
                        this.LogWarning("No genera found in database");
                        return new List<Genus>();
                    }

                    var genera = response.Models
                        .Select(sg => sg.ToGenus())
                        .Where(g => includeInactive || g.IsActive)
                        .OrderBy(g => g.Name)
                        .ToList();

                    this.LogDataOperation("Retrieved", "System Genera", $"{genera.Count} items");

                    return genera;
                }
            }, "Genera");

            if (result.Success && result.Data != null)
            {
                return result.Data;
            }
            else
            {
                this.LogError($"GetAllAsync failed: {result.Message}");
                return new List<Genus>();
            }
        }
    }

    /// <summary>
    /// Get genera by family ID
    /// </summary>
    public async Task<List<Genus>> GetByFamilyIdAsync(Guid familyId, bool includeInactive = false)
    {
        using (this.LogPerformance("Get Genera By Family"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                if (_supabaseService.Client == null)
                {
                    this.LogError("Supabase client not available");
                    return new List<Genus>();
                }

                this.LogInfo($"Getting genera for family: {familyId}");

                var currentUserIdString = _supabaseService.GetCurrentUserId();
                Guid? currentUserId = null;
                if (Guid.TryParse(currentUserIdString, out Guid parsedUserId))
                {
                    currentUserId = parsedUserId;
                }

                var response = await _supabaseService.Client
                    .From<SupabaseGenus>()
                    .Select("*")
                    .Where(g => g.FamilyId == familyId)
                    .Get();

                if (response?.Models == null || !response.Models.Any())
                {
                    this.LogWarning($"No genera found for family: {familyId}");
                    return new List<Genus>();
                }

                // Filter: user genera OR system defaults (UserId == null)
                var filteredGenera = response.Models.Where(sg =>
                    (currentUserId.HasValue && sg.UserId == currentUserId) || sg.UserId == null
                ).ToList();

                var genera = filteredGenera
                    .Select(sg => sg.ToGenus())
                    .Where(g => includeInactive || g.IsActive)
                    .OrderBy(g => g.Name)
                    .ToList();

                this.LogDataOperation("Retrieved", "Genera by Family", $"{genera.Count} items for family {familyId}");

                return genera;
            }, "Genera");

            return result.Success && result.Data != null ? result.Data : new List<Genus>();
        }
    }

    /// <summary>
    /// Create new genus
    /// </summary>
    public async Task<Genus> CreateAsync(Genus genus)
    {
        using (this.LogPerformance("Create Genus"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                ArgumentNullException.ThrowIfNull(genus);

                if (_supabaseService.Client == null)
                {
                    throw new InvalidOperationException("Supabase client not available");
                }

                this.LogInfo($"Creating genus: {genus.Name} in family {genus.FamilyId}");

                var supabaseGenus = SupabaseGenus.FromGenus(genus);

                var response = await _supabaseService.Client
                    .From<SupabaseGenus>()
                    .Insert(supabaseGenus);

                if (response?.Models == null || !response.Models.Any())
                {
                    throw new InvalidOperationException("Failed to create genus");
                }

                var created = response.Models.First().ToGenus();

                this.LogDataOperation("Created", "Genus", $"{created.Name} successfully (ID: {created.Id})");
                return created;
            }, "Genus");

            if (result.Success && result.Data != null)
            {
                return result.Data;
            }
            else
            {
                throw new InvalidOperationException(result.Message);
            }
        }
    }

    /// <summary>
    /// Update existing genus
    /// </summary>
    public async Task<Genus> UpdateAsync(Genus genus)
    {
        using (this.LogPerformance("Update Genus"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                ArgumentNullException.ThrowIfNull(genus);

                if (_supabaseService.Client == null)
                {
                    throw new InvalidOperationException("Supabase client not available");
                }

                this.LogInfo($"Updating genus: {genus.Name} (ID: {genus.Id})");

                var supabaseGenus = SupabaseGenus.FromGenus(genus);

                var response = await _supabaseService.Client
                    .From<SupabaseGenus>()
                    .Where(g => g.Id == genus.Id)
                    .Update(supabaseGenus);

                if (response?.Models == null || !response.Models.Any())
                {
                    throw new InvalidOperationException("Failed to update genus");
                }

                var updated = response.Models.First().ToGenus();

                this.LogDataOperation("Updated", "Genus", $"{updated.Name} successfully");
                return updated;
            }, "Genus");

            if (result.Success && result.Data != null)
            {
                return result.Data;
            }
            else
            {
                throw new InvalidOperationException(result.Message);
            }
        }
    }

    /// <summary>
    /// Delete genus by ID
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        using (this.LogPerformance("Delete Genus"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                if (_supabaseService.Client == null)
                {
                    this.LogError("Supabase client not available");
                    return false;
                }

                this.LogInfo($"Deleting genus: {id}");

                await _supabaseService.Client
                    .From<SupabaseGenus>()
                    .Where(g => g.Id == id)
                    .Delete();

                this.LogDataOperation("Deleted", "Genus", $"{id} successfully");
                return true;
            }, "Genus");

            return result.Success && result.Data;
        }
    }

    /// <summary>
    /// Delete multiple genera by IDs
    /// </summary>
    public async Task<int> DeleteMultipleAsync(List<Guid> ids)
    {
        using (this.LogPerformance("Bulk Delete Genera"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                if (!ids.Any())
                {
                    this.LogWarning("DeleteMultipleAsync called with empty ID list");
                    return 0;
                }

                if (_supabaseService.Client == null)
                {
                    this.LogError("Supabase client not available");
                    return 0;
                }

                this.LogInfo($"Bulk deleting {ids.Count} genera");

                int deletedCount = 0;

                // Delete in batches to avoid query limits
                const int batchSize = 50;
                for (int i = 0; i < ids.Count; i += batchSize)
                {
                    var batch = ids.Skip(i).Take(batchSize).ToList();

                    await _supabaseService.Client
                        .From<SupabaseGenus>()
                        .Where(g => batch.Contains(g.Id))
                        .Delete();

                    deletedCount += batch.Count;
                }

                this.LogDataOperation("Bulk deleted", "Genera", $"{deletedCount} items");
                return deletedCount;
            }, "Genera");

            return result.Success ? result.Data : 0;
        }
    }

    #endregion

    #region Validation and Business Logic

    /// <summary>
    /// Check if genus name exists within a specific family
    /// </summary>
    public async Task<bool> NameExistsInFamilyAsync(string name, Guid familyId, Guid? excludeId = null)
    {
        using (this.LogPerformance("Check Name Exists In Family"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                if (string.IsNullOrEmpty(name))
                {
                    return false;
                }

                if (_supabaseService.Client == null)
                {
                    this.LogError("Supabase client not available");
                    return false;
                }

                this.LogInfo($"Checking if genus name exists in family: {name} in {familyId}");

                var currentUserIdString = _supabaseService.GetCurrentUserId();
                Guid? currentUserId = null;
                if (Guid.TryParse(currentUserIdString, out Guid parsedUserId))
                {
                    currentUserId = parsedUserId;
                }

                var response = await _supabaseService.Client
                    .From<SupabaseGenus>()
                    .Select("id")
                    .Where(g => g.FamilyId == familyId && g.Name.ToLower() == name.ToLower())
                    .Get();

                if (response?.Models == null || !response.Models.Any())
                {
                    return false;
                }

                // Filter and check exclusion
                var filteredGenera = response.Models.Where(sg =>
                    (currentUserId.HasValue && sg.Id != excludeId && (sg.UserId == currentUserId || sg.UserId == null)) ||
                    (!currentUserId.HasValue && sg.Id != excludeId && sg.UserId == null)
                );

                var exists = filteredGenera.Any();

                this.LogInfo($"Genus name '{name}' exists in family: {exists}");
                return exists;
            }, "Name Check");

            return result.Success && result.Data;
        }
    }

    /// <summary>
    /// Test database connectivity
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        using (this.LogPerformance("Test Connection"))
        {
            var result = await this.SafeNetworkExecuteAsync(async () =>
            {
                if (_supabaseService.Client == null)
                {
                    this.LogError("No client available");
                    return false;
                }

                var response = await _supabaseService.Client
                    .From<SupabaseGenus>()
                    .Select("id")
                    .Limit(1)
                    .Get();

                this.LogSuccess("Connection test successful");
                return true;
            }, "Connection Test");

            return result;
        }
    }

    #endregion
}