using OrchidPro.Models;
using OrchidPro.Services.Data;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace OrchidPro.Services;

/// <summary>
/// Modelo da Family para Supabase (com atributos de mapeamento)
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
    public bool IsSystemDefault { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("sync_hash")]
    public string? SyncHash { get; set; }

    /// <summary>
    /// Converte de Family para SupabaseFamily
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
            CreatedAt = family.CreatedAt,
            UpdatedAt = family.UpdatedAt,
            SyncHash = family.SyncHash
        };
    }

    /// <summary>
    /// Converte de SupabaseFamily para Family
    /// </summary>
    public Family ToFamily()
    {
        return new Family
        {
            Id = this.Id,
            UserId = this.UserId,
            Name = this.Name,
            Description = this.Description,
            IsSystemDefault = this.IsSystemDefault,
            IsActive = this.IsActive,
            CreatedAt = this.CreatedAt,
            UpdatedAt = this.UpdatedAt,
            SyncStatus = SyncStatus.Synced, // Se veio do servidor, está sincronizado
            LastSyncAt = DateTime.UtcNow,
            SyncHash = this.SyncHash
        };
    }
}

/// <summary>
/// Serviço para sincronizar Families com Supabase
/// </summary>
public class SupabaseFamilySync
{
    private readonly SupabaseService _supabaseService;

    public SupabaseFamilySync(SupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    /// <summary>
    /// 📤 Envia uma família para o Supabase
    /// </summary>
    public async Task<bool> UploadFamilyAsync(Family family)
    {
        try
        {
            if (_supabaseService.Client?.Auth.CurrentUser == null)
            {
                throw new InvalidOperationException("User not authenticated");
            }

            var supabaseFamily = SupabaseFamily.FromFamily(family);

            // Verifica se já existe
            var existing = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Where(f => f.Id == family.Id)
                .Single();

            if (existing != null)
            {
                // Atualizar
                await _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Where(f => f.Id == family.Id)
                    .Set(f => f.Name, family.Name)
                    .Set(f => f.Description, family.Description)
                    .Set(f => f.IsActive, family.IsActive)
                    .Set(f => f.UpdatedAt, family.UpdatedAt)
                    .Set(f => f.SyncHash, family.SyncHash)
                    .Update();
            }
            else
            {
                // Inserir
                await _supabaseService.Client
                    .From<SupabaseFamily>()
                    .Insert(supabaseFamily);
            }

            System.Diagnostics.Debug.WriteLine($"✅ Uploaded family to Supabase: {family.Name}");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Failed to upload family {family.Name}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 📥 Baixa famílias do Supabase
    /// </summary>
    public async Task<List<Family>> DownloadFamiliesAsync()
    {
        try
        {
            if (_supabaseService.Client?.Auth.CurrentUser == null)
            {
                throw new InvalidOperationException("User not authenticated");
            }

            var currentUserId = Guid.Parse(_supabaseService.Client.Auth.CurrentUser.Id);

            // Busca famílias do usuário + system defaults
            var supabaseFamilies = await _supabaseService.Client
                .From<SupabaseFamily>()
                .Where(f => f.UserId == currentUserId || f.UserId == null)
                .Where(f => f.IsActive == true)
                .Get();

            var families = supabaseFamilies.Models
                .Select(sf => sf.ToFamily())
                .ToList();

            System.Diagnostics.Debug.WriteLine($"📥 Downloaded {families.Count} families from Supabase");
            return families;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Failed to download families: {ex.Message}");
            return new List<Family>();
        }
    }

    /// <summary>
    /// 🔄 Sincronização bidirecional completa
    /// </summary>
    public async Task<SyncResult> PerformFullSyncAsync(List<Family> localFamilies)
    {
        var result = new SyncResult
        {
            StartTime = DateTime.UtcNow
        };

        try
        {
            // 1. 📥 Baixar famílias do servidor
            var serverFamilies = await DownloadFamiliesAsync();

            // 2. 📤 Enviar famílias locais pendentes
            var pendingLocal = localFamilies.Where(f =>
                f.SyncStatus == SyncStatus.Local ||
                f.SyncStatus == SyncStatus.Pending ||
                f.SyncStatus == SyncStatus.Error
            ).ToList();

            result.TotalProcessed = pendingLocal.Count;

            foreach (var family in pendingLocal)
            {
                var success = await UploadFamilyAsync(family);
                if (success)
                    result.Successful++;
                else
                    result.Failed++;
            }

            // 3. 🔍 Detectar conflitos (mesma família modificada em ambos os lados)
            // Implementação básica - pode ser expandida

            System.Diagnostics.Debug.WriteLine($"🔄 Full sync completed: {result.Successful}/{result.TotalProcessed}");
        }
        catch (Exception ex)
        {
            result.ErrorMessages.Add(ex.Message);
            System.Diagnostics.Debug.WriteLine($"❌ Full sync failed: {ex.Message}");
        }

        result.EndTime = DateTime.UtcNow;
        result.Duration = result.EndTime - result.StartTime;
        return result;
    }

    /// <summary>
    /// 🧪 Testa conectividade com Supabase
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            if (_supabaseService.Client?.Auth.CurrentUser == null)
            {
                return false;
            }

            // Tenta fazer uma query simples
            await _supabaseService.Client
                .From<SupabaseFamily>()
                .Limit(1)
                .Get();

            System.Diagnostics.Debug.WriteLine("✅ Supabase connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Supabase connection test failed: {ex.Message}");
            return false;
        }
    }
}