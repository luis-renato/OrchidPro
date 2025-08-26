using OrchidPro.Extensions;
using OrchidPro.Models.Enums;
using System.Globalization;

namespace OrchidPro.Services.Localization;

public class SimpleFieldOptionsService : IFieldOptionsService
{
    private readonly string _currentLanguage;

    public SimpleFieldOptionsService()
    {
        _currentLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
    }

    private List<string> GetEnumOptions<T>(string? language = null) where T : struct, Enum
    {
        language ??= _currentLanguage;
        return [.. Enum.GetValues<T>().Select(e => e.ToDisplayText(language))];
    }

    public List<string> GetPhRangeOptions(string? language = null)
        => GetEnumOptions<PhRange>(language);

    public List<string> GetDrainageLevelOptions(string? language = null)
        => GetEnumOptions<DrainageLevel>(language);

    public List<string> GetMountMaterialOptions(string? language = null)
        => GetEnumOptions<MountMaterial>(language);

    public List<string> GetMountSizeOptions(string? language = null)
        => GetEnumOptions<MountSize>(language);

    public List<string> GetDrainageTypeOptions(string? language = null)
        => GetEnumOptions<DrainageType>(language);

    public List<string> GetSupplierTypeOptions(string? language = null)
        => GetEnumOptions<SupplierType>(language);

    public List<string> GetLocationTypeOptions(string? language = null)
        => GetEnumOptions<LocationType>(language);
}