using OrchidPro.ViewModels.Families;
using System.Diagnostics;

namespace OrchidPro.Views.Pages;

/// <summary>
/// ✅ CORRIGIDO: FamilyEditPage com inicialização correta e sincronização de título
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

            // ✅ CORREÇÃO CRÍTICA: Sincronizar título da página com o ViewModel
            SynchronizePageTitle();

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

            // ✅ CORREÇÃO CRÍTICA: Garantir que a inicialização aconteça
            if (_viewModel.IsEditMode && _viewModel.CurrentFamilyId.HasValue)
            {
                Debug.WriteLine($"🔄 [FAMILY_EDIT_PAGE] Triggering initialization for edit mode - ID: {_viewModel.CurrentFamilyId}");

                // Força a inicialização se ainda não aconteceu
                await _viewModel.OnAppearingAsync();
            }

            // ✅ CORREÇÃO CRÍTICA: Sincronizar título após possível carregamento de dados
            SynchronizePageTitle();

            Debug.WriteLine($"✅ [FAMILY_EDIT_PAGE] OnAppearing completed - Title: '{Title}', ViewModel Title: '{_viewModel.Title}'");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_PAGE] OnAppearing error: {ex.Message}");
        }
    }

    protected override async void OnDisappearing()
    {
        try
        {
            Debug.WriteLine($"👋 [FAMILY_EDIT_PAGE] OnDisappearing - cleaning up resources");

            await _viewModel.OnDisappearingAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_PAGE] OnDisappearing error: {ex.Message}");
        }
        finally
        {
            base.OnDisappearing();
        }
    }

    #endregion

    #region ✅ NOVO: Sincronização de Título

    /// <summary>
    /// ✅ CORRIGIDO: Sincroniza o título da página com o ViewModel
    /// </summary>
    private void SynchronizePageTitle()
    {
        try
        {
            // ✅ CORREÇÃO CRÍTICA: Usar o título correto baseado no modo
            var newTitle = _viewModel.IsEditMode ? "Edit Family" : "New Family";

            if (Title != newTitle)
            {
                Title = newTitle;
                Debug.WriteLine($"🔄 [FAMILY_EDIT_PAGE] Title synchronized: '{Title}'");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILY_EDIT_PAGE] SynchronizePageTitle error: {ex.Message}");
        }
    }

    #endregion
}