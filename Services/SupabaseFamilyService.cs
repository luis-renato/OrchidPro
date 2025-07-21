using OrchidPro.Models;
using OrchidPro.Services.Data;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Diagnostics;

namespace OrchidPro.Services;

/// <summary>
/// Modelo da Family para Supabase - SCHEMA PUBLIC (families)
/// ✅ ATUALIZADO: Adicionado campo IsFavorite
/// </summary>
[Table("families")]
public class SupabaseFamily : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_system_default")]
    public bool? IsSystemDefault { get; set; } = false;

    [Column("is_active")]
    public bool? IsActive { get; set; } = true;

    /// <summary>
    /// ✅ NOVO: Campo favorito
    /// </summary>
    [Column("is_favorite")]
    public bool? IsFavorite { get; set; } = false;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    public static SupabaseFamily FromFamily(Family family)
    {
        return new SupabaseFamily
        {
            Id = family.Id,
            UserId = family.UserId,
            Name = family.Name,
            Description = family.Description,
            IsSystemDefault = family.IsSystemDefault,
            IsActive = family.IsActive,
            IsFavorite = family.IsFavorite, // ✅ NOVO: Incluir favorito
            CreatedAt = family.CreatedAt,
            UpdatedAt = family.UpdatedAt
        };
    }

    public Family ToFamily()
    {
        return new Family
        {
            Id = this.Id,
            UserId = this.UserId,
            Name = this.Name,
            Description = this.Description,
            IsSystemDefault = this.IsSystemDefault ?? false,
            IsActive = this.IsActive ?? true,
            IsFavorite = this.IsFavorite ?? false, // ✅ NOVO: Incluir favorito
            CreatedAt = this.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = this.UpdatedAt ?? DateTime.UtcNow
        };
    }
}

/// <summary>
/// ✅ CORRIGIDO: Serviço Supabase com APIs corretas
/// </summary>
public class SupabaseFamilyService
{
    private readonly SupabaseService _supabaseService;

    public SupabaseFamilyService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    /// <summary>
    /// Busca todas as famílias do usuário atual + system defaults
    /// </summary>
    public async Task<List<Family>> GetAllAsync()
    {
        try
        {
            if (_supabaseService.Client == null)
            {
                Debug.WriteLine("❌ [FAMILY_SERVICE] Supabase client not available");
                return new List<Family>();
            }

            Debug.WriteLine("📥 [FAMILY_SERVICE] Fetching families from Supabase...");

            var currentUserIdString = _supabaseService.GetCurrentUserId();
            Debug.WriteLine($"📥 [FAMILY_SERVICE] Current user ID: {currentUserIdString}");

            // ✅ CORRIGIDO: Converter string para Guid? se necessário
            Guid? currentUserId = null;
            if (!string.IsNullOrEmpty(currentUserIdString) && Guid.TryParse(currentUserIdString, out var parsedGuid))
            {
                currentUserId = parsedGuid;
            }

            // Query todas as famílias do usuário + system defaults
            var response = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Select("*")
                .Where(f => f.UserId == currentUserId || f.UserId == null)
                .Order("is_favorite", Supabase.Postgrest.Constants.Ordering.Descending) // ✅ Favoritos primeiro
                .Order("name", Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            var families = response.Models.Select(f => f.ToFamily()).ToList();

            Debug.WriteLine($"✅ [FAMILY_SERVICE] Retrieved {families.Count} families from database");
            Debug.WriteLine($"✅ [FAMILY_SERVICE] Favorites: {families.Count(f => f.IsFavorite)}");

            return families;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] GetAllAsync failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// ✅ NOVO: Toggle favorite status of a family
    /// </summary>
    public async Task<Family> ToggleFavoriteAsync(Guid familyId)
    {
        try
        {
            Debug.WriteLine($"⭐ [FAMILY_SERVICE] Toggling favorite for family: {familyId}");

            var currentUserIdString = _supabaseService.GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserIdString))
            {
                throw new InvalidOperationException("User not authenticated");
            }

            // ✅ CORRIGIDO: Converter para Guid se necessário
            if (!Guid.TryParse(currentUserIdString, out var currentUserId))
            {
                throw new InvalidOperationException("Invalid user ID format");
            }

            // Buscar família atual
            var response = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Select("*")
                .Where(f => f.Id == familyId)
                .Single();

            if (response == null)
            {
                throw new InvalidOperationException($"Family with ID {familyId} not found");
            }

            // Toggle favorite
            var updatedFamily = new SupabaseFamily
            {
                Id = response.Id,
                IsFavorite = !(response.IsFavorite ?? false),
                UpdatedAt = DateTime.UtcNow
            };

            // Update no banco
            await _supabaseService.Client
                .From<SupabaseFamily>()
                .Where(f => f.Id == familyId)
                .Update(updatedFamily);

            Debug.WriteLine($"✅ [FAMILY_SERVICE] Family {familyId} favorite status: {updatedFamily.IsFavorite}");

            // Buscar dados atualizados
            var updatedResponse = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Select("*")
                .Where(f => f.Id == familyId)
                .Single();

            return updatedResponse.ToFamily();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] ToggleFavoriteAsync failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// ✅ NOVO: Verifica se nome já existe (para FamilyRepository)
    /// </summary>
    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
    {
        try
        {
            var currentUserIdString = _supabaseService.GetCurrentUserId();

            // ✅ CORRIGIDO: Converter string para Guid? 
            Guid? currentUserId = null;
            if (!string.IsNullOrEmpty(currentUserIdString) && Guid.TryParse(currentUserIdString, out var parsedGuid))
            {
                currentUserId = parsedGuid;
            }

            // Query sem comparação incorreta de tipos
            var response = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Select("id")
                .Where(f => f.Name.ToLower() == name.ToLower())
                .Where(f => f.UserId == currentUserId || f.UserId == null)
                .Get();

            // Se tem excludeId, filtrar localmente
            var results = response.Models.AsEnumerable();
            if (excludeId.HasValue)
            {
                results = results.Where(f => f.Id != excludeId.Value);
            }

            return results.Any();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] NameExistsAsync failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Cria nova família
    /// </summary>
    public async Task<Family> CreateAsync(Family family)
    {
        try
        {
            Debug.WriteLine($"➕ [FAMILY_SERVICE] Creating family: {family.Name}");

            var currentUserIdString = _supabaseService.GetCurrentUserId();

            // ✅ CORRIGIDO: Converter string para Guid?
            if (!string.IsNullOrEmpty(currentUserIdString) && Guid.TryParse(currentUserIdString, out var parsedGuid))
            {
                family.UserId = parsedGuid;
            }
            else
            {
                family.UserId = null; // Para system defaults
            }

            family.CreatedAt = DateTime.UtcNow;
            family.UpdatedAt = DateTime.UtcNow;

            var supabaseFamily = SupabaseFamily.FromFamily(family);

            var response = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Insert(supabaseFamily);

            var createdFamily = response.Models.First().ToFamily();

            Debug.WriteLine($"✅ [FAMILY_SERVICE] Created family: {createdFamily.Name} (ID: {createdFamily.Id})");

            return createdFamily;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] CreateAsync failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Atualiza família existente
    /// </summary>
    public async Task<Family> UpdateAsync(Family family)
    {
        try
        {
            Debug.WriteLine($"📝 [FAMILY_SERVICE] Updating family: {family.Name} (ID: {family.Id})");

            family.UpdatedAt = DateTime.UtcNow;
            var supabaseFamily = SupabaseFamily.FromFamily(family);

            await _supabaseService.Client
                .From<SupabaseFamily>()
                .Where(f => f.Id == family.Id)
                .Update(supabaseFamily);

            Debug.WriteLine($"✅ [FAMILY_SERVICE] Updated family: {family.Name}");

            return family;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] UpdateAsync failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Deleta família
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            Debug.WriteLine($"🗑️ [FAMILY_SERVICE] Deleting family: {id}");

            await _supabaseService.Client
                .From<SupabaseFamily>()
                .Where(f => f.Id == id)
                .Delete();

            Debug.WriteLine($"✅ [FAMILY_SERVICE] Deleted family: {id}");

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] DeleteAsync failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Busca família por ID
    /// </summary>
    public async Task<Family?> GetByIdAsync(Guid id)
    {
        try
        {
            Debug.WriteLine($"🔍 [FAMILY_SERVICE] Getting family by ID: {id}");

            var response = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Select("*")
                .Where(f => f.Id == id)
                .Single();

            if (response == null)
            {
                Debug.WriteLine($"⚠️ [FAMILY_SERVICE] Family not found: {id}");
                return null;
            }

            var family = response.ToFamily();
            Debug.WriteLine($"✅ [FAMILY_SERVICE] Found family: {family.Name}");

            return family;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] GetByIdAsync failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Busca famílias com filtros
    /// </summary>
    public async Task<List<Family>> GetFilteredAsync(string? searchText = null, bool? isActive = null)
    {
        try
        {
            var allFamilies = await GetAllAsync();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var search = searchText.ToLowerInvariant();
                allFamilies = allFamilies.Where(f =>
                    f.Name.ToLowerInvariant().Contains(search) ||
                    (f.Description?.ToLowerInvariant().Contains(search) == true)
                ).ToList();
            }

            if (isActive.HasValue)
            {
                allFamilies = allFamilies.Where(f => f.IsActive == isActive.Value).ToList();
            }

            // ✅ NOVO: Ordenar favoritos primeiro
            allFamilies = allFamilies
                .OrderByDescending(f => f.IsFavorite)
                .ThenBy(f => f.Name)
                .ToList();

            Debug.WriteLine($"🔍 [FAMILY_SERVICE] Filtered results: {allFamilies.Count} families");

            return allFamilies;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] GetFilteredAsync failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Testa conectividade
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            Debug.WriteLine("🔄 [FAMILY_SERVICE] Testing connection...");

            var response = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Select("id")
                .Limit(1)
                .Get(cts.Token);

            Debug.WriteLine("✅ [FAMILY_SERVICE] Connection test successful");
            return true;
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("⏰ [FAMILY_SERVICE] Connection test timeout (10s)");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] Connection test failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Calcula estatísticas das famílias
    /// </summary>
    public async Task<FamilyStatistics> GetStatisticsAsync()
    {
        try
        {
            var families = await GetAllAsync();

            return new FamilyStatistics
            {
                TotalCount = families.Count,
                ActiveCount = families.Count(f => f.IsActive),
                InactiveCount = families.Count(f => !f.IsActive),
                SystemDefaultCount = families.Count(f => f.IsSystemDefault),
                UserCreatedCount = families.Count(f => !f.IsSystemDefault),
                LastRefreshTime = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] GetStatisticsAsync failed: {ex.Message}");
            return new FamilyStatistics();
        }
    }
}