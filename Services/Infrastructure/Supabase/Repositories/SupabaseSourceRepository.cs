using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Services.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Infrastructure.Supabase.Models;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;

namespace OrchidPro.Services.Infrastructure.Supabase.Repositories;

public class SupabaseSourceRepository(SupabaseService supabaseService) : BaseRepository<Source>(supabaseService), ISourceRepository
{
    private readonly BaseSupabaseEntityService<Source, SupabaseSource> _supabaseEntityService = new InternalSupabaseSourceService(supabaseService);

    protected override string EntityTypeName => "Source";

    protected override async Task<IEnumerable<Source>> GetAllFromServiceAsync()
        => await _supabaseEntityService.GetAllAsync();

    protected override async Task<Source?> GetByIdFromServiceAsync(Guid id)
        => await _supabaseEntityService.GetByIdAsync(id);

    protected override async Task<Source?> CreateInServiceAsync(Source entity)
        => await _supabaseEntityService.CreateAsync(entity);

    protected override async Task<Source?> UpdateInServiceAsync(Source entity)
        => await _supabaseEntityService.UpdateAsync(entity);

    protected override async Task<bool> DeleteInServiceAsync(Guid id)
        => await _supabaseEntityService.DeleteAsync(id);

    protected override async Task<bool> NameExistsInServiceAsync(string name, Guid? excludeId)
        => await _supabaseEntityService.NameExistsAsync(name, excludeId);

    public async Task<SourceStatistics> GetSourceStatisticsAsync()
    {
        var baseStats = await GetStatisticsAsync();
        return new SourceStatistics
        {
            TotalCount = baseStats.TotalCount,
            ActiveCount = baseStats.ActiveCount,
            InactiveCount = baseStats.InactiveCount,
            SystemDefaultCount = baseStats.SystemDefaultCount,
            UserCreatedCount = baseStats.UserCreatedCount,
            LastRefreshTime = baseStats.LastRefreshTime
        };
    }

    public override async Task<Source> ToggleFavoriteAsync(Guid sourceId)
    {
        var source = await GetByIdAsync(sourceId) ?? throw new ArgumentException($"Source with ID {sourceId} not found");
        source.IsFavorite = !source.IsFavorite;
        source.UpdatedAt = DateTime.UtcNow;
        var updatedSource = await UpdateAsync(source);
        return updatedSource ?? throw new InvalidOperationException("Failed to update source favorite status");
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
            this.LogError(ex, "Error refreshing source data");
            var endTime = DateTime.UtcNow;
            return OperationResult.Failure(1, [ex.Message], startTime, endTime);
        }
    }
}

internal class InternalSupabaseSourceService(SupabaseService supabaseService)
    : BaseSupabaseEntityService<Source, SupabaseSource>(supabaseService)
{
    protected override string EntityTypeName => "Source";
    protected override string EntityPluralName => "Sources";

    protected override Source ConvertToEntity(SupabaseSource supabaseModel)
        => supabaseModel.ToSource();

    protected override SupabaseSource ConvertFromEntity(Source entity)
        => SupabaseSource.FromSource(entity);
}
