using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Services.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Infrastructure.Supabase.Models;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;

namespace OrchidPro.Services.Infrastructure.Supabase.Repositories;

public class SupabaseMountRepository(SupabaseService supabaseService) : BaseRepository<Mount>(supabaseService), IMountRepository
{
    private readonly BaseSupabaseEntityService<Mount, SupabaseMount> _supabaseEntityService = new InternalSupabaseMountService(supabaseService);

    protected override string EntityTypeName => "Mount";

    protected override async Task<IEnumerable<Mount>> GetAllFromServiceAsync()
        => await _supabaseEntityService.GetAllAsync();

    protected override async Task<Mount?> GetByIdFromServiceAsync(Guid id)
        => await _supabaseEntityService.GetByIdAsync(id);

    protected override async Task<Mount?> CreateInServiceAsync(Mount entity)
        => await _supabaseEntityService.CreateAsync(entity);

    protected override async Task<Mount?> UpdateInServiceAsync(Mount entity)
        => await _supabaseEntityService.UpdateAsync(entity);

    protected override async Task<bool> DeleteInServiceAsync(Guid id)
        => await _supabaseEntityService.DeleteAsync(id);

    protected override async Task<bool> NameExistsInServiceAsync(string name, Guid? excludeId)
        => await _supabaseEntityService.NameExistsAsync(name, excludeId);

    public async Task<MountStatistics> GetMountStatisticsAsync()
    {
        var baseStats = await GetStatisticsAsync();
        var allMounts = await GetAllAsync(includeInactive: true);

        return new MountStatistics
        {
            TotalCount = baseStats.TotalCount,
            ActiveCount = baseStats.ActiveCount,
            InactiveCount = baseStats.InactiveCount,
            SystemDefaultCount = baseStats.SystemDefaultCount,
            UserCreatedCount = baseStats.UserCreatedCount,
            LastRefreshTime = baseStats.LastRefreshTime,
            UniqueMaterialsCount = allMounts.Where(c => !string.IsNullOrEmpty(c.Material))
                                              .Select(c => c.Material)
                                              .Distinct()
                                              .Count(),
            MaterialDistribution = allMounts
                .Where(c => !string.IsNullOrEmpty(c.Material))
                .GroupBy(c => c.Material!)
                .ToDictionary(g => g.Key, g => g.Count()),
            SizeDistribution = allMounts
                .Where(c => !string.IsNullOrEmpty(c.Size))
                .GroupBy(c => c.Size!)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public override async Task<Mount> ToggleFavoriteAsync(Guid MountId)
    {
        var Mount = await GetByIdAsync(MountId) ?? throw new ArgumentException($"Mount with ID {MountId} not found");
        Mount.IsFavorite = !Mount.IsFavorite;
        Mount.UpdatedAt = DateTime.UtcNow;
        var updatedMount = await UpdateAsync(Mount);
        return updatedMount ?? throw new InvalidOperationException("Failed to update Mount favorite status");
    }

    public async Task<OperationResult> RefreshAllDataAsync()
    {
        var startTime = DateTime.UtcNow;
        try
        {
            await RefreshCacheAsync();
            var endTime = DateTime.UtcNow;
            return OperationResult.Success(1, startTime, endTime);
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Error refreshing Mount data");
            var endTime = DateTime.UtcNow;
            return OperationResult.Failure(1, [ex.Message], startTime, endTime);
        }
    }
}

internal class InternalSupabaseMountService(SupabaseService supabaseService)
    : BaseSupabaseEntityService<Mount, SupabaseMount>(supabaseService)
{
    protected override string EntityTypeName => "Mount";
    protected override string EntityPluralName => "Mounts";

    protected override Mount ConvertToEntity(SupabaseMount supabaseModel)
        => supabaseModel.ToMount();

    protected override SupabaseMount ConvertFromEntity(Mount entity)
        => SupabaseMount.FromMount(entity);
}