namespace OrchidPro.Services.Localization;

public interface ILanguageService
{
    string CurrentLanguage { get; }
    List<LanguageOption> AvailableLanguages { get; }
    Task SetLanguageAsync(string languageCode);
    event EventHandler<string> LanguageChanged;
}