using OrchidPro.Models;
using OrchidPro.ViewModels.Base;

namespace OrchidPro.ViewModels.Substrates;

public partial class SubstrateItemViewModel(Substrate substrate) : BaseItemViewModel<Substrate>(substrate)
{
    #region Required Base Class Override

    public override string EntityName => "Substrate";

    #endregion

    #region Substrate-Specific Properties (READ-ONLY)

    public bool IsFavorite => Entity?.IsFavorite ?? false;
    public string? Components => Entity?.Components;
    public string? PhRange => Entity?.PhRange;
    public string? DrainageLevel => Entity?.DrainageLevel;
    public string? Supplier => Entity?.Supplier;

    #endregion

    #region Display Properties

    public string ComponentsPreview => string.IsNullOrWhiteSpace(Components) ? "Components not specified" :
        Components.Length > 50 ? Components.Substring(0, 47) + "..." : Components;
    public string PhRangeDisplay => string.IsNullOrWhiteSpace(PhRange) ? "pH not specified" : $"pH {PhRange}";
    public string DrainageLevelDisplay => string.IsNullOrWhiteSpace(DrainageLevel) ? "Drainage not specified" : DrainageLevel;
    public string SupplierDisplay => string.IsNullOrWhiteSpace(Supplier) ? "No supplier" : Supplier;
    public bool HasDetails => !string.IsNullOrWhiteSpace(Components) || !string.IsNullOrWhiteSpace(PhRange);

    #endregion

    #region Computed Properties

    private Substrate Entity => ToModel();

    #endregion
}
