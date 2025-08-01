using OrchidPro.Models;
using OrchidPro.Services.Data;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using OrchidPro.Extensions;

namespace OrchidPro.Services;

/// <summary>
/// Supabase database model representing families table without system default field.
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
/// Service for managing family entities in Supabase database.
/// Provides CRUD operations and business logic for family management.
/// </summary>
public class SupabaseFamilyService
{
    private readonly SupabaseService _supabaseService;

    public SupabaseFamilyService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
        this.LogInfo("SupabaseFamilyService initialized");
    }

    /// <summary>
    /// Retrieve all families accessible to current user including system defaults
    /// </summary>
    public async Task<List<Family>> GetAllAsync()
    {
        using (this.LogPerformance("Get All Families"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                if (_supabaseService.Client == null)
                {
                    this.LogError("Supabase client not available");
                    return new List<Family>();
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
                    this.LogWarning("Could not parse user ID, will only get system families");
                }

                if (currentUserId.HasValue)
                {
                    // Query all families and filter on client side
                    var response = await _supabaseService.Client
                        .From<SupabaseFamily>()
                        .Select("*")
                        .Get();

                    this.LogInfo("Querying all families");

                    if (response?.Models == null || !response.Models.Any())
                    {
                        this.LogWarning("No families found in database");
                        return new List<Family>();
                    }

                    // Filter: user families OR system defaults (UserId == null)
                    var filteredFamilies = response.Models.Where(sf =>
                        sf.UserId == currentUserId || sf.UserId == null
                    ).ToList();

                    this.LogInfo($"Found {response.Models.Count()} total families in database");
                    this.LogInfo($"Filtered to {filteredFamilies.Count} families for user");

                    var families = filteredFamilies
                        .Select(sf => sf.ToFamily())
                        .OrderBy(f => f.Name)
                        .ToList();

                    this.LogDataOperation("Retrieved", "Families", $"{families.Count} items");

                    // Log sample families for diagnostics
                    foreach (var family in families.Take(3))
                    {
                        this.LogDebug($"Family: {family.Name} (ID: {family.Id}, Active: {family.IsActive}, Favorite: {family.IsFavorite}, System: {family.IsSystemDefault})");
                    }

                    return families;
                }
                else
                {
                    // Only system families if no authenticated user
                    var response = await _supabaseService.Client
                        .From<SupabaseFamily>()
                        .Select("*")
                        .Where(f => f.UserId == null)
                        .Get();

                    this.LogInfo("Querying only system families (no authenticated user)");

                    if (response?.Models == null || !response.Models.Any())
                    {
                        this.LogWarning("No families found in database");
                        return new List<Family>();
                    }

                    var families = response.Models
                        .Select(sf => sf.ToFamily())
                        .OrderBy(f => f.Name)
                        .ToList();

                    this.LogDataOperation("Retrieved", "System Families", $"{families.Count} items");

                    // Log sample families for diagnostics
                    foreach (var family in families.Take(3))
                    {
                        this.LogDebug($"Family: {family.Name} (ID: {family.Id}, Active: {family.IsActive}, Favorite: {family.IsFavorite}, System: {family.IsSystemDefault})");
                    }

                    return families;
                }
            }, "Families");

            if (result.Success && result.Data != null)
            {
                return result.Data;
            }
            else
            {
                this.LogError($"GetAllAsync failed: {result.Message}");
                return new List<Family>();
            }
        }
    }

    /// <summary>
    /// Check if a family name already exists in the database
    /// </summary>
    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
    {
        using (this.LogPerformance("Check Name Exists"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                this.LogInfo($"Checking if name exists: '{name}', exclude: {excludeId}");

                var allFamilies = await GetAllAsync();

                var exists = allFamilies.Any(f =>
                    string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase) &&
                    f.Id != excludeId);

                this.LogInfo($"Name '{name}' exists: {exists}");
                return exists;
            }, "Name Check");

            return result.Success && result.Data;
        }
    }

    /// <summary>
    /// Toggle favorite status for a specific family
    /// </summary>
    public async Task<Family> ToggleFavoriteAsync(Guid familyId)
    {
        using (this.LogPerformance("Toggle Favorite"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                this.LogDataOperation("Toggling favorite", "Family", familyId);

                var response = await _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Select("*")
                    .Where(f => f.Id == familyId)
                    .Single();

                if (response == null)
                {
                    throw new InvalidOperationException($"Family {familyId} not found");
                }

                var updatedFamily = new SupabaseFamily
                {
                    Id = familyId,
                    UserId = response.UserId,
                    Name = response.Name,
                    Description = response.Description,
                    IsActive = response.IsActive ?? true,
                    IsFavorite = !(response.IsFavorite ?? false),
                    UpdatedAt = DateTime.UtcNow
                };

                await _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Where(f => f.Id == familyId)
                    .Update(updatedFamily);

                this.LogDataOperation("Toggled favorite", "Family", $"{familyId} to {updatedFamily.IsFavorite}");

                var updatedResponse = await _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Select("*")
                    .Where(f => f.Id == familyId)
                    .Single();

                return updatedResponse.ToFamily();
            }, "Family");

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
                    .From<SupabaseFamily>()
                    .Select("id")
                    .Limit(1)
                    .Get();

                this.LogSuccess("Connection test successful");
                return true;
            }, "Connection Test");

            return result;
        }
    }

    /// <summary>
    /// Get statistical information about families
    /// </summary>
    public async Task<FamilyStatistics> GetStatisticsAsync()
    {
        using (this.LogPerformance("Get Statistics"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                this.LogInfo("Getting family statistics");

                var families = await GetAllAsync();

                var statistics = new FamilyStatistics
                {
                    TotalCount = families.Count,
                    ActiveCount = families.Count(f => f.IsActive),
                    InactiveCount = families.Count(f => !f.IsActive),
                    SystemDefaultCount = families.Count(f => f.IsSystemDefault),
                    UserCreatedCount = families.Count(f => !f.IsSystemDefault),
                    LastRefreshTime = DateTime.UtcNow
                };

                this.LogInfo($"Statistics: {statistics.TotalCount} total, {statistics.ActiveCount} active, {statistics.SystemDefaultCount} system");

                return statistics;
            }, "Statistics");

            if (result.Success && result.Data != null)
            {
                return result.Data;
            }
            else
            {
                this.LogError($"GetStatisticsAsync failed: {result.Message}");
                return new FamilyStatistics();
            }
        }
    }

    /// <summary>
    /// Create a new family entity in the database
    /// </summary>
    public async Task<Family> CreateAsync(Family family)
    {
        using (this.LogPerformance("Create Family"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                this.LogDataOperation("Creating", "Family", family.Name);

                family.UpdatedAt = DateTime.UtcNow;
                var supabaseFamily = SupabaseFamily.FromFamily(family);

                var response = await _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Insert(supabaseFamily);

                var createdFamily = response.Models.First().ToFamily();

                this.LogDataOperation("Created", "Family", $"{createdFamily.Name} (ID: {createdFamily.Id})");

                return createdFamily;
            }, "Family");

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
    /// Update an existing family entity in the database
    /// </summary>
    public async Task<Family> UpdateAsync(Family family)
    {
        using (this.LogPerformance("Update Family"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                this.LogDataOperation("Updating", "Family", $"{family.Name} (ID: {family.Id})");

                family.UpdatedAt = DateTime.UtcNow;
                var supabaseFamily = SupabaseFamily.FromFamily(family);

                await _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Where(f => f.Id == family.Id)
                    .Update(supabaseFamily);

                this.LogDataOperation("Updated", "Family", family.Name);

                return family;
            }, "Family");

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
    /// Delete a family entity from the database
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        using (this.LogPerformance("Delete Family"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                this.LogDataOperation("Deleting", "Family", id);

                await _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Where(f => f.Id == id)
                    .Delete();

                this.LogDataOperation("Deleted", "Family", id);

                return true;
            }, "Family");

            if (result.Success)
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
    /// Retrieve a specific family by its unique identifier
    /// </summary>
    public async Task<Family?> GetByIdAsync(Guid id)
    {
        using (this.LogPerformance("Get Family By ID"))
        {
            var result = await this.SafeDataExecuteAsync(async () =>
            {
                this.LogDataOperation("Getting", "Family", $"ID: {id}");

                var response = await _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Select("*")
                    .Where(f => f.Id == id)
                    .Single();

                if (response == null)
                {
                    this.LogWarning($"Family not found: {id}");
                    return null;
                }

                var family = response.ToFamily();
                this.LogDataOperation("Found", "Family", family.Name);

                return family;
            }, "Family");

            if (result.Success)
            {
                return result.Data;
            }
            else
            {
                this.LogWarning($"GetByIdAsync failed: {result.Message}");
                return null;
            }
        }
    }
}