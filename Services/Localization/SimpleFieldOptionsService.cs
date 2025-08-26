using System.Globalization;
using OrchidPro.Models.Enums;
using OrchidPro.Extensions;

namespace OrchidPro.Services.Localization;

public class SimpleFieldOptionsService : IFieldOptionsService
{
    private readonly string _currentLanguage;

    public SimpleFieldOptionsService()
    {
        _currentLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
    }

    public List<string> GetPhRangeOptions(string language = "en")
    {
        language ??= _currentLanguage;
        return Enum.GetValues<PhRange>()
                   .Select(e => e.ToDisplayText(language))
                   .ToList();
    }

    public List<string> GetDrainageLevelOptions(string language = "en")
    {
        language ??= _currentLanguage;
        return Enum.GetValues<DrainageLevel>()
                   .Select(e => e.ToDisplayText(language))
                   .ToList();
    }

    public List<string> GetMountMaterialOptions(string language = "en")
    {
        language ??= _currentLanguage;
        return Enum.GetValues<MountMaterial>()
                   .Select(e => e.ToDisplayText(language))
                   .ToList();
    }

    public List<string> GetMountSizeOptions(string language = "en")
    {
        language ??= _currentLanguage;
        return Enum.GetValues<MountSize>()
                   .Select(e => e.ToDisplayText(language))
                   .ToList();
    }

    public List<string> GetDrainageTypeOptions(string language = "en")
    {
        language ??= _currentLanguage;
        return Enum.GetValues<DrainageType>()
                   .Select(e => e.ToDisplayText(language))
                   .ToList();
    }

    public List<string> GetSupplierTypeOptions(string language = "en")
    {
        language ??= _currentLanguage;
        return Enum.GetValues<SupplierType>()
                   .Select(e => e.ToDisplayText(language))
                   .ToList();
    }

    public List<string> GetLocationTypeOptions(string language = "en")
    {
        language ??= _currentLanguage;
        return Enum.GetValues<LocationType>()
                   .Select(e => e.ToDisplayText(language))
                   .ToList();
    }
}