namespace OrchidPro.Services.Localization;

public interface IFieldOptionsService
{
    List<string> GetPhRangeOptions(string language = "en");
    List<string> GetDrainageLevelOptions(string language = "en");
    List<string> GetMountMaterialOptions(string language = "en");
    List<string> GetMountSizeOptions(string language = "en");
    List<string> GetDrainageTypeOptions(string language = "en");
    List<string> GetSupplierTypeOptions(string language = "en");
    List<string> GetLocationTypeOptions(string language = "en");
}