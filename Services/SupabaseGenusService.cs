using OrchidPro.Models;
using OrchidPro.Services.Data;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using OrchidPro.Extensions;

namespace OrchidPro.Services;

/// <summary>
/// Supabase database model representing genera table
/// Maps between database schema and application domain models
/// </summary>
[Table("genera")]
public class SupabaseGenus : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("family_id")]
    public Guid FamilyId { get; set; }

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
            UserId = this.UserId,
            FamilyId = this.FamilyId,
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
            UserId = genus.UserId,
            FamilyId = genus.FamilyId,
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
/// Service for managing genus entities in Supabase database
/// Provides CRUD operations and business logic for genus management
/// </summary>
public class SupabaseGenusService
{
    #region Private Fields

    private readonly SupabaseService _supabaseService;

    #endregion

    #region Constructor

    public SupabaseGenusService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
        this.LogInfo("SupabaseGenusService initialized");
    }

    #endregion

    #region CRUD Operations

    /// <summary>
    /// Get all genera with family names included
    /// </summary>
    public async Task<List<Genus>> GetAllWithFamilyAsync(bool includeInactive = false)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            var client = _supabaseService.Client;
            if (client == null) throw new InvalidOperationException("Supabase client not initialized");

            var query = client.From<SupabaseGenus>()
                .Select("*, families!inner(name)")
                .Order("name");

            if (!includeInactive)
            {
                query = query.Where(g => g.IsActive == true);
            }

            var response = await query.Get();
            var results = new List<Genus>();

            foreach (var item in response.Models)
            {
                var genus = item.ToGenus();

                // Extract family name from joined data
                if (response.Content != null)
                {
                    try
                    {
                        var jsonData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(response.Content);
                        var genusData = jsonData.FirstOrDefault(j =>
                            j.TryGetProperty("id", out var idProp) &&
                            idProp.GetString() == genus.Id.ToString());

                        if (genusData.ValueKind != System.Text.Json.JsonValueKind.Undefined &&
                            genusData.TryGetProperty("families", out var familyData))
                        {
                            genus.FamilyName = familyData.GetProperty("name").GetString() ?? string.Empty;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.LogWarning($"Failed to parse family data for genus {genus.Name}: {ex.Message}");
                    }
                }

                results.Add(genus);
            }

            return results;
        }, "Get All Genera With Family") ?? new List<Genus>();
    }

    /// <summary>
    /// Get genera filtered by family
    /// </summary>
    public async Task<List<Genus>> GetByFamilyAsync(Guid familyId, bool includeInactive = false)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            var client = _supabaseService.Client;
            if (client == null) throw new InvalidOperationException("Supabase client not initialized");

            var query = client.From<SupabaseGenus>()
                .Select("*, families!inner(name)")
                .Where(g => g.FamilyId == familyId)
                .Order("name");

            if (!includeInactive)
            {
                query = query.Where(g => g.IsActive == true);
            }

            var response = await query.Get();
            var results = new List<Genus>();

            foreach (var item in response.Models)
            {
                var genus = item.ToGenus();

                // Extract family name from joined data
                if (response.Content != null)
                {
                    try
                    {
                        var jsonData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(response.Content);
                        var genusData = jsonData.FirstOrDefault(j =>
                            j.TryGetProperty("id", out var idProp) &&
                            idProp.GetString() == genus.Id.ToString());

                        if (genusData.ValueKind != System.Text.Json.JsonValueKind.Undefined &&
                            genusData.TryGetProperty("families", out var familyData))
                        {
                            genus.FamilyName = familyData.GetProperty("name").GetString() ?? string.Empty;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.LogWarning($"Failed to parse family data for genus {genus.Name}: {ex.Message}");
                    }
                }

                results.Add(genus);
            }

            return results;
        }, "Get Genera By Family") ?? new List<Genus>();
    }

    /// <summary>
    /// Get filtered genera with advanced search
    /// </summary>
    public async Task<List<Genus>> GetFilteredAsync(string? searchText = null, bool? isActive = null, bool? isFavorite = null, Guid? familyId = null)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            var client = _supabaseService.Client;
            if (client == null) throw new InvalidOperationException("Supabase client not initialized");

            var query = client.From<SupabaseGenus>()
                .Select("*, families!inner(name)")
                .Order("name");

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(g => g.Name.Contains(searchText) || g.Description.Contains(searchText));
            }

            if (isActive.HasValue)
            {
                query = query.Where(g => g.IsActive == isActive.Value);
            }

            if (isFavorite.HasValue)
            {
                query = query.Where(g => g.IsFavorite == isFavorite.Value);
            }

            if (familyId.HasValue)
            {
                query = query.Where(g => g.FamilyId == familyId.Value);
            }

            var response = await query.Get();
            var results = new List<Genus>();

            foreach (var item in response.Models)
            {
                var genus = item.ToGenus();

                // Extract family name from joined data
                if (response.Content != null)
                {
                    try
                    {
                        var jsonData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(response.Content);
                        var genusData = jsonData.FirstOrDefault(j =>
                            j.TryGetProperty("id", out var idProp) &&
                            idProp.GetString() == genus.Id.ToString());

                        if (genusData.ValueKind != System.Text.Json.JsonValueKind.Undefined &&
                            genusData.TryGetProperty("families", out var familyData))
                        {
                            genus.FamilyName = familyData.GetProperty("name").GetString() ?? string.Empty;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.LogWarning($"Failed to parse family data for genus {genus.Name}: {ex.Message}");
                    }
                }

                results.Add(genus);
            }

            return results;
        }, "Get Filtered Genera") ?? new List<Genus>();
    }

    /// <summary>
    /// Get genus by ID with family name
    /// </summary>
    public async Task<Genus?> GetByIdAsync(Guid id)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            var client = _supabaseService.Client;
            if (client == null) throw new InvalidOperationException("Supabase client not initialized");

            var response = await client.From<SupabaseGenus>()
                .Select("*, families!inner(name)")
                .Where(g => g.Id == id)
                .Single();

            if (response == null) return null;

            var genus = response.ToGenus();

            // Extract family name from response
            // Note: For single responses, family name extraction might be different

            return genus;
        }, "Get Genus By ID");
    }

    /// <summary>
    /// Create new genus
    /// </summary>
    public async Task<Genus?> CreateAsync(Genus genus)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            var client = _supabaseService.Client;
            if (client == null) throw new InvalidOperationException("Supabase client not initialized");

            var supabaseGenus = SupabaseGenus.FromGenus(genus);
            supabaseGenus.CreatedAt = DateTime.UtcNow;
            supabaseGenus.UpdatedAt = DateTime.UtcNow;

            var response = await client.From<SupabaseGenus>()
                .Insert(supabaseGenus);

            return response.Models.FirstOrDefault()?.ToGenus();
        }, "Create Genus");
    }

    /// <summary>
    /// Update existing genus
    /// </summary>
    public async Task<Genus?> UpdateAsync(Genus genus)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            var client = _supabaseService.Client;
            if (client == null) throw new InvalidOperationException("Supabase client not initialized");

            var supabaseGenus = SupabaseGenus.FromGenus(genus);
            supabaseGenus.UpdatedAt = DateTime.UtcNow;

            var response = await client.From<SupabaseGenus>()
                .Where(g => g.Id == genus.Id)
                .Update(supabaseGenus);

            return response.Models.FirstOrDefault()?.ToGenus();
        }, "Update Genus");
    }

    /// <summary>
    /// Delete genus
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            var client = _supabaseService.Client;
            if (client == null) throw new InvalidOperationException("Supabase client not initialized");

            await client.From<SupabaseGenus>()
                .Where(g => g.Id == id)
                .Delete();

            return true;
        }, "Delete Genus") ?? false;
    }

    /// <summary>
    /// Toggle favorite status
    /// </summary>
    public async Task<Genus?> ToggleFavoriteAsync(Guid id)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            var client = _supabaseService.Client;
            if (client == null) throw new InvalidOperationException("Supabase client not initialized");

            // First get current status
            var current = await client.From<SupabaseGenus>()
                .Where(g => g.Id == id)
                .Single();

            if (current == null) return null;

            // Toggle favorite
            current.IsFavorite = !(current.IsFavorite ?? false);
            current.UpdatedAt = DateTime.UtcNow;

            var response = await client.From<SupabaseGenus>()
                .Where(g => g.Id == id)
                .Update(current);

            return response.Models.FirstOrDefault()?.ToGenus();
        }, "Toggle Genus Favorite");
    }

    /// <summary>
    /// Check if genus name exists in family
    /// </summary>
    public async Task<bool> ExistsInFamilyAsync(string name, Guid familyId, Guid? excludeId = null)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            var client = _supabaseService.Client;
            if (client == null) throw new InvalidOperationException("Supabase client not initialized");

            var query = client.From<SupabaseGenus>()
                .Where(g => g.Name.ToLower() == name.ToLower() && g.FamilyId == familyId);

            if (excludeId.HasValue)
            {
                query = query.Where(g => g.Id != excludeId.Value);
            }

            var response = await query.Get();
            return response.Models.Any();
        }, "Check Genus Exists In Family") ?? false;
    }

    /// <summary>
    /// Get count by family
    /// </summary>
    public async Task<int> GetCountByFamilyAsync(Guid familyId, bool includeInactive = false)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            var client = _supabaseService.Client;
            if (client == null) throw new InvalidOperationException("Supabase client not initialized");

            var query = client.From<SupabaseGenus>()
                .Where(g => g.FamilyId == familyId);

            if (!includeInactive)
            {
                query = query.Where(g => g.IsActive == true);
            }

            var response = await query.Get();
            return response.Models.Count;
        }, "Get Genus Count By Family") ?? 0;
    }

    /// <summary>
    /// Delete all genera in a family
    /// </summary>
    public async Task<bool> DeleteByFamilyAsync(Guid familyId)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            var client = _supabaseService.Client;
            if (client == null) throw new InvalidOperationException("Supabase client not initialized");

            await client.From<SupabaseGenus>()
                .Where(g => g.FamilyId == familyId)
                .Delete();

            return true;
        }, "Delete Genera By Family") ?? false;
    }

    #endregion
}using OrchidPro.Models;
using OrchidPro.Services.Data;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using OrchidPro.Extensions;

namespace OrchidPro.Services;

/// <summary>
/// Supabase database model representing genera table
/// Maps between database schema and application domain models
/// </summary>
[Table("genera")]
public class SupabaseGenus : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("family_id")]
    public Guid FamilyId { get; set; }

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
            UserId = this.UserId,
            FamilyId = this.FamilyId,
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
            UserId = genus.UserId,
            FamilyId = genus.FamilyId,
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
/// Service for managing genus entities in Supabase database
/// Provides CRUD operations and business logic for genus management
/// </summary>
public class SupabaseGenusService
{
    #region Private Fields

    private readonly SupabaseService _supabaseService;

    #endregion

    #region Constructor

    public SupabaseGenusService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
        this.LogInfo("SupabaseGenusService initialized");
    }

    #endregion

    #region CRUD Operations

    /// <summary>
    /// Get all genera with family names included
    /// </summary>
    public async Task<List<Genus>> GetAllWithFamilyAsync(bool includeInactive = false)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            var client = await _supabaseService.GetClientAsync();
            var query = client.From<SupabaseGenus>()
                .Select("*, families!inner(name)")
                .Order("name");

            if (!includeInactive)
            {
                query = query.Where(g => g.IsActive == true);
            }

            var response = await query.Get();
            var results = new List<Genus>();

            foreach (var item in response.Models)
            {
                var genus = item.ToGenus();

                // Extract family name from joined data
                if (response.Content != null)
                {
                    var jsonData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(response.Content);
                    var genusData = jsonData.FirstOrDefault(j => j.GetProperty("id").GetString() == genus.Id.ToString());

                    if (genusData.ValueKind != System.Text.Json.JsonValueKind.Undefined &&
                        genusData.TryGetProperty("families", out var familyData))
                    {
                        genus.FamilyName = familyData.GetProperty("name").GetString() ?? string.Empty;
                    }
                }

                results.Add(genus);
            }

            return results;
        }, "Get All Genera With Family") ?? new List<Genus>();
    }

    /// <summary>
    /// Get genera filtered by family
    /// </summary>
    public async Task<List<Genus>> GetByFamilyAsync(Guid familyId, bool includeInactive = false)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            var client = await _supabaseService.GetClientAsync();
            var query = client.From<SupabaseGenus>()
                .Select("*, families!inner(name)")
                .Where(g => g.FamilyId == familyId)
                .Order("name");

            if (!includeInactive)
            {
                query = query.Where(g => g.IsActive == true);
            }

            var response = await query.Get();
            var results = new List<Genus>();

            foreach (var item in response.Models)
            {
                var genus = item.ToGenus();

                // Extract family name from joined data
                if (response.Content != null)
                {
                    var jsonData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(response.Content);
                    var genusData = jsonData.FirstOrDefault(j => j.GetProperty("id").GetString() == genus.Id.ToString());

                    if (genusData.ValueKind != System.Text.Json.JsonValueKind.Undefined &&
                        genusData.TryGetProperty("families", out var familyData))
                    {
                        genus.FamilyName = familyData.GetProperty("name").GetString() ?? string.Empty;
                    }
                }

                results.Add(genus);
            }

            return results;
        }, "Get Genera By Family") ?? new List<Genus>();
    }

    /// <summary>
    /// Get filtered genera with advanced search
    /// </summary>
    public async Task<List<Genus>> GetFilteredAsync(string? searchText = null, bool? isActive = null, bool? isFavorite = null, Guid? familyId = null)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            var client = await _supabaseService.GetClientAsync();
            var query = client.From<SupabaseGenus>()
                .Select("*, families!inner(name)")
                .Order("name");

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(g => g.Name.Contains(searchText) || g.Description.Contains(searchText));
            }

            if (isActive.HasValue)
            {
                query = query.Where(g => g.IsActive == isActive.Value);
            }

            if (isFavorite.HasValue)
            {
                query = query.Where(g => g.IsFavorite == isFavorite.Value);
            }

            if (familyId.HasValue)
            {
                query = query.Where(g => g.FamilyId == familyId.Value);
            }

            var response = await query.Get();
            var results = new List<Genus>();

            foreach (var item in response.Models)
            {
                var genus = item.ToGenus();

                // Extract family name from joined data
                if (response.Content != null)
                {
                    var jsonData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(response.Content);
                    var genusData = jsonData.FirstOrDefault(j => j.GetProperty("id").GetString() == genus.Id.ToString());

                    if (genusData.ValueKind != System.Text.Json.JsonValueKind.Undefined &&
                        genusData.TryGetProperty("families", out var familyData))
                    {
                        genus.FamilyName = familyData.GetProperty("name").GetString() ?? string.Empty;
                    }
                }

                results.Add(genus);
            }

            return results;
        }, "Get Filtered Genera") ?? new List<Genus>();
    }

    /// <summary>
    /// Get genus by ID with family name
    /// </summary>
    public async Task<Genus?> GetByIdAsync(Guid id)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            var client = await _supabaseService.GetClientAsync();
            var response = await client.From<SupabaseGenus>()
                .Select("*, families!inner(name)")
                .Where(g => g.Id == id)
                .Single();

            if (response == null) return null;

            var genus = response.ToGenus();

            // TODO: Extract family name from joined data when Supabase supports it better

            return genus;
        }, "Get Genus By ID");
    }

    /// <summary>
    /// Create new genus
    /// </summary>
    public async Task<Genus?> CreateAsync(Genus genus)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            var client = await _supabaseService.GetClientAsync();
            var supabaseGenus = SupabaseGenus.FromGenus(genus);
            supabaseGenus.CreatedAt = DateTime.UtcNow;
            supabaseGenus.UpdatedAt = DateTime.UtcNow;

            var response = await client.From<SupabaseGenus>()
                .Insert(supabaseGenus);

            return response.Models.FirstOrDefault()?.ToGenus();
        }, "Create Genus");
    }

    /// <summary>
    /// Update existing genus
    /// </summary>
    public async Task<Genus?> UpdateAsync(Genus genus)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            var client = await _supabaseService.GetClientAsync();
            var supabaseGenus = SupabaseGenus.FromGenus(genus);
            supabaseGenus.UpdatedAt = DateTime.UtcNow;

            var response = await client.From<SupabaseGenus>()
                .Where(g => g.Id == genus.Id)
                .Update(supabaseGenus);

            return response.Models.FirstOrDefault()?.ToGenus();
        }, "Update Genus");
    }

    /// <summary>
    /// Delete genus
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            var client = await _supabaseService.GetClientAsync();
            await client.From<SupabaseGenus>()
                .Where(g => g.Id == id)
                .Delete();

            return true;
        }, "Delete Genus") ?? false;
    }

    /// <summary>
    /// Toggle favorite status
    /// </summary>
    public async Task<Genus?> ToggleFavoriteAsync(Guid id)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            var client = await _supabaseService.GetClientAsync();

            // First get current status
            var current = await client.From<SupabaseGenus>()
                .Where(g => g.Id == id)
                .Single();

            if (current == null) return null;

            // Toggle favorite
            current.IsFavorite = !(current.IsFavorite ?? false);
            current.UpdatedAt = DateTime.UtcNow;

            var response = await client.From<SupabaseGenus>()
                .Where(g => g.Id == id)
                .Update(current);

            return response.Models.FirstOrDefault()?.ToGenus();
        }, "Toggle Genus Favorite");
    }

    /// <summary>
    /// Check if genus name exists in family
    /// </summary>
    public async Task<bool> ExistsInFamilyAsync(string name, Guid familyId, Guid? excludeId = null)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            var client = await _supabaseService.GetClientAsync();
            var query = client.From<SupabaseGenus>()
                .Where(g => g.Name.ToLower() == name.ToLower() && g.FamilyId == familyId);

            if (excludeId.HasValue)
            {
                query = query.Where(g => g.Id != excludeId.Value);
            }

            var response = await query.Get();
            return response.Models.Any();
        }, "Check Genus Exists In Family") ?? false;
    }

    /// <summary>
    /// Get count by family
    /// </summary>
    public async Task<int> GetCountByFamilyAsync(Guid familyId, bool includeInactive = false)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            var client = await _supabaseService.GetClientAsync();
            var query = client.From<SupabaseGenus>()
                .Where(g => g.FamilyId == familyId);

            if (!includeInactive)
            {
                query = query.Where(g => g.IsActive == true);
            }

            var response = await query.Get();
            return response.Models.Count;
        }, "Get Genus Count By Family") ?? 0;
    }

    /// <summary>
    /// Delete all genera in a family
    /// </summary>
    public async Task<bool> DeleteByFamilyAsync(Guid familyId)
    {
        return await this.SafeDataExecuteAsync(async () =>
        {
            var client = await _supabaseService.GetClientAsync();
            await client.From<SupabaseGenus>()
                .Where(g => g.FamilyId == familyId)
                .Delete();

            return true;
        }, "Delete Genera By Family") ?? false;
    }

    #endregion
}