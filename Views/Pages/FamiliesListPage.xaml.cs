using OrchidPro.ViewModels.Families;

namespace OrchidPro.Views.Pages;

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
        await PerformEntranceAnimation();
        await _viewModel.OnAppearingAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await PerformExitAnimation();
    }

    /// <summary>
    /// Animação de entrada da página com fade in suave
    /// </summary>
    private async Task PerformEntranceAnimation()
    {
        try
        {
            // Estados iniciais para animação
            RootGrid.Opacity = 0;
            RootGrid.Scale = 0.95;
            RootGrid.TranslationY = 30;

            FabButton.Opacity = 0;
            FabButton.Scale = 0.8;
            FabButton.TranslationY = 50;

            // Animação principal do conteúdo
            await Task.WhenAll(
                RootGrid.FadeTo(1, 600, Easing.CubicOut),
                RootGrid.ScaleTo(1, 600, Easing.SpringOut),
                RootGrid.TranslateTo(0, 0, 600, Easing.CubicOut)
            );

            // Animação do FAB com delay
            await Task.Delay(200);
            await Task.WhenAll(
                FabButton.FadeTo(1, 400, Easing.CubicOut),
                FabButton.ScaleTo(1, 400, Easing.SpringOut),
                FabButton.TranslateTo(0, 0, 400, Easing.CubicOut)
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Entrance animation error: {ex.Message}");
            // Garantir que elementos ficam visíveis mesmo com erro
            RootGrid.Opacity = 1;
            FabButton.Opacity = 1;
        }
    }

    /// <summary>
    /// Animação de saída da página
    /// </summary>
    private async Task PerformExitAnimation()
    {
        try
        {
            await Task.WhenAll(
                RootGrid.FadeTo(0.8, 300, Easing.CubicIn),
                RootGrid.ScaleTo(0.98, 300, Easing.CubicIn),
                FabButton.FadeTo(0, 200, Easing.CubicIn),
                FabButton.ScaleTo(0.9, 200, Easing.CubicIn)
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Exit animation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handler do FAB - CÓDIGO ORIGINAL
    /// </summary>
    private async void OnFabPressed(object sender, EventArgs e)
    {
        try
        {
            // Animação de feedback do FAB
            await FabButton.ScaleTo(0.9, 100, Easing.CubicIn);
            await FabButton.ScaleTo(1, 100, Easing.CubicOut);

            // O comando já está bindado via Style.Triggers no XAML
            // Não precisa executar manualmente aqui
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] FAB animation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handler do filtro - CÓDIGO ORIGINAL
    /// </summary>
    private async void OnFilterTapped(object sender, EventArgs e)
    {
        try
        {
            string[] options = { "All", "Active", "Inactive" };
            string result = await DisplayActionSheet("Filter by Status", "Cancel", null, options);

            if (result != "Cancel" && result != null)
            {
                _viewModel.StatusFilter = result;
                await _viewModel.FilterByStatusCommand.ExecuteAsync(null);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [FAMILIES_LIST_PAGE] Filter error: {ex.Message}");
        }
    }

    #region LongPress Implementation

    private DateTime _pressStartTime;
    private bool _isLongPressHandled;
    private bool _wasInMultiSelectMode; // Nova flag para lembrar o estado
    private const int LongPressDurationMs = 800; // 800ms para LongPress

    /// <summary>
    /// 🎯 Handler do Pressed - inicia timer para LongPress
    /// </summary>
    private void OnItemPressed(object sender, EventArgs e)
    {
        _pressStartTime = DateTime.Now;
        _isLongPressHandled = false;
        _wasInMultiSelectMode = _viewModel.IsMultiSelectMode; // Salvar estado atual

        System.Diagnostics.Debug.WriteLine($"🔘 [FAMILIES_LIST_PAGE] Pressed - MultiSelect was: {_wasInMultiSelectMode}");

        // Pequena animação de feedback
        if (sender is Button button)
        {
            _ = button.ScaleTo(0.95, 100, Easing.CubicOut);
        }

        // Timer para detectar LongPress
        _ = Task.Run(async () =>
        {
            await Task.Delay(LongPressDurationMs);

            if (!_isLongPressHandled && (DateTime.Now - _pressStartTime).TotalMilliseconds >= LongPressDurationMs)
            {
                _isLongPressHandled = true;

                // Executar LongPress no UI thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (sender is Button btn && btn.BindingContext is FamilyItemViewModel item)
                    {
                        System.Diagnostics.Debug.WriteLine($"🔘 [FAMILIES_LIST_PAGE] LongPress detected on: {item.Name}");
                        _viewModel.ItemLongPressCommand.Execute(item);
                        _wasInMultiSelectMode = true; // Agora está em modo seleção
                    }
                });
            }
        });
    }

    /// <summary>
    /// 🎯 Handler do Released - CORRIGIDO para não interferir com LongPress
    /// </summary>
    private async void OnItemReleased(object sender, EventArgs e)
    {
        var pressDuration = (DateTime.Now - _pressStartTime).TotalMilliseconds;

        // Restaurar animação
        if (sender is Button button)
        {
            await button.ScaleTo(1.0, 100, Easing.CubicOut);
        }

        System.Diagnostics.Debug.WriteLine($"📤 [FAMILIES_LIST_PAGE] Released after {pressDuration}ms, LongPress handled: {_isLongPressHandled}, Was in MultiSelect: {_wasInMultiSelectMode}");

        // Se LongPress foi executado, NÃO fazer mais nada
        if (_isLongPressHandled)
        {
            System.Diagnostics.Debug.WriteLine($"🔘 [FAMILIES_LIST_PAGE] LongPress was handled - ignoring release");
            return;
        }

        // Se foi um tap rápido
        if (pressDuration < LongPressDurationMs)
        {
            _isLongPressHandled = true; // Prevenir LongPress tardio

            if (sender is Button btn && btn.BindingContext is FamilyItemViewModel item)
            {
                System.Diagnostics.Debug.WriteLine($"👆 [FAMILIES_LIST_PAGE] Quick tap on: {item.Name}");

                // Se estava em modo multi-seleção, apenas toggle manualmente
                if (_wasInMultiSelectMode)
                {
                    System.Diagnostics.Debug.WriteLine($"🔘 [FAMILIES_LIST_PAGE] Manual toggle selection for: {item.Name}");

                    // Toggle manual da seleção
                    item.IsSelected = !item.IsSelected;

                    // Atualizar lista de selecionados manualmente
                    if (item.IsSelected)
                    {
                        if (!_viewModel.SelectedItems.Contains(item))
                        {
                            _viewModel.SelectedItems.Add(item);
                        }
                    }
                    else
                    {
                        _viewModel.SelectedItems.Remove(item);
                    }

                    System.Diagnostics.Debug.WriteLine($"🔘 [FAMILIES_LIST_PAGE] Item {item.Name} now selected: {item.IsSelected}, Total selected: {_viewModel.SelectedItems.Count}");
                }
                else
                {
                    // Não estava em modo seleção - navegar normalmente
                    System.Diagnostics.Debug.WriteLine($"👆 [FAMILIES_LIST_PAGE] Normal navigation for: {item.Name}");
                    _viewModel.ItemTappedCommand.Execute(item);
                }
            }
        }
    }

    #endregion
}