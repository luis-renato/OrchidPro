using OrchidPro.Models;
using OrchidPro.ViewModels.Base;

namespace OrchidPro.ViewModels.Mounts;

public partial class MountItemViewModel(Mount Mount) : BaseItemViewModel<Mount>(Mount)
{
    #region Required Base Class Override

    public override string EntityName => "Mount";

    #endregion

    #region Mount-Specific Properties (READ-ONLY)

    public bool IsFavorite => Entity?.IsFavorite ?? false;
    public string? Material => Entity?.Material;
    public string? Size => Entity?.Size;
    public string? DrainageType => Entity?.DrainageType;

    #endregion

    #region Display Properties

    public string MaterialDisplay => string.IsNullOrWhiteSpace(Material) ? "Not specified" : Material;
    public string SizeDisplay => string.IsNullOrWhiteSpace(Size) ? "Not specified" : Size;
    public string DrainageDisplay => string.IsNullOrWhiteSpace(DrainageType) ? "Not specified" : DrainageType;
    public bool HasSpecifications => !string.IsNullOrWhiteSpace(Material) || !string.IsNullOrWhiteSpace(Size);

    #endregion

    #region Computed Properties

    private Mount Entity => ToModel();

    #endregion
}