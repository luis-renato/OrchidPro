namespace OrchidPro.Services.Localization;

public interface IFieldOptionsService
{
    List<string> GetPhRangeOptions(string? language = null); 
    List<string> GetDrainageLevelOptions(string? language = null);
    List<string> GetMountMaterialOptions(string? language = null);
    List<string> GetMountSizeOptions(string? language = null);
    List<string> GetDrainageTypeOptions(string? language = null);
    List<string> GetSupplierTypeOptions(string? language = null);
    List<string> GetLocationTypeOptions(string? language = null);
}