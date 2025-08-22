using OrchidPro.Models;
using OrchidPro.Services.Base;
using OrchidPro.Extensions;
using OrchidPro.Services.Data;

namespace OrchidPro.Services;

/// <summary>
/// MINIMAL Variant repository - follows exact pattern of FamilyRepository.
/// BaseRepository handles ALL the heavy lifting - this just provides the service connection.
/// Reduces code from ~400 lines to just the essentials!
/// </summary>
public class VariantRepository : BaseRepository<Variant>, IVariantRepository
{
    #region Private Fields

    private readonly SupabaseVariantService _variantService;

    #endregion

    #region Required Base Implementation

    protected override string EntityTypeName => "Variant";

    protected override async Task<IEnumerable<Variant>> GetAllFromServiceAsync()
        => await _variantService.GetAllAsync();

    protected override async Task<Variant?> GetByIdFromServiceAsync(Guid id)
        => await _variantService.GetByIdAsync(id);

    protected override async Task<Variant?> CreateInServiceAsync(Variant entity)
        => await _variantService.CreateAsync(entity);

    protected override async Task<Variant?> UpdateInServiceAsync(Variant entity)
        => await _variantService.UpdateAsync(entity);

    protected override async Task<bool> DeleteInServiceAsync(Guid id)
        => await _variantService.DeleteAsync(id);

    protected override async Task<bool> NameExistsInServiceAsync(string name, Guid? excludeId)
        => await _variantService.NameExistsAsync(name, excludeId);

    #endregion

    #region Constructor

    public VariantRepository(SupabaseService supabaseService, SupabaseVariantService variantService)
        : base(supabaseService)
    {
        _variantService = variantService ?? throw new ArgumentNullException(nameof(variantService));
        this.LogInfo("VariantRepository initialized - BaseRepository handles everything!");
    }

    #endregion
}