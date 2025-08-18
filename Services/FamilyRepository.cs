using OrchidPro.Models;
using OrchidPro.Models.Base;
using OrchidPro.Services.Base;
using OrchidPro.Services.Data;
using OrchidPro.Extensions;

namespace OrchidPro.Services;

/// <summary>
/// MINIMAL Family repository - reduced from ~400 lines to just the essentials!
/// BaseRepository handles ALL the heavy lifting - this just provides the service connection.
/// </summary>
public class FamilyRepository : BaseRepository<Family>, IFamilyRepository
{
    #region Private Fields

    private readonly SupabaseFamilyService _familyService;

    #endregion

    #region Required Base Implementation

    protected override string EntityTypeName => "Family";

    protected override async Task<IEnumerable<Family>> GetAllFromServiceAsync()
        => await _familyService.GetAllAsync();

    protected override async Task<Family?> GetByIdFromServiceAsync(Guid id)
        => await _familyService.GetByIdAsync(id);

    protected override async Task<Family?> CreateInServiceAsync(Family entity)
        => await _familyService.CreateAsync(entity);

    protected override async Task<Family?> UpdateInServiceAsync(Family entity)
        => await _familyService.UpdateAsync(entity);

    protected override async Task<bool> DeleteInServiceAsync(Guid id)
        => await _familyService.DeleteAsync(id);

    protected override async Task<bool> NameExistsInServiceAsync(string name, Guid? excludeId)
        => await _familyService.NameExistsAsync(name, excludeId);

    #endregion

    #region Constructor

    public FamilyRepository(SupabaseService supabaseService, SupabaseFamilyService familyService)
        : base(supabaseService)
    {
        _familyService = familyService ?? throw new ArgumentNullException(nameof(familyService));
        this.LogInfo("FamilyRepository initialized - BaseRepository handles everything!");
    }

    #endregion

    #region IFamilyRepository Specific Implementation

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

    public async Task<OperationResult> RefreshAllDataAsync()
    {
        try
        {
            await RefreshCacheAsync();
            return new OperationResult { Success = true, Message = "Family data refreshed successfully" };
        }
        catch (Exception ex)
        {
            return new OperationResult { Success = false, Message = ex.Message };
        }
    }

    #endregion
}

// Keep existing OperationResult if it already exists
public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}