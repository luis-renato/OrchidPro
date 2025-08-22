using OrchidPro.Extensions;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;

namespace OrchidPro.ViewModels.Variants;

/// <summary>
/// Variant list ViewModel - ULTRA CLEAN using BaseListViewModel core.
/// Follows exact pattern of FamiliesListViewModel since Variant is an independent entity.
/// Inherits all functionality automatically through Template Method Pattern.
/// </summary>
public partial class VariantsListViewModel : BaseListViewModel<Models.Variant, VariantItemViewModel>
{
    #region Private Fields

    private readonly IVariantRepository _variantRepository;

    #endregion

    #region Required Base Class Overrides

    public override string EntityName => "Variant";
    public override string EntityNamePlural => "Variants";
    public override string EditRoute => "variantedit";

    #endregion

    #region Constructor

    public VariantsListViewModel(IVariantRepository repository, INavigationService navigationService)
        : base(repository, navigationService)
    {
        _variantRepository = repository;
        this.LogInfo("🚀 CLEAN VariantsListViewModel initialized using BaseListViewModel core");
    }

    #endregion

    #region ONLY REQUIRED: CreateItemViewModel

    /// <summary>
    /// Only required override - creates VariantItemViewModel instances
    /// </summary>
    protected override VariantItemViewModel CreateItemViewModel(Models.Variant entity)
    {
        return new VariantItemViewModel(entity);
    }

    #endregion

    // ALL OTHER FUNCTIONALITY INHERITED AUTOMATICALLY:
    // ✅ Filtering, Sorting, Multi-selection
    // ✅ Pull-to-refresh, Pagination  
    // ✅ Search, Visual states
    // ✅ CRUD operations, Validation
    // ✅ Add, Edit, Delete commands
    // ✅ Bulk operations
    // ✅ Loading states, Error handling
    // ✅ Connection status monitoring
    // ✅ Performance optimizations
}