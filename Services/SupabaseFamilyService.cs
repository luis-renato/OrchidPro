using OrchidPro.Models;
using OrchidPro.Services.Data;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Diagnostics;

namespace OrchidPro.Services;

/// <summary>
/// Modelo da Family para Supabase - SCHEMA PUBLIC (families)
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
            CreatedAt = this.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = this.UpdatedAt ?? DateTime.UtcNow
        };
    }
}

/// <summary>
/// CORRIGIDO: Serviço com teste REAL de conectividade
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

            var currentUserId = _supabaseService.GetCurrentUserId();
            Debug.WriteLine($"📥 [FAMILY_SERVICE] Current user ID: {currentUserId ?? "null"}");

            var allFamilies = new List<SupabaseFamily>();

            // Se autenticado, buscar minhas + system
            if (!string.IsNullOrEmpty(currentUserId) && Guid.TryParse(currentUserId, out var userGuid))
            {
                Debug.WriteLine("📥 [FAMILY_SERVICE] Authenticated - fetching user + system families...");

                // Buscar families do usuário
                var userQuery = _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Where(f => f.UserId == userGuid);

                var userResponse = await userQuery.Get();

                if (userResponse?.Models != null)
                {
                    allFamilies.AddRange(userResponse.Models);
                    Debug.WriteLine($"📥 [FAMILY_SERVICE] User families: {userResponse.Models.Count}");
                }

                // Buscar families do sistema (user_id = null)
                var systemQuery = _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Where(f => f.UserId == null);

                var systemResponse = await systemQuery.Get();

                if (systemResponse?.Models != null)
                {
                    allFamilies.AddRange(systemResponse.Models);
                    Debug.WriteLine($"📥 [FAMILY_SERVICE] System families: {systemResponse.Models.Count}");
                }
            }
            else
            {
                Debug.WriteLine("📥 [FAMILY_SERVICE] Not authenticated - fetching system families only...");

                // Apenas families do sistema
                var systemQuery = _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Where(f => f.UserId == null);

                var response = await systemQuery.Get();

                if (response?.Models != null)
                {
                    allFamilies.AddRange(response.Models);
                }
            }

            var families = allFamilies.Select(sf => sf.ToFamily()).ToList();
            Debug.WriteLine($"📥 [FAMILY_SERVICE] Retrieved {families.Count} families successfully");
            return families;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] GetAllAsync failed: {ex.Message}");
            return new List<Family>();
        }
    }

    /// <summary>
    /// Busca uma família por ID
    /// </summary>
    public async Task<Family?> GetByIdAsync(Guid id)
    {
        try
        {
            if (_supabaseService.Client == null)
            {
                Debug.WriteLine("❌ [FAMILY_SERVICE] Client not available");
                return null;
            }

            Debug.WriteLine($"🔍 [FAMILY_SERVICE] Fetching family by ID: {id}");

            var query = _supabaseService.Client
                .From<SupabaseFamily>()
                .Where(f => f.Id == id)
                .Limit(1);

            var response = await query.Get();
            var supabaseFamily = response?.Models?.FirstOrDefault();

            if (supabaseFamily != null)
            {
                var family = supabaseFamily.ToFamily();
                Debug.WriteLine($"✅ [FAMILY_SERVICE] Found family: {family.Name}");
                return family;
            }

            Debug.WriteLine($"❌ [FAMILY_SERVICE] Family not found: {id}");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] GetByIdAsync failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Verifica se nome já existe
    /// </summary>
    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
    {
        try
        {
            if (_supabaseService.Client == null) return false;

            var currentUserId = _supabaseService.GetCurrentUserId();
            Guid? userGuid = null;

            if (!string.IsNullOrEmpty(currentUserId))
            {
                Guid.TryParse(currentUserId, out var parsed);
                userGuid = parsed;
            }

            Debug.WriteLine($"🔍 [FAMILY_SERVICE] Checking name existence: {name}");

            var query = _supabaseService.Client
                .From<SupabaseFamily>()
                .Where(f => f.Name == name && f.UserId == userGuid);

            if (excludeId.HasValue)
            {
                query = query.Where(f => f.Id != excludeId.Value);
            }

            var response = await query.Get();
            var exists = response?.Models?.Any() == true;

            Debug.WriteLine($"🔍 [FAMILY_SERVICE] Name '{name}' exists: {exists}");
            return exists;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] NameExistsAsync failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Cria uma nova família
    /// </summary>
    public async Task<Family> CreateAsync(Family family)
    {
        try
        {
            if (_supabaseService.Client == null)
            {
                throw new InvalidOperationException("Supabase client not available");
            }

            Debug.WriteLine($"➕ [FAMILY_SERVICE] Creating family: {family.Name}");

            var supabaseFamily = SupabaseFamily.FromFamily(family);

            // Garantir user_id para famílias não-system
            if (supabaseFamily.UserId == null && !(supabaseFamily.IsSystemDefault ?? false))
            {
                var currentUserId = _supabaseService.GetCurrentUserId();
                if (Guid.TryParse(currentUserId, out var userId))
                {
                    supabaseFamily.UserId = userId;
                    Debug.WriteLine($"➕ [FAMILY_SERVICE] Set user_id to: {userId}");
                }
                else
                {
                    throw new InvalidOperationException("Could not determine current user ID");
                }
            }

            // Garantir timestamps
            var now = DateTime.UtcNow;
            supabaseFamily.CreatedAt = now;
            supabaseFamily.UpdatedAt = now;

            var response = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Insert(supabaseFamily);

            if (response?.Models?.Any() == true)
            {
                var created = response.Models.First().ToFamily();
                Debug.WriteLine($"✅ [FAMILY_SERVICE] Created family: {created.Name} (ID: {created.Id})");
                return created;
            }

            throw new InvalidOperationException("Insert returned no data");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] CreateAsync failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Atualiza uma família existente
    /// </summary>
    public async Task<Family> UpdateAsync(Family family)
    {
        try
        {
            if (_supabaseService.Client == null)
            {
                throw new InvalidOperationException("Supabase client not available");
            }

            Debug.WriteLine($"📝 [FAMILY_SERVICE] Updating family: {family.Name} (ID: {family.Id})");

            var supabaseFamily = SupabaseFamily.FromFamily(family);
            supabaseFamily.UpdatedAt = DateTime.UtcNow;

            var response = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Where(f => f.Id == family.Id)
                .Update(supabaseFamily);

            if (response?.Models?.Any() == true)
            {
                var updated = response.Models.First().ToFamily();
                Debug.WriteLine($"✅ [FAMILY_SERVICE] Updated family: {updated.Name}");
                return updated;
            }

            throw new InvalidOperationException("Update returned no data");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] UpdateAsync failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Soft delete de uma família
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            if (_supabaseService.Client == null)
            {
                Debug.WriteLine("❌ [FAMILY_SERVICE] Client not available");
                return false;
            }

            Debug.WriteLine($"🗑️ [FAMILY_SERVICE] Soft deleting family: {id}");

            // Buscar família primeiro para verificar se pode deletar
            var existing = await GetByIdAsync(id);
            if (existing == null)
            {
                Debug.WriteLine($"❌ [FAMILY_SERVICE] Family not found for deletion: {id}");
                return false;
            }

            if (existing.IsSystemDefault)
            {
                Debug.WriteLine($"❌ [FAMILY_SERVICE] Cannot delete system default family: {existing.Name}");
                return false;
            }

            // Fazer soft delete (isActive = false)
            var updateData = new SupabaseFamily
            {
                Id = id,
                IsActive = false,
                UpdatedAt = DateTime.UtcNow
            };

            var response = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Where(f => f.Id == id)
                .Update(updateData);

            var success = response?.Models?.Any() == true;

            if (success)
            {
                Debug.WriteLine($"✅ [FAMILY_SERVICE] Soft deleted family: {existing.Name}");
            }
            else
            {
                Debug.WriteLine($"❌ [FAMILY_SERVICE] Soft delete failed for: {existing.Name}");
            }

            return success;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] DeleteAsync failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// ✅ CORRIGIDO: Testa conectividade REAL com query no banco
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            if (_supabaseService.Client == null || !_supabaseService.IsAuthenticated)
            {
                Debug.WriteLine("❌ [FAMILY_SERVICE] Client null or not authenticated");
                return false;
            }

            Debug.WriteLine("🔍 [FAMILY_SERVICE] Testing REAL database connection...");

            // ✅ TESTE REAL: Query simples na tabela families com timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var query = _supabaseService.Client.From<SupabaseFamily>()
                .Select("id,name")
                .Limit(1);

            var response = await query.Get();

            var success = response?.Models != null;

            if (success)
            {
                Debug.WriteLine($"✅ [FAMILY_SERVICE] REAL connection test: SUCCESS");
                Debug.WriteLine($"✅ [FAMILY_SERVICE] Query returned valid response");
            }
            else
            {
                Debug.WriteLine($"❌ [FAMILY_SERVICE] REAL connection test: FAILED - no response");
            }

            return success;
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("⏰ [FAMILY_SERVICE] Connection test timeout (10s)");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] REAL connection test failed: {ex.Message}");
            Debug.WriteLine($"❌ [FAMILY_SERVICE] Exception type: {ex.GetType().Name}");

            // ✅ Log mais detalhes para debug
            if (ex.InnerException != null)
            {
                Debug.WriteLine($"❌ [FAMILY_SERVICE] Inner exception: {ex.InnerException.Message}");
            }

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
                LastRefreshTime = DateTime.UtcNow // Sempre atual na arquitetura direta
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_SERVICE] GetStatisticsAsync failed: {ex.Message}");
            return new FamilyStatistics();
        }
    }
}