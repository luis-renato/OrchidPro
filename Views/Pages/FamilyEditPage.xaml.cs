using OrchidPro.ViewModels;

namespace OrchidPro.Views.Pages;

/// <summary>
/// CORRIGIDO: Family edit page com animações otimizadas e tratamento de conectividade
/// </summary>
public partial class FamilyEditPage : ContentPage
{
    private readonly FamilyEditViewModel _viewModel;

    public FamilyEditPage(FamilyEditViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // ✅ OTIMIZADO: Animação + inicialização em paralelo
        var animationTask = PerformEntranceAnimation();
        var initTask = _viewModel.OnAppearingAsync();

        // Aguarda ambos completarem
        await Task.WhenAll(animationTask, initTask);
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        // Perform exit animation
        await PerformExitAnimation();

        // Cleanup ViewModel
        await _viewModel.OnDisappearingAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        // Handle back button with unsaved changes check
        _ = Task.Run(async () =>
        {
            // ✅ CORRIGIDO: Verifica mudanças não salvas
            if (_viewModel.HasUnsavedChanges)
            {
                var canNavigate = await _viewModel.ShowConfirmAsync(
                    "Unsaved Changes",
                    "You have unsaved changes. Discard them?");

                if (canNavigate)
                {
                    await Shell.Current.GoToAsync("..");
                }
            }
            else
            {
                await Shell.Current.GoToAsync("..");
            }
        });

        return true; // Always handle the back button
    }

    /// <summary>
    /// ✅ OTIMIZADO: Performs enhanced entrance animation
    /// </summary>
    private async Task PerformEntranceAnimation()
    {
        // Set initial states
        RootGrid.Opacity = 0;
        RootGrid.Scale = 0.95;
        RootGrid.TranslationY = 30;

        // Animate with multiple effects mais suave
        await Task.WhenAll(
            RootGrid.FadeTo(1, 500, Easing.CubicOut),
            RootGrid.ScaleTo(1, 500, Easing.SpringOut),
            RootGrid.TranslateTo(0, 0, 500, Easing.CubicOut)
        );
    }

    /// <summary>
    /// Performs smooth exit animation
    /// </summary>
    private async Task PerformExitAnimation()
    {
        await Task.WhenAll(
            RootGrid.FadeTo(0, 300, Easing.CubicIn),
            RootGrid.ScaleTo(0.95, 300, Easing.CubicIn),
            RootGrid.TranslateTo(0, -20, 300, Easing.CubicIn)
        );
    }

    /// <summary>
    /// ✅ CORRIGIDO: Handles entry focus with animation otimizada
    /// </summary>
    private async void OnEntryFocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && e.IsFocused)
        {
            // Animate field focus mais sutil
            if (entry.Parent is Border border)
            {
                await border.ScaleTo(1.01, 150, Easing.CubicOut);
            }
        }
    }

    /// <summary>
    /// ✅ CORRIGIDO: Handles entry unfocus with animation otimizada
    /// </summary>
    private async void OnEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && !e.IsFocused)
        {
            // Animate field unfocus mais sutil
            if (entry.Parent is Border border)
            {
                await border.ScaleTo(1, 150, Easing.CubicOut);
            }
        }
    }

    /// <summary>
    /// Handles button press animations
    /// </summary>
    private async void OnButtonPressed(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            await button.ScaleTo(0.96, 50, Easing.CubicOut);
            await button.ScaleTo(1, 50, Easing.CubicOut);
        }
    }

    /// <summary>
    /// Handles switch toggle with visual feedback
    /// </summary>
    private async void OnSwitchToggled(object sender, ToggledEventArgs e)
    {
        if (sender is Switch switchControl)
        {
            // Visual feedback only
            await switchControl.ScaleTo(1.05, 80, Easing.CubicOut);
            await switchControl.ScaleTo(1, 80, Easing.CubicOut);
        }
    }

    /// <summary>
    /// ✅ NOVO: Testa conectividade e mostra overlay temporário
    /// </summary>
    private async void OnTestConnectivityTapped(object sender, EventArgs e)
    {
        try
        {
            // Mostrar overlay de teste
            ConnectionTestOverlay.IsVisible = true;

            // Executar teste via ViewModel
            await _viewModel.TestConnectionCommand.ExecuteAsync(null);

            // Esconder overlay após 2 segundos
            await Task.Delay(2000);
            ConnectionTestOverlay.IsVisible = false;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Test Error", $"Connection test failed: {ex.Message}", "OK");
            ConnectionTestOverlay.IsVisible = false;
        }
    }
}