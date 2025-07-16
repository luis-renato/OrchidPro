using OrchidPro.ViewModels;

namespace OrchidPro.Views.Pages;

/// <summary>
/// PASSO 12.2: CORRIGIDO - Families list page com FAB funcionando
/// ✅ PROBLEMA RESOLVIDO: OnFabPressed agora chama os commands corretos do ViewModel
/// </summary>
public partial class FamiliesListPage : ContentPage
{
    private readonly FamiliesListViewModel _viewModel;

    public FamiliesListPage(FamiliesListViewModel viewModel)
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

    /// <summary>
    /// ✅ OTIMIZADO: Performs dramatic entrance animation
    /// </summary>
    private async Task PerformEntranceAnimation()
    {
        // Set initial states for dramatic effect
        RootGrid.Opacity = 0;
        RootGrid.Scale = 0.95;
        FabButton.Scale = 0;
        FabButton.Rotation = -90;

        // Animate main content with fade + scale mais suave
        var contentTask = Task.WhenAll(
            RootGrid.FadeTo(1, 500, Easing.CubicOut),
            RootGrid.ScaleTo(1, 500, Easing.SpringOut)
        );

        // Wait for content, then animate FAB
        await contentTask;

        await Task.WhenAll(
            FabButton.ScaleTo(0.9, 300, Easing.SpringOut),
            FabButton.RotateTo(0, 300, Easing.CubicOut)
        );
    }

    /// <summary>
    /// Performs smooth exit animation
    /// </summary>
    private async Task PerformExitAnimation()
    {
        // Animate FAB out first
        var fabTask = Task.WhenAll(
            FabButton.ScaleTo(0, 200, Easing.CubicIn),
            FabButton.RotateTo(90, 200, Easing.CubicIn)
        );

        // Then animate main content
        var contentTask = Task.WhenAll(
            RootGrid.FadeTo(0, 300, Easing.CubicIn),
            RootGrid.ScaleTo(0.95, 300, Easing.CubicIn)
        );

        await Task.WhenAll(fabTask, contentTask);
    }

    /// <summary>
    /// Handles button press animations for better feedback
    /// </summary>
    private async void OnButtonPressed(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            await button.ScaleTo(0.95, 50, Easing.CubicOut);
            await button.ScaleTo(1, 50, Easing.CubicOut);
        }
    }

    /// <summary>
    /// ✅ LIMPO: Handles filter selection apenas Status
    /// </summary>
    private async void OnFilterTapped(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            string action = "";

            if (button.Text?.Contains("Status") == true)
            {
                action = await DisplayActionSheet(
                    "Filter by Status",
                    "Cancel",
                    null,
                    _viewModel.StatusFilterOptions.ToArray());

                if (!string.IsNullOrEmpty(action) && action != "Cancel")
                {
                    _viewModel.StatusFilter = action;
                }
            }
        }
    }

    /// <summary>
    /// ✅ CORRIGIDO: Handle FAB button - AGORA CHAMA OS COMMANDS DO VIEWMODEL!
    /// </summary>
    private async void OnFabPressed(object sender, EventArgs e)
    {
        if (sender is Button fab)
        {
            // ✅ ANIMAÇÃO: Special animation for FAB
            await fab.ScaleTo(0.9, 100, Easing.CubicOut);
            await fab.ScaleTo(1, 100, Easing.SpringOut);

            // ✅ CORRIGIDO: Agora chama o command apropriado do ViewModel
            try
            {
                var selectedCount = _viewModel.SelectedItems.Count;

                if (selectedCount > 0)
                {
                    // Modo DELETE: Executa delete dos selecionados
                    if (_viewModel.DeleteSelectedCommand.CanExecute(null))
                    {
                        await _viewModel.DeleteSelectedCommand.ExecuteAsync(null);
                    }
                }
                else if (_viewModel.IsMultiSelectMode)
                {
                    // Modo CANCEL: Sai do modo de multisseleção
                    if (_viewModel.ToggleMultiSelectCommand.CanExecute(null))
                    {
                        _viewModel.ToggleMultiSelectCommand.Execute(null);
                    }
                }
                else
                {
                    // Modo ADD: Navega para adicionar família
                    if (_viewModel.AddItemCommand.CanExecute(null))
                    {
                        await _viewModel.AddItemCommand.ExecuteAsync(null);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"❌ [FAB_PRESSED] Error: {ex.Message}");
            }
        }
    }
}