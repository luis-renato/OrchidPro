using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Services.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Infrastructure.Supabase.Models;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;

namespace OrchidPro.Services.Infrastructure.Supabase.Repositories;

public class SupabaseSubstrateRepository(SupabaseService supabaseService) : BaseRepository<Substrate>(supabaseService), ISubstrateRepository
{
    private readonly BaseSupabaseEntityService<Substrate, SupabaseSubstrate> _supabaseEntityService = new InternalSupabaseSubstrateService(supabaseService);

    protected override string EntityTypeName => "Substrate";

    protected override async Task<IEnumerable<Substrate>> GetAllFromServiceAsync()
        => await _supabaseEntityService.GetAllAsync();

    protected override async Task<Substrate?> GetByIdFromServiceAsync(Guid id)
        => await _supabaseEntityService.GetByIdAsync(id);

    protected override async Task<Substrate?> CreateInServiceAsync(Substrate entity)
        => await _supabaseEntityService.CreateAsync(entity);

    protected override async Task<Substrate?> UpdateInServiceAsync(Substrate entity)
        => await _supabaseEntityService.UpdateAsync(entity);

    protected override async Task<bool> DeleteInServiceAsync(Guid id)
        => await _supabaseEntityService.DeleteAsync(id);

    protected override async Task<bool> NameExistsInServiceAsync(string name, Guid? excludeId)
        => await _supabaseEntityService.NameExistsAsync(name, excludeId);

    public async Task<SubstrateStatistics> GetSubstrateStatisticsAsync()
    {
        var baseStats = await GetStatisticsAsync();
        var allSubstrates = await GetAllAsync(includeInactive: true);

        return new SubstrateStatistics
        {
            TotalCount = baseStats.TotalCount,
            ActiveCount = baseStats.ActiveCount,
            InactiveCount = baseStats.InactiveCount,
            SystemDefaultCount = baseStats.SystemDefaultCount,
            UserCreatedCount = baseStats.UserCreatedCount,
            LastRefreshTime = baseStats.LastRefreshTime,
            UniqueSuppliersCount = allSubstrates.Where(s => !string.IsNullOrEmpty(s.Supplier))
                                               .Select(s => s.Supplier)
                                               .Distinct()
                                               .Count(),
            DrainageLevelDistribution = allSubstrates
                .Where(s => !string.IsNullOrEmpty(s.DrainageLevel))
                .GroupBy(s => s.DrainageLevel!)
                .ToDictionary(g => g.Key, g => g.Count()),
            SupplierDistribution = allSubstrates
                .Where(s => !string.IsNullOrEmpty(s.Supplier))
                .GroupBy(s => s.Supplier!)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public override async Task<Substrate> ToggleFavoriteAsync(Guid substrateId)
    {
        var substrate = await GetByIdAsync(substrateId) ?? throw new ArgumentException($"Substrate with ID {substrateId} not found");
        substrate.IsFavorite = !substrate.IsFavorite;
        substrate.UpdatedAt = DateTime.UtcNow;
        var updatedSubstrate = await UpdateAsync(substrate);
        return updatedSubstrate ?? throw new InvalidOperationException("Failed to update substrate favorite status");
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
            this.LogError(ex, "Error refreshing substrate data");
            var endTime = DateTime.UtcNow;
            return OperationResult.Failure(1, [ex.Message], startTime, endTime);
        }
    }
}

internal class InternalSupabaseSubstrateService(SupabaseService supabaseService)
    : BaseSupabaseEntityService<Substrate, SupabaseSubstrate>(supabaseService)
{
    protected override string EntityTypeName => "Substrate";
    protected override string EntityPluralName => "Substrates";

    protected override Substrate ConvertToEntity(SupabaseSubstrate supabaseModel)
        => supabaseModel.ToSubstrate();

    protected override SupabaseSubstrate ConvertFromEntity(Substrate entity)
        => SupabaseSubstrate.FromSubstrate(entity);
}