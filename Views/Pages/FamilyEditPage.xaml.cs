using OrchidPro.ViewModels.Families;

namespace OrchidPro.Views.Pages;

/// <summary>
/// ✅ CORRIGIDO: Family edit page sem conflitos de nomes
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

        // ✅ Animação + inicialização em paralelo
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
            // ✅ Verifica mudanças não salvas
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
    /// ✅ Performs enhanced entrance animation
    /// </summary>
    private async Task PerformEntranceAnimation()
    {
        try
        {
            // Set initial states
            MainGrid.Opacity = 0;
            MainGrid.Scale = 0.95;
            MainGrid.TranslationY = 30;

            // Animate with multiple effects
            await Task.WhenAll(
                MainGrid.FadeTo(1, 500, Easing.CubicOut),
                MainGrid.ScaleTo(1, 500, Easing.SpringOut),
                MainGrid.TranslateTo(0, 0, 500, Easing.CubicOut)
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [FAMILY_EDIT_PAGE] Animation error: {ex.Message}");
            MainGrid.Opacity = 1;
        }
    }

    /// <summary>
    /// Performs smooth exit animation
    /// </summary>
    private async Task PerformExitAnimation()
    {
        try
        {
            await Task.WhenAll(
                MainGrid.FadeTo(0, 300, Easing.CubicIn),
                MainGrid.ScaleTo(0.95, 300, Easing.CubicIn),
                MainGrid.TranslateTo(0, -20, 300, Easing.CubicIn)
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [FAMILY_EDIT_PAGE] Exit animation error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ Handles entry focus with animation
    /// </summary>
    private async void OnEntryFocused(object sender, FocusEventArgs e)
    {
        try
        {
            if (sender is Entry entry && e.IsFocused)
            {
                // Animate field focus
                if (entry.Parent is Border border)
                {
                    await border.ScaleTo(1.01, 150, Easing.CubicOut);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [FAMILY_EDIT_PAGE] Entry focus error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ Handles entry unfocus with animation
    /// </summary>
    private async void OnEntryUnfocused(object sender, FocusEventArgs e)
    {
        try
        {
            if (sender is Entry entry && !e.IsFocused)
            {
                // Animate field unfocus
                if (entry.Parent is Border border)
                {
                    await border.ScaleTo(1, 150, Easing.CubicOut);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [FAMILY_EDIT_PAGE] Entry unfocus error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles button press animations
    /// </summary>
    private async void OnButtonPressed(object sender, EventArgs e)
    {
        try
        {
            if (sender is Button button)
            {
                await button.ScaleTo(0.96, 50, Easing.CubicOut);
                await button.ScaleTo(1, 50, Easing.CubicOut);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [FAMILY_EDIT_PAGE] Button press error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles switch toggle with visual feedback
    /// </summary>
    private async void OnSwitchToggled(object sender, ToggledEventArgs e)
    {
        try
        {
            if (sender is Switch switchControl)
            {
                // Visual feedback only
                await switchControl.ScaleTo(1.05, 80, Easing.CubicOut);
                await switchControl.ScaleTo(1, 80, Easing.CubicOut);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [FAMILY_EDIT_PAGE] Switch toggle error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ Testa conectividade e mostra overlay temporário
    /// </summary>
    private async void OnTestConnectivityTapped(object sender, EventArgs e)
    {
        try
        {
            // Mostrar overlay de teste
            ConnectionOverlay.IsVisible = true;

            // Executar teste via ViewModel
            await _viewModel.TestConnectionCommand.ExecuteAsync(null);

            // Esconder overlay após 2 segundos
            await Task.Delay(2000);
            ConnectionOverlay.IsVisible = false;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Test Error", $"Connection test failed: {ex.Message}", "OK");
            ConnectionOverlay.IsVisible = false;
        }
    }
}