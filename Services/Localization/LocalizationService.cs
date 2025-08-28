using System.Globalization;
using System.Resources;
using OrchidPro.Resources.Strings;

namespace OrchidPro.Services.Localization;

public class LocalizationService : ILocalizationService
{
    private readonly ResourceManager _resourceManager;
    private CultureInfo _currentCulture;

    public event EventHandler? LanguageChanged;

    public LocalizationService()
    {
        _resourceManager = AppStrings.ResourceManager;
        _currentCulture = CultureInfo.CurrentUICulture;

        // Log inicial para debug
        System.Diagnostics.Debug.WriteLine($"[LocalizationService] Initialized with culture: {_currentCulture.Name}");
        System.Diagnostics.Debug.WriteLine($"[LocalizationService] ResourceManager BaseName: {_resourceManager.BaseName}");
    }

    public string GetString(string key, string? fallback = null)
    {
        try
        {
            var value = _resourceManager.GetString(key, _currentCulture);
            var result = value ?? fallback ?? key;

            // Log detalhado para debug
            System.Diagnostics.Debug.WriteLine($"[LocalizationService] GetString('{key}')");
            System.Diagnostics.Debug.WriteLine($"  Culture: {_currentCulture.Name}");
            System.Diagnostics.Debug.WriteLine($"  Result: '{result}'");
            System.Diagnostics.Debug.WriteLine($"  Was Fallback Used: {value == null}");

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LocalizationService] Error getting string '{key}': {ex.Message}");
            return fallback ?? key;
        }
    }

    public string GetCurrentLanguage()
    {
        return _currentCulture.Name;
    }

    public void SetLanguage(string language)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[LocalizationService] SetLanguage called with: {language}");

            // Usar o código exato fornecido sem normalização
            var newCulture = new CultureInfo(language);
            _currentCulture = newCulture;

            // IMPORTANTE: Definir a cultura para todo o thread
            CultureInfo.CurrentUICulture = newCulture;
            CultureInfo.CurrentCulture = newCulture;
            Thread.CurrentThread.CurrentUICulture = newCulture;
            Thread.CurrentThread.CurrentCulture = newCulture;

            System.Diagnostics.Debug.WriteLine($"[LocalizationService] Language changed to: {newCulture.Name}");
            System.Diagnostics.Debug.WriteLine($"[LocalizationService] CurrentUICulture: {CultureInfo.CurrentUICulture.Name}");
            System.Diagnostics.Debug.WriteLine($"[LocalizationService] Thread Culture: {Thread.CurrentThread.CurrentUICulture.Name}");

            // Teste imediato para verificar se funciona
            TestResourceAccess(newCulture);

            // Notificar mudança
            LanguageChanged?.Invoke(this, EventArgs.Empty);

            System.Diagnostics.Debug.WriteLine($"[LocalizationService] LanguageChanged event fired");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LocalizationService] Error setting language to '{language}': {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[LocalizationService] Exception details: {ex}");
        }
    }

    private void TestResourceAccess(CultureInfo culture)
    {
        try
        {
            var testKey = "PhRange_VeryAcidic_40_45";
            var testResult = _resourceManager.GetString(testKey, culture);

            System.Diagnostics.Debug.WriteLine($"[LocalizationService] Resource Test:");
            System.Diagnostics.Debug.WriteLine($"  Culture: {culture.Name}");
            System.Diagnostics.Debug.WriteLine($"  Key: {testKey}");
            System.Diagnostics.Debug.WriteLine($"  Result: {testResult ?? "NULL"}");

            // Teste adicional: verificar algumas chaves conhecidas
            var additionalTests = new[]
            {
                "DrainageLevel_High",
                "Label_Name",
                "Button_Save"
            };

            foreach (var additionalKey in additionalTests)
            {
                var additionalResult = _resourceManager.GetString(additionalKey, culture);
                System.Diagnostics.Debug.WriteLine($"  Additional Test - {additionalKey}: {additionalResult ?? "NULL"}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LocalizationService] Resource test failed: {ex.Message}");
        }
    }
}