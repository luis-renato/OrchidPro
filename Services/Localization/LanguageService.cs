namespace OrchidPro.Services.Localization;

public class LanguageService : ILanguageService
{
    private readonly ILocalizationService _localizationService;
    private const string LANGUAGE_KEY = "selected_language";

    public event EventHandler<string>? LanguageChanged;

    public LanguageService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;

        // Carrega idioma salvo ou usa padrão
        var savedLanguage = Preferences.Default.Get(LANGUAGE_KEY, "en-US");

        _localizationService.SetLanguage(savedLanguage);

        System.Diagnostics.Debug.WriteLine($"[LanguageService] Initialized with saved language: {savedLanguage}");
        System.Diagnostics.Debug.WriteLine($"[LanguageService] Current language: {CurrentLanguage}");

        // Teste inicial para verificar se as traduções estão funcionando
        TestTranslations();
    }

    public string CurrentLanguage => _localizationService.GetCurrentLanguage();

    public List<LanguageOption> AvailableLanguages => new()
    {
        new() { Code = "en-US", DisplayName = "English (US)", NativeName = "English" },
        new() { Code = "pt-BR", DisplayName = "Portuguese (Brazil)", NativeName = "Português (Brasil)" }
    };

    public async Task SetLanguageAsync(string languageCode)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[LanguageService] SetLanguageAsync called with: {languageCode}");

            // Verificar se é uma mudança real
            var currentLang = CurrentLanguage;
            if (string.Equals(currentLang, languageCode, StringComparison.OrdinalIgnoreCase))
            {
                System.Diagnostics.Debug.WriteLine($"[LanguageService] Language is already set to {languageCode}, skipping change");
                return;
            }

            // Salva a preferência (código completo)
            Preferences.Default.Set(LANGUAGE_KEY, languageCode);

            System.Diagnostics.Debug.WriteLine($"[LanguageService] Saved language preference: {languageCode}");

            // Aplica a mudança
            _localizationService.SetLanguage(languageCode);

            System.Diagnostics.Debug.WriteLine($"[LanguageService] Applied language change to LocalizationService");

            // Verificar se a mudança foi efetiva
            var newCurrentLanguage = CurrentLanguage;
            System.Diagnostics.Debug.WriteLine($"[LanguageService] Language after change: {newCurrentLanguage}");

            // Teste das traduções após mudança
            await Task.Delay(100); // Pequeno delay para garantir que a mudança foi processada
            TestTranslations();

            // Notifica mudança
            LanguageChanged?.Invoke(this, languageCode);

            System.Diagnostics.Debug.WriteLine($"[LanguageService] LanguageChanged event fired with: {languageCode}");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LanguageService] Error setting language: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[LanguageService] Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Testa se as traduções estão funcionando corretamente
    /// </summary>
    private void TestTranslations()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[LanguageService] === TRANSLATION TEST ===");
            System.Diagnostics.Debug.WriteLine($"[LanguageService] Current Language: {CurrentLanguage}");

            // Testar algumas chaves conhecidas
            var testKeys = new[]
            {
                "PhRange_VeryAcidic_40_45",
                "DrainageLevel_High",
                "DrainageLevel_Medium"
            };

            foreach (var key in testKeys)
            {
                var translation = _localizationService.GetString(key);
                var isTranslated = !string.Equals(key, translation, StringComparison.OrdinalIgnoreCase);

                System.Diagnostics.Debug.WriteLine($"[LanguageService] Key: {key}");
                System.Diagnostics.Debug.WriteLine($"[LanguageService] Translation: {translation}");
                System.Diagnostics.Debug.WriteLine($"[LanguageService] Is Translated: {isTranslated}");
                System.Diagnostics.Debug.WriteLine($"[LanguageService] ---");
            }

            System.Diagnostics.Debug.WriteLine($"[LanguageService] === END TRANSLATION TEST ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LanguageService] Error in translation test: {ex.Message}");
        }
    }

    /// <summary>
    /// Método de diagnóstico para verificar o status do serviço
    /// </summary>
    public void DiagnoseLanguageService()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[LanguageService] === LANGUAGE SERVICE DIAGNOSIS ===");
            System.Diagnostics.Debug.WriteLine($"[LanguageService] Current Language: {CurrentLanguage}");
            System.Diagnostics.Debug.WriteLine($"[LanguageService] Saved Preference: {Preferences.Default.Get(LANGUAGE_KEY, "NOT_SET")}");
            System.Diagnostics.Debug.WriteLine($"[LanguageService] Available Languages: {string.Join(", ", AvailableLanguages.Select(l => $"{l.Code}({l.DisplayName})"))}");
            System.Diagnostics.Debug.WriteLine($"[LanguageService] System Culture: {System.Globalization.CultureInfo.CurrentUICulture.Name}");
            System.Diagnostics.Debug.WriteLine($"[LanguageService] Thread Culture: {System.Threading.Thread.CurrentThread.CurrentUICulture.Name}");

            TestTranslations();

            System.Diagnostics.Debug.WriteLine($"[LanguageService] === END DIAGNOSIS ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LanguageService] Error in diagnosis: {ex.Message}");
        }
    }
}