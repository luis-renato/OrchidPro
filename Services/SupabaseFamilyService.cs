using System.Diagnostics;
using OrchidPro.Models;
using OrchidPro.Services.Data;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace OrchidPro.Services;

/// <summary>
/// ✅ ATUALIZADO: Model Supabase sem is_system_default
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

    /// <summary>
    /// ✅ REMOVIDO: is_system_default não existe mais no schema
    /// </summary>

    [Column("is_active")]
    public bool? IsActive { get; set; } = true;

    [Column("is_favorite")]
    public bool? IsFavorite { get; set; } = false;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ✅ ATUALIZADO: Converte SupabaseFamily para Family
    /// </summary>
    public Family ToFamily()
    {
        return new Family
        {
            Id = this.Id,
            UserId = this.UserId,
            Name = this.Name ?? string.Empty,
            Description = this.Description,
            // ✅ REMOVIDO: IsSystemDefault não é mais setado (é computed property baseado em UserId)
            IsActive = this.IsActive ?? true,
            IsFavorite = this.IsFavorite ?? false,
            CreatedAt = this.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = this.UpdatedAt ?? DateTime.UtcNow
        };
    }

    /// <summary>
    /// ✅ ATUALIZADO: Converte Family para SupabaseFamily
    /// </summary>
    public static SupabaseFamily FromFamily(Family family)
    {
        return new SupabaseFamily
        {
            Id = family.Id,
            UserId = family.UserId,
            Name = family.Name,
            Description = family.Description,
            // ✅ REMOVIDO: IsSystemDefault não é mais enviado para o banco
            IsActive = family.IsActive,
            IsFavorite = family.IsFavorite,
            CreatedAt = family.CreatedAt,
            UpdatedAt = family.UpdatedAt
        };
    }
}

/// <summary>
/// ✅ ATUALIZADO: Serviço Supabase sem referências a IsSystemDefault
/// </summary>
public class SupabaseFamilyService
{
    private readonly SupabaseService _supabaseService;

    public SupabaseFamilyService(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    /// <summary>
    /// ✅ ATUALIZADO: Busca todas as famílias (sistema identificado por UserId null)
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

            Debug.WriteLine("📥 [FAMILY_SERVICE] Starting GetAllAsync...");

            var currentUserIdString = _supabaseService.GetCurrentUserId();
            Debug.WriteLine($"📥 [FAMILY_SERVICE] Current user ID: '{currentUserIdString}'");

            // ✅ CORREÇÃO: Validar e converter userId corretamente
            Guid? currentUserId = null;
            if (Guid.TryParse(currentUserIdString, out Guid parsedUserId))
            {
                currentUserId = parsedUserId;
                Debug.WriteLine($"✅ [FAMILY_SERVICE] Parsed user ID: {currentUserId}");
            }
            else
            {
                Debug.WriteLine($"⚠️ [FAMILY_SERVICE] Could not parse user ID, will only get system families");
            }

            if (currentUserId.HasValue)
            {
                // ✅ CORREÇÃO: Query simples - buscar todos e filtrar no cliente
                var response = await _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Select("*")
                    .Get();

                Debug.WriteLine($"🔍 [FAMILY_SERVICE] Querying all families");

                if (response?.Models == null || !response.Models.Any())
                {
                    Debug.WriteLine("📝 [FAMILY_SERVICE] No families found in database - returning mock data");
                    return GetMockFamilies();
                }

                // ✅ Filtrar no cliente: famílias do usuário OU system defaults (UserId == null)
                var filteredFamilies = response.Models.Where(sf =>
                    sf.UserId == currentUserId || sf.UserId == null
                ).ToList();

                Debug.WriteLine($"✅ [FAMILY_SERVICE] Found {response.Models.Count()} total families in database");
                Debug.WriteLine($"✅ [FAMILY_SERVICE] Filtered to {filteredFamilies.Count} families for user");

                var families = filteredFamilies
                    .Select(sf => sf.ToFamily())
                    .OrderBy(f => f.Name)
                    .ToList();

                Debug.WriteLine($"✅ [FAMILY_SERVICE] Final result: {families.Count} families");

                // ✅ DIAGNÓSTICO: Log detalhado das famílias
                foreach (var family in families.Take(3))
                {
                    Debug.WriteLine($"  - Family: {family.Name} (ID: {family.Id}, Active: {family.IsActive}, Favorite: {family.IsFavorite}, System: {family.IsSystemDefault})");
                }

                return families;
            }
            else
            {
                // Só famílias do sistema se não há usuário autenticado
                var response = await _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Select("*")
                    .Where(f => f.UserId == null)
                    .Get();

                Debug.WriteLine($"🔍 [FAMILY_SERVICE] Querying only system families (no authenticated user)");

                if (response?.Models == null || !response.Models.Any())
                {
                    Debug.WriteLine("📝 [FAMILY_SERVICE] No families found in database - returning mock data");
                    return GetMockFamilies();
                }

                var families = response.Models
                    .Select(sf => sf.ToFamily())
                    .OrderBy(f => f.Name)
                    .ToList();

                Debug.WriteLine($"✅ [FAMILY_SERVICE] Final result: {families.Count} families");

                // ✅ DIAGNÓSTICO: Log detalhado das famílias  
                foreach (var family in families.Take(3))
                {
                    Debug.WriteLine($"  - Family: {family.Name} (ID: {family.Id}, Active: {family.IsActive}, Favorite: {family.IsFavorite}, System: {family.IsSystemDefault})");
                }

                return families;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] GetAllAsync failed: {ex.Message}");
            Debug.WriteLine($"❌ [FAMILY_SERVICE] Stack trace: {ex.StackTrace}");

            Debug.WriteLine("🆘 [FAMILY_SERVICE] Returning mock data due to error");
            return GetMockFamilies();
        }
    }

    /// <summary>
    /// ✅ ATUALIZADO: Mock families sem IsSystemDefault explícito
    /// </summary>
    private List<Family> GetMockFamilies()
    {
        try
        {
            Debug.WriteLine("🎭 [FAMILY_SERVICE] Creating mock families for offline/fallback");

            return new List<Family>
            {
                new Family
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    UserId = null, // ✅ Sistema identificado por UserId null
                    Name = "Orchidaceae",
                    Description = "The orchid family - largest family of flowering plants",
                    IsActive = true,
                    IsFavorite = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new Family
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    UserId = null, // ✅ Sistema identificado por UserId null
                    Name = "Rosaceae",
                    Description = "The rose family including roses, apples, and many fruit trees",
                    IsActive = true,
                    IsFavorite = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-25),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new Family
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    UserId = null, // ✅ Sistema identificado por UserId null
                    Name = "Asteraceae",
                    Description = "The sunflower family - one of the largest plant families",
                    IsActive = true,
                    IsFavorite = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-20),
                    UpdatedAt = DateTime.UtcNow.AddDays(-3)
                }
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] GetMockFamilies failed: {ex.Message}");
            return new List<Family>();
        }
    }

    /// <summary>
    /// ✅ Cria nova família
    /// </summary>
    public async Task<Family> CreateAsync(Family family)
    {
        try
        {
            Debug.WriteLine($"➕ [FAMILY_SERVICE] Creating family: {family.Name}");

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
    /// ✅ Atualiza família existente
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
    /// ✅ Deleta família
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
    /// ✅ Busca família por ID
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

            var mockFamilies = GetMockFamilies();
            return mockFamilies.FirstOrDefault(f => f.Id == id);
        }
    }

    /// <summary>
    /// ✅ Verifica se nome já existe
    /// </summary>
    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
    {
        try
        {
            Debug.WriteLine($"🔍 [FAMILY_SERVICE] Checking if name exists: '{name}', exclude: {excludeId}");

            var allFamilies = await GetAllAsync();

            var exists = allFamilies.Any(f =>
                string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase) &&
                f.Id != excludeId);

            Debug.WriteLine($"✅ [FAMILY_SERVICE] Name '{name}' exists: {exists}");
            return exists;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] NameExistsAsync failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// ✅ Toggle favorite
    /// </summary>
    public async Task<Family> ToggleFavoriteAsync(Guid familyId)
    {
        try
        {
            Debug.WriteLine($"⭐ [FAMILY_SERVICE] Toggling favorite for family: {familyId}");

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
                // ✅ REMOVIDO: IsSystemDefault não é mais enviado
                IsActive = response.IsActive ?? true,
                IsFavorite = !(response.IsFavorite ?? false), // ✅ TOGGLE
                UpdatedAt = DateTime.UtcNow
            };

            await _supabaseService.Client
                .From<SupabaseFamily>()
                .Where(f => f.Id == familyId)
                .Update(updatedFamily);

            Debug.WriteLine($"✅ [FAMILY_SERVICE] Family {familyId} favorite status: {updatedFamily.IsFavorite}");

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
    /// ✅ Teste de conectividade
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
    /// ✅ ATUALIZADO: Estatísticas sem IsSystemDefault
    /// </summary>
    public async Task<FamilyStatistics> GetStatisticsAsync()
    {
        try
        {
            Debug.WriteLine("📊 [FAMILY_SERVICE] Getting family statistics...");

            var families = await GetAllAsync();

            var statistics = new FamilyStatistics
            {
                TotalCount = families.Count,
                ActiveCount = families.Count(f => f.IsActive),
                InactiveCount = families.Count(f => !f.IsActive),
                SystemDefaultCount = families.Count(f => f.IsSystemDefault), // ✅ Computed property baseado em UserId
                UserCreatedCount = families.Count(f => !f.IsSystemDefault),
                LastRefreshTime = DateTime.UtcNow
            };

            Debug.WriteLine($"📊 [FAMILY_SERVICE] Statistics: {statistics.TotalCount} total, {statistics.ActiveCount} active, {statistics.SystemDefaultCount} system");

            return statistics;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] GetStatisticsAsync failed: {ex.Message}");
            return new FamilyStatistics();
        }
    }
}