using OrchidPro.ViewModels.Families;
using System.Diagnostics;

namespace OrchidPro.Views.Pages;

/// <summary>
/// ✅ CORRIGIDO: FamilyEditPage com interceptação inteligente de navegação
/// </summary>
public partial class FamilyEditPage : ContentPage, IQueryAttributable
{
    private readonly FamilyEditViewModel _viewModel;
    private bool _isNavigating = false;
    private bool _isNavigationHandlerAttached = false;

    public FamilyEditPage(FamilyEditViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = _viewModel;

        try
        {
            InitializeComponent();
            Debug.WriteLine("✅ [FAMILY_EDIT_PAGE] InitializeComponent completed successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_PAGE] InitializeComponent failed: {ex.Message}");
        }

        Debug.WriteLine("✅ [FAMILY_EDIT_PAGE] Initialized successfully");
    }

    /// <summary>
    /// ✅ SIMPLIFICADO: Intercepta navegação SEMPRE, decide se mostra dialog baseado no HasUnsavedChanges
    /// </summary>
    private async void OnShellNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        // ✅ Só interceptar navegação de volta da toolbar
        if (_isNavigating || (e.Source != ShellNavigationSource.Pop && e.Source != ShellNavigationSource.PopToRoot))
            return;

        Debug.WriteLine($"⬅️ [FAMILY_EDIT_PAGE] Toolbar navigation detected - HasUnsavedChanges: {_viewModel.HasUnsavedChanges}");

        // ✅ SÓ interceptar se há mudanças não salvas
        if (_viewModel.HasUnsavedChanges)
        {
            // ✅ Cancelar navegação para interceptar
            e.Cancel();
            _isNavigating = true;

            try
            {
                // ✅ IMPORTANTE: Remover handler ANTES de chamar CancelCommand para evitar interferência
                DetachNavigationHandler();

                Debug.WriteLine("🔄 [FAMILY_EDIT_PAGE] Handler removed, delegating to CancelCommand");

                if (_viewModel.CancelCommand.CanExecute(null))
                {
                    await _viewModel.CancelCommand.ExecuteAsync(null);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ [FAMILY_EDIT_PAGE] Navigation handler error: {ex.Message}");
            }
            finally
            {
                _isNavigating = false;
                // ✅ NÃO reativar handler aqui - deixar que seja gerenciado naturalmente
            }
        }
        // Se não há mudanças, deixa navegar normalmente (não cancela)
    }

    /// <summary>
    /// ✅ SIMPLIFICADO: Remove handler de navegação
    /// </summary>
    private void DetachNavigationHandler()
    {
        if (_isNavigationHandlerAttached)
        {
            Shell.Current.Navigating -= OnShellNavigating;
            _isNavigationHandlerAttached = false;
            Debug.WriteLine("🔗 [FAMILY_EDIT_PAGE] Navigation handler detached");
        }
    }

    #region ✅ Query Attributes

    /// <summary>
    /// ✅ Implementa IQueryAttributable para receber parâmetros de navegação
    /// </summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        try
        {
            Debug.WriteLine($"🔍 [FAMILY_EDIT_PAGE] ApplyQueryAttributes called with {query.Count} parameters");

            foreach (var param in query)
            {
                Debug.WriteLine($"📝 [FAMILY_EDIT_PAGE] Parameter: {param.Key} = {param.Value} ({param.Value?.GetType().Name})");
            }

            // Passar parâmetros para o ViewModel
            _viewModel.ApplyQueryAttributes(query);

            Debug.WriteLine($"✅ [FAMILY_EDIT_PAGE] Parameters applied to ViewModel");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_PAGE] ApplyQueryAttributes error: {ex.Message}");
        }
    }

    #endregion

    #region Page Lifecycle

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            Debug.WriteLine($"👀 [FAMILY_EDIT_PAGE] OnAppearing - Mode: {(_viewModel.IsEditMode ? "EDIT" : "CREATE")}");

            // ✅ SEMPRE interceptar navegação da toolbar - mais simples
            if (!_isNavigationHandlerAttached)
            {
                Shell.Current.Navigating += OnShellNavigating;
                _isNavigationHandlerAttached = true;
                Debug.WriteLine("🔗 [FAMILY_EDIT_PAGE] Navigation handler attached (always active)");
            }

            // Animação + inicialização em paralelo
            var animationTask = PerformEntranceAnimation();
            var initTask = _viewModel.OnAppearingAsync();

            // Aguarda ambos completarem
            await Task.WhenAll(animationTask, initTask);

            Debug.WriteLine($"✅ [FAMILY_EDIT_PAGE] Page fully loaded and initialized");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_PAGE] OnAppearing error: {ex.Message}");
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        try
        {
            Debug.WriteLine("👋 [FAMILY_EDIT_PAGE] OnDisappearing");

            // ✅ SEMPRE remover handler
            DetachNavigationHandler();

            // Perform exit animation
            await PerformExitAnimation();

            // Cleanup ViewModel
            await _viewModel.OnDisappearingAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_PAGE] OnDisappearing error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ REMOVIDO: Não precisa mais monitorar PropertyChanged - handler sempre ativo
    /// </summary>
    // private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    // {
    //     if (e.PropertyName == nameof(_viewModel.HasUnsavedChanges))
    //     {
    //         AttachNavigationHandlerIfNeeded();
    //     }
    // }

    /// <summary>
    /// ✅ CORRIGIDO: Handle apenas back button físico do Android - delega para CancelCommand
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
        // ✅ Verificar se já está navegando para evitar múltiplos dialogs
        if (_isNavigating)
            return true;

        // ✅ Para botão físico, redirecionar para o comando Cancel da classe base
        _ = Task.Run(async () =>
        {
            try
            {
                Debug.WriteLine($"⬅️ [FAMILY_EDIT_PAGE] Physical back button pressed - calling CancelCommand");
                _isNavigating = true;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // ✅ Usar o comando Cancel da classe base que já tem toda a lógica
                    if (_viewModel.CancelCommand.CanExecute(null))
                    {
                        await _viewModel.CancelCommand.ExecuteAsync(null);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ [FAMILY_EDIT_PAGE] Back button handler error: {ex.Message}");
            }
            finally
            {
                _isNavigating = false;
            }
        });

        // Prevent default back button behavior
        return true;
    }

    #endregion

    #region ✅ Animations

    /// <summary>
    /// ✅ Animação de entrada mantendo o padrão do projeto
    /// </summary>
    private async Task PerformEntranceAnimation()
    {
        try
        {
            // Setup initial state
            Content.Opacity = 0;
            Content.Scale = 0.95;
            Content.TranslationY = 30;

            // Animate entrance
            await Task.WhenAll(
                Content.FadeTo(1, 600, Easing.CubicOut),
                Content.ScaleTo(1, 600, Easing.SpringOut),
                Content.TranslateTo(0, 0, 600, Easing.CubicOut)
            );

            Debug.WriteLine("✨ [FAMILY_EDIT_PAGE] Entrance animation completed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_PAGE] Entrance animation error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ Animação de saída mantendo o padrão do projeto
    /// </summary>
    private async Task PerformExitAnimation()
    {
        try
        {
            await Task.WhenAll(
                Content.FadeTo(0, 300, Easing.CubicIn),
                Content.ScaleTo(0.95, 300, Easing.CubicIn),
                Content.TranslateTo(0, -20, 300, Easing.CubicIn)
            );

            Debug.WriteLine("✨ [FAMILY_EDIT_PAGE] Exit animation completed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_PAGE] Exit animation error: {ex.Message}");
        }
    }

    #endregion
}