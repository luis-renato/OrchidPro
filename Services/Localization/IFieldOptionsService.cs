namespace OrchidPro.Services.Localization;

public interface IFieldOptionsService
{
    // Métodos para chaves (salvar no banco)
    List<string> GetPhRangeKeys();
    List<string> GetDrainageLevelKeys();
    List<string> GetMountMaterialKeys();
    List<string> GetMountSizeKeys();
    List<string> GetDrainageTypeKeys();
    List<string> GetSupplierTypeKeys();
    List<string> GetLocationTypeKeys();

    // Métodos para display (UI)
    List<string> GetPhRangeOptions(string? language = null);
    List<string> GetDrainageLevelOptions(string? language = null);
    List<string> GetMountMaterialOptions(string? language = null);
    List<string> GetMountSizeOptions(string? language = null);
    List<string> GetDrainageTypeOptions(string? language = null);
    List<string> GetSupplierTypeOptions(string? language = null);
    List<string> GetLocationTypeOptions(string? language = null);

    // Helper methods
    string GetDisplayForKey(string key);
    string GetKeyForDisplay(string display, List<string> keys);
}