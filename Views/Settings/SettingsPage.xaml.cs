using OrchidPro.ViewModels.Settings;

namespace OrchidPro.Views.Settings;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;

        // Escuta mudan�as de idioma para atualizar UI
        _viewModel.LanguageChanged += OnLanguageChanged;
        UpdateLanguageDisplay();
    }

    private void OnLanguageChanged(object? sender, string language)
    {
        UpdateLanguageDisplay();
    }

    private void UpdateLanguageDisplay()
    {
        var current = _viewModel.CurrentLanguage;
        CurrentLanguageLabel.Text = current == "en" ? "Current: English" : "Atual: Portugu�s";
        LanguageToggleButton.Text = current == "en" ? "Switch to Portugu�s" : "Mudar para English";
    }
}