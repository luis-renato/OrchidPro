// Services/Localization/IFieldOptionsService.cs
namespace OrchidPro.Services.Localization;

public interface IFieldOptionsService
{
    // Existing methods
    List<string> GetPhRangeKeys();
    List<string> GetDrainageLevelKeys();
    List<string> GetMountMaterialKeys();
    List<string> GetMountSizeKeys();
    List<string> GetDrainageTypeKeys();
    List<string> GetSupplierTypeKeys();
    List<string> GetLocationTypeKeys();

    // NEW: Plant-specific methods
    List<string> GetWaterTypeKeys();
    List<string> GetFertilizerTypeKeys();
    List<string> GetHealthSeverityKeys();

    // Existing display methods
    List<string> GetPhRangeOptions(string? language = null);
    List<string> GetDrainageLevelOptions(string? language = null);
    List<string> GetMountMaterialOptions(string? language = null);
    List<string> GetMountSizeOptions(string? language = null);
    List<string> GetDrainageTypeOptions(string? language = null);
    List<string> GetSupplierTypeOptions(string? language = null);
    List<string> GetLocationTypeOptions(string? language = null);

    // NEW: Plant-specific display methods
    List<string> GetWaterTypeOptions(string? language = null);
    List<string> GetFertilizerTypeOptions(string? language = null);
    List<string> GetHealthSeverityOptions(string? language = null);

    // Helper methods
    string GetDisplayForKey(string key);
    string GetKeyForDisplay(string display, List<string> keys);
}