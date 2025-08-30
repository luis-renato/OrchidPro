// Services/Localization/SimpleFieldOptionsService.cs
using OrchidPro.Models.Enums;

namespace OrchidPro.Services.Localization;

public class SimpleFieldOptionsService : IFieldOptionsService
{
    private readonly ILocalizationService _localizationService;

    public SimpleFieldOptionsService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    #region Key Methods - Return keys for saving to database

    public List<string> GetPhRangeKeys()
        => [.. Enum.GetValues<PhRange>().Select(e => $"PhRange.{e}")];

    public List<string> GetDrainageLevelKeys()
        => [.. Enum.GetValues<DrainageLevel>().Select(e => $"DrainageLevel.{e}")];

    public List<string> GetMountMaterialKeys()
        => [.. Enum.GetValues<MountMaterial>().Select(e => $"MountMaterial.{e}")];

    public List<string> GetMountSizeKeys()
        => [.. Enum.GetValues<MountSize>().Select(e => $"MountSize.{e}")];

    public List<string> GetDrainageTypeKeys()
        => [.. Enum.GetValues<DrainageType>().Select(e => $"DrainageType.{e}")];

    public List<string> GetSupplierTypeKeys()
        => [.. Enum.GetValues<SupplierType>().Select(e => $"SupplierType.{e}")];

    public List<string> GetLocationTypeKeys()
        => [.. Enum.GetValues<LocationType>().Select(e => $"LocationType.{e}")];

    // NEW: Plant-specific key methods
    public List<string> GetWaterTypeKeys()
        => [.. Enum.GetValues<WaterType>().Select(e => $"WaterType.{e}")];

    public List<string> GetFertilizerTypeKeys()
        => [.. Enum.GetValues<FertilizerType>().Select(e => $"FertilizerType.{e}")];

    public List<string> GetHealthSeverityKeys()
        => [.. Enum.GetValues<HealthSeverity>().Select(e => $"HealthSeverity.{e}")];

    #endregion

    #region Display Methods - Return translated displays for UI

    public List<string> GetPhRangeOptions(string? language = null)
        => [.. GetPhRangeKeys().Select(key => _localizationService.GetString(key))];

    public List<string> GetDrainageLevelOptions(string? language = null)
        => [.. GetDrainageLevelKeys().Select(key => _localizationService.GetString(key))];

    public List<string> GetMountMaterialOptions(string? language = null)
        => [.. GetMountMaterialKeys().Select(key => _localizationService.GetString(key))];

    public List<string> GetMountSizeOptions(string? language = null)
        => [.. GetMountSizeKeys().Select(key => _localizationService.GetString(key))];

    public List<string> GetDrainageTypeOptions(string? language = null)
        => [.. GetDrainageTypeKeys().Select(key => _localizationService.GetString(key))];

    public List<string> GetSupplierTypeOptions(string? language = null)
        => [.. GetSupplierTypeKeys().Select(key => _localizationService.GetString(key))];

    public List<string> GetLocationTypeOptions(string? language = null)
        => [.. GetLocationTypeKeys().Select(key => _localizationService.GetString(key))];

    // NEW: Plant-specific display methods
    public List<string> GetWaterTypeOptions(string? language = null)
        => [.. GetWaterTypeKeys().Select(key => _localizationService.GetString(key))];

    public List<string> GetFertilizerTypeOptions(string? language = null)
        => [.. GetFertilizerTypeKeys().Select(key => _localizationService.GetString(key))];

    public List<string> GetHealthSeverityOptions(string? language = null)
        => [.. GetHealthSeverityKeys().Select(key => _localizationService.GetString(key))];

    #endregion

    #region Helper Methods

    /// <summary>
    /// Convert a key to translated display text
    /// </summary>
    public string GetDisplayForKey(string key)
    {
        return _localizationService.GetString(key, key);
    }

    /// <summary>
    /// Find the key corresponding to a translated display text
    /// </summary>
    public string GetKeyForDisplay(string display, List<string> keys)
    {
        return keys.FirstOrDefault(key =>
            _localizationService.GetString(key).Equals(display, StringComparison.OrdinalIgnoreCase)) ?? "";
    }

    #endregion
}