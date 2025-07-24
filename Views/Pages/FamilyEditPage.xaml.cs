using OrchidPro.ViewModels.Families;
using System.Diagnostics;

namespace OrchidPro.Views.Pages;

/// <summary>
/// ✅ FINAL: FamilyEditPage sem erros de compilação
/// </summary>
public partial class FamilyEditPage : ContentPage, IQueryAttributable
{
    private readonly FamilyEditViewModel _viewModel;

    public FamilyEditPage(FamilyEditViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = _viewModel;

        // InitializeComponent com tratamento de erro
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

    protected override bool OnBackButtonPressed()
    {
        // Handle back button with unsaved changes check
        _ = Task.Run(async () =>
        {
            try
            {
                // Verifica mudanças não salvas
                if (_viewModel.HasUnsavedChanges)
                {
                    var result = await DisplayAlert(
                        "Unsaved Changes",
                        "You have unsaved changes. Discard them?",
                        "Discard",
                        "Cancel");

                    if (result)
                    {
                        // User chose to discard changes
                        Device.BeginInvokeOnMainThread(async () =>
                        {
                            await Shell.Current.GoToAsync("..");
                        });
                    }
                    // If false, stay on page
                }
                else
                {
                    // No unsaved changes, navigate back normally
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        await Shell.Current.GoToAsync("..");
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ [FAMILY_EDIT_PAGE] Back button handler error: {ex.Message}");
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