
using System.Globalization;
using System.Resources;

namespace OrchidPro.Services.Localization;

public interface ILocalizationService
{
    string GetString(string key, string? fallback = null);
    string GetCurrentLanguage();
    void SetLanguage(string language);
    event EventHandler? LanguageChanged;
}
