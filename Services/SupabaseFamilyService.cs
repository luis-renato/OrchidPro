using System.Diagnostics;
using OrchidPro.Models;
using OrchidPro.Services.Data;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace OrchidPro.Services;

/// <summary>
/// ✅ CORRIGIDO: Model Supabase com todos os campos da tabela families
/// </summary>
[Table("families")]
public class SupabaseFamily : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid? UserId { get; set; } // ✅ Nullable para system defaults

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_system_default")]
    public bool? IsSystemDefault { get; set; } = false;

    [Column("is_active")]
    public bool? IsActive { get; set; } = true;

    [Column("is_favorite")] // ✅ NOVO: Campo favorito
    public bool? IsFavorite { get; set; } = false;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ✅ CORRIGIDO: Converte SupabaseFamily para Family
    /// </summary>
    public Family ToFamily()
    {
        return new Family
        {
            Id = this.Id,
            UserId = this.UserId,
            Name = this.Name ?? string.Empty,
            Description = this.Description,
            IsSystemDefault = this.IsSystemDefault.HasValue ? this.IsSystemDefault.Value : false,
            IsActive = this.IsActive.HasValue ? this.IsActive.Value : true,
            IsFavorite = this.IsFavorite.HasValue ? this.IsFavorite.Value : false,
            CreatedAt = this.CreatedAt.HasValue ? this.CreatedAt.Value : DateTime.UtcNow,
            UpdatedAt = this.UpdatedAt.HasValue ? this.UpdatedAt.Value : DateTime.UtcNow
        };
    }

    /// <summary>
    /// ✅ CORRIGIDO: Converte Family para SupabaseFamily
    /// </summary>
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
            IsFavorite = family.IsFavorite,
            CreatedAt = family.CreatedAt,
            UpdatedAt = family.UpdatedAt
        };
    }
}

/// <summary>
/// ✅ CORRIGIDO: Serviço Supabase com correção para UUID vazio
/// </summary>
public class SupabaseFamilyService
{
    private readonly SupabaseService _supabaseService;

    public SupabaseFamilyService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    /// <summary>
    /// ✅ CORRIGIDO: Busca todas as famílias com proteção contra UUID vazio
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

            // ✅ CORREÇÃO PRINCIPAL: Validar e converter userId corretamente
            Guid? currentUserId = null;

            if (!string.IsNullOrEmpty(currentUserIdString) && !string.IsNullOrWhiteSpace(currentUserIdString))
            {
                if (Guid.TryParse(currentUserIdString, out var parsedGuid))
                {
                    currentUserId = parsedGuid;
                    Debug.WriteLine($"✅ [FAMILY_SERVICE] Parsed user ID: {currentUserId}");
                }
                else
                {
                    Debug.WriteLine($"❌ [FAMILY_SERVICE] Invalid user ID format: '{currentUserIdString}'");
                    return new List<Family>();
                }
            }
            else
            {
                Debug.WriteLine("❌ [FAMILY_SERVICE] User ID is null or empty - user not authenticated");
                return new List<Family>();
            }

            // ✅ CORREÇÃO SIMPLES: Buscar todas as famílias e filtrar no cliente
            Debug.WriteLine("🔍 [FAMILY_SERVICE] Fetching all families...");
            var response = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Select("*")
                .Get();

            if (response?.Models == null || !response.Models.Any())
            {
                Debug.WriteLine("📝 [FAMILY_SERVICE] No families found in database");
                return new List<Family>();
            }

            // ✅ Filtrar no cliente: famílias do usuário OU system defaults
            var filteredFamilies = response.Models.Where(sf =>
                sf.UserId == currentUserId.Value || sf.UserId == null
            ).ToList();

            Debug.WriteLine($"✅ [FAMILY_SERVICE] Found {response.Models.Count()} total families");
            Debug.WriteLine($"✅ [FAMILY_SERVICE] Filtered to {filteredFamilies.Count} families for user");

            var families = filteredFamilies
                .Select(sf => sf.ToFamily())
                .OrderBy(f => f.Name)
                .ToList();

            Debug.WriteLine($"✅ [FAMILY_SERVICE] Retrieved {families.Count} families total");

            return families;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] GetAllAsync failed: {ex.Message}");
            if (ex.Message.Contains("uuid"))
            {
                Debug.WriteLine("❌ [FAMILY_SERVICE] UUID parsing error detected. Check user authentication.");
            }
            throw;
        }
    }

    /// <summary>
    /// ✅ CORRIGIDO: Busca famílias com filtros e proteção UUID
    /// </summary>
    public async Task<List<Family>> GetFilteredAsync(string? searchText = null, bool? isActive = null)
    {
        try
        {
            Debug.WriteLine($"🔍 [FAMILY_SERVICE] Getting filtered families - Search: '{searchText}', Active: {isActive}");

            var currentUserIdString = _supabaseService.GetCurrentUserId();
            Debug.WriteLine($"🔍 [FAMILY_SERVICE] Current user ID: {currentUserIdString}");

            // ✅ CORREÇÃO: Validar UUID antes de usar
            Guid? currentUserId = null;

            if (!string.IsNullOrEmpty(currentUserIdString) && !string.IsNullOrWhiteSpace(currentUserIdString))
            {
                if (Guid.TryParse(currentUserIdString, out var parsedGuid))
                {
                    currentUserId = parsedGuid;
                }
                else
                {
                    Debug.WriteLine($"❌ [FAMILY_SERVICE] Invalid user ID in GetFilteredAsync: '{currentUserIdString}'");
                    return new List<Family>();
                }
            }
            else
            {
                Debug.WriteLine("❌ [FAMILY_SERVICE] No valid user ID for filtering");
                return new List<Family>();
            }

            // ✅ CORREÇÃO: Buscar todos e filtrar no cliente
            var response = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Select("*")
                .Get();

            if (response?.Models == null)
            {
                return new List<Family>();
            }

            // Filtrar no cliente
            var filteredSupabaseFamilies = response.Models.Where(sf =>
                sf.UserId == currentUserId.Value || sf.UserId == null
            ).ToList();

            var families = filteredSupabaseFamilies.Select(sf => sf.ToFamily()).ToList();

            // Aplicar filtros adicionais
            if (!string.IsNullOrEmpty(searchText))
            {
                var searchLower = searchText.ToLowerInvariant();
                families = families.Where(f =>
                    f.Name.ToLowerInvariant().Contains(searchLower) ||
                    (!string.IsNullOrEmpty(f.Description) && f.Description.ToLowerInvariant().Contains(searchLower))
                ).ToList();
            }

            if (isActive.HasValue)
            {
                families = families.Where(f => f.IsActive == isActive.Value).ToList();
            }

            families = families.OrderBy(f => f.Name).ToList();

            Debug.WriteLine($"✅ [FAMILY_SERVICE] Filtered query returned {families.Count} families");

            return families;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] GetFilteredAsync failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// ✅ CORRIGIDO: Cria nova família com validação UUID
    /// </summary>
    public async Task<Family> CreateAsync(Family family)
    {
        try
        {
            Debug.WriteLine($"➕ [FAMILY_SERVICE] Creating family: {family.Name}");

            var currentUserIdString = _supabaseService.GetCurrentUserId();

            // ✅ CORREÇÃO: Validar UUID antes de criar
            if (!string.IsNullOrEmpty(currentUserIdString) && !string.IsNullOrWhiteSpace(currentUserIdString))
            {
                if (Guid.TryParse(currentUserIdString, out var parsedGuid))
                {
                    family.UserId = parsedGuid;
                    Debug.WriteLine($"✅ [FAMILY_SERVICE] Assigned user ID: {family.UserId}");
                }
                else
                {
                    Debug.WriteLine($"❌ [FAMILY_SERVICE] Invalid user ID for creation: '{currentUserIdString}'");
                    throw new InvalidOperationException("Invalid user ID - cannot create family");
                }
            }
            else
            {
                // Para system defaults, usar null
                family.UserId = null;
                Debug.WriteLine("✅ [FAMILY_SERVICE] Creating system default family (no user ID)");
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
    /// ✅ CORRIGIDO: Atualiza família existente
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
    /// ✅ CORRIGIDO: Deleta família
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
    /// ✅ CORRIGIDO: Busca família por ID
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
    /// ✅ CORRIGIDO: Verifica se nome já existe com proteção UUID
    /// </summary>
    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
    {
        try
        {
            var currentUserIdString = _supabaseService.GetCurrentUserId();

            // ✅ CORREÇÃO: Validar UUID antes de usar
            Guid? currentUserId = null;
            if (!string.IsNullOrEmpty(currentUserIdString) && !string.IsNullOrWhiteSpace(currentUserIdString))
            {
                if (Guid.TryParse(currentUserIdString, out var parsedGuid))
                {
                    currentUserId = parsedGuid;
                }
                else
                {
                    Debug.WriteLine($"❌ [FAMILY_SERVICE] Invalid user ID in NameExistsAsync: '{currentUserIdString}'");
                    return false; // Se não consegue validar, assume que não existe
                }
            }

            // ✅ CORREÇÃO: Buscar todos e filtrar no cliente
            var response = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Select("id,name,user_id")
                .Get();

            if (response?.Models == null)
            {
                return false;
            }

            // Filtrar no cliente
            var relevantFamilies = response.Models.Where(sf =>
                (sf.UserId == currentUserId.Value || sf.UserId == null) &&
                string.Equals(sf.Name, name, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            // Se tem excludeId, filtrar
            if (excludeId.HasValue)
            {
                relevantFamilies = relevantFamilies.Where(f => f.Id != excludeId.Value).ToList();
            }

            return relevantFamilies.Any();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] NameExistsAsync failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// ✅ CORRIGIDO: Toggle favorite com proteção UUID
    /// </summary>
    public async Task<Family> ToggleFavoriteAsync(Guid familyId)
    {
        try
        {
            Debug.WriteLine($"⭐ [FAMILY_SERVICE] Toggling favorite for family: {familyId}");

            // Buscar família atual
            var response = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Select("*")
                .Where(f => f.Id == familyId)
                .Single();

            if (response == null)
            {
                throw new InvalidOperationException($"Family {familyId} not found");
            }

            // Toggle do status favorito
            var updatedFamily = new SupabaseFamily
            {
                Id = familyId,
                UserId = response.UserId,
                Name = response.Name,
                Description = response.Description,
                IsSystemDefault = response.IsSystemDefault ?? false,
                IsActive = response.IsActive ?? true,
                IsFavorite = !(response.IsFavorite ?? false), // ✅ TOGGLE
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
    /// ✅ NOVO: Teste de conectividade
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            Debug.WriteLine("🧪 [FAMILY_SERVICE] Testing connection...");

            if (_supabaseService.Client == null)
            {
                Debug.WriteLine("❌ [FAMILY_SERVICE] No client available");
                return false;
            }

            // Query simples para testar conectividade
            var response = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Select("id")
                .Limit(1)
                .Get();

            Debug.WriteLine("✅ [FAMILY_SERVICE] Connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] Connection test failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// ✅ NOVO: Busca estatísticas das famílias (para compatibilidade com FamilyRepository)
    /// </summary>
    public async Task<FamilyStatistics> GetStatisticsAsync()
    {
        try
        {
            Debug.WriteLine("📊 [FAMILY_SERVICE] Getting family statistics...");

            if (_supabaseService.Client == null)
            {
                Debug.WriteLine("❌ [FAMILY_SERVICE] No client available for statistics");
                return new FamilyStatistics();
            }

            var currentUserIdString = _supabaseService.GetCurrentUserId();
            Guid? currentUserId = null;

            if (!string.IsNullOrEmpty(currentUserIdString) && !string.IsNullOrWhiteSpace(currentUserIdString))
            {
                if (Guid.TryParse(currentUserIdString, out var parsedGuid))
                {
                    currentUserId = parsedGuid;
                }
            }

            // ✅ CORREÇÃO: Buscar todos e filtrar no cliente
            var response = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Select("*")
                .Get();

            if (response?.Models == null)
            {
                return new FamilyStatistics();
            }

            // Filtrar no cliente
            var filteredSupabaseFamilies = response.Models.Where(sf =>
                sf.UserId == currentUserId.Value || sf.UserId == null
            ).ToList();

            var families = filteredSupabaseFamilies.Select(sf => sf.ToFamily()).ToList();

            var statistics = new FamilyStatistics
            {
                TotalCount = families.Count,
                ActiveCount = families.Count(f => f.IsActive),
                InactiveCount = families.Count(f => !f.IsActive),
                SystemDefaultCount = families.Count(f => f.IsSystemDefault),
                UserCreatedCount = families.Count(f => !f.IsSystemDefault),
                LastRefreshTime = DateTime.UtcNow
            };

            Debug.WriteLine($"📊 [FAMILY_SERVICE] Statistics: {statistics.TotalCount} total, {statistics.ActiveCount} active");

            return statistics;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] GetStatisticsAsync failed: {ex.Message}");
            return new FamilyStatistics();
        }
    }
}