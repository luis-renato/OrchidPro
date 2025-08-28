using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.ViewModels.Base;
using OrchidPro.Services.Localization;

namespace OrchidPro.ViewModels.Settings;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly ILanguageService _languageService;

    public event EventHandler<string>? LanguageChanged;

    public SettingsViewModel(ILanguageService languageService)
    {
        _languageService = languageService;
        _languageService.LanguageChanged += OnServiceLanguageChanged;
    }

    public string CurrentLanguage => _languageService.CurrentLanguage;

    [RelayCommand]
    private async Task ToggleLanguage()
    {
        var newLanguage = CurrentLanguage == "en" ? "pt" : "en";
        await _languageService.SetLanguageAsync(newLanguage);
    }

    private void OnServiceLanguageChanged(object? sender, string language)
    {
        OnPropertyChanged(nameof(CurrentLanguage));
        LanguageChanged?.Invoke(this, language);

        var displayName = language == "en" ? "English" : "Português";
        Application.Current?.MainPage?.DisplayAlert("Language Changed", $"Language changed to {displayName}", "OK");
    }
}