using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Services.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Infrastructure.Supabase.Models;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;

namespace OrchidPro.Services.Infrastructure.Supabase.Repositories;

public class SupabaseFamilyRepository(SupabaseService supabaseService) : BaseRepository<Family>(supabaseService), IFamilyRepository
{
    private readonly BaseSupabaseEntityService<Family, SupabaseFamily> _supabaseEntityService = new InternalSupabaseFamilyService(supabaseService);

    protected override string EntityTypeName => "Family";

    protected override async Task<IEnumerable<Family>> GetAllFromServiceAsync()
        => await _supabaseEntityService.GetAllAsync();

    protected override async Task<Family?> GetByIdFromServiceAsync(Guid id)
        => await _supabaseEntityService.GetByIdAsync(id);

    protected override async Task<Family?> CreateInServiceAsync(Family entity)
        => await _supabaseEntityService.CreateAsync(entity);

    protected override async Task<Family?> UpdateInServiceAsync(Family entity)
        => await _supabaseEntityService.UpdateAsync(entity);

    protected override async Task<bool> DeleteInServiceAsync(Guid id)
        => await _supabaseEntityService.DeleteAsync(id);

    protected override async Task<bool> NameExistsInServiceAsync(string name, Guid? excludeId)
        => await _supabaseEntityService.NameExistsAsync(name, excludeId);

    public async Task<FamilyStatistics> GetFamilyStatisticsAsync()
    {
        var baseStats = await GetStatisticsAsync();
        return new FamilyStatistics
        {
            TotalCount = baseStats.TotalCount,
            ActiveCount = baseStats.ActiveCount,
            InactiveCount = baseStats.InactiveCount,
            SystemDefaultCount = baseStats.SystemDefaultCount,
            UserCreatedCount = baseStats.UserCreatedCount,
            LastRefreshTime = baseStats.LastRefreshTime
        };
    }

    public override async Task<Family> ToggleFavoriteAsync(Guid familyId)
    {
        var family = await GetByIdAsync(familyId) ?? throw new ArgumentException($"Family with ID {familyId} not found");
        family.IsFavorite = !family.IsFavorite;
        family.UpdatedAt = DateTime.UtcNow;

        var updatedFamily = await UpdateAsync(family);
        return updatedFamily ?? throw new InvalidOperationException("Failed to update family favorite status");
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
            this.LogError(ex, "Error refreshing family data");
            var endTime = DateTime.UtcNow;
            return OperationResult.Failure(1, [ex.Message], startTime, endTime);
        }
    }
}

internal class InternalSupabaseFamilyService(SupabaseService supabaseService)
    : BaseSupabaseEntityService<Family, SupabaseFamily>(supabaseService)
{
    protected override string EntityTypeName => "Family";
    protected override string EntityPluralName => "Families";

    protected override Family ConvertToEntity(SupabaseFamily supabaseModel)
        => supabaseModel.ToFamily();

    protected override SupabaseFamily ConvertFromEntity(Family entity)
        => SupabaseFamily.FromFamily(entity);
}