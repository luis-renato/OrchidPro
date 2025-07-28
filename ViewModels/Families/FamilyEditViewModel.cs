using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using System.Diagnostics;

namespace OrchidPro.ViewModels.Families;

/// <summary>
/// ✅ CORRIGIDO: FamilyEditViewModel com commands e título funcionais
/// </summary>
public partial class FamilyEditViewModel : BaseEditViewModel<Family>, IQueryAttributable
{
    #region ✅ Overrides obrigatórios

    public override string EntityName => "Family";
    public override string EntityNamePlural => "Families";

    #endregion

    #region ✅ CORRIGIDO: Page Title dinâmico

    /// <summary>
    /// ✅ CORRIGIDO: Page title que realmente muda baseado no modo
    /// </summary>
    public new string PageTitle => IsEditMode ? "Edit Family" : "New Family";

    /// <summary>
    /// ✅ CORRIGIDO: Propriedade IsEditMode que notifica mudanças
    /// </summary>
    public bool IsEditMode => _isEditMode;

    #endregion

    #region Constructor

    public FamilyEditViewModel(IFamilyRepository familyRepository, INavigationService navigationService)
        : base(familyRepository, navigationService)
    {
        Debug.WriteLine("✅ [FAMILY_EDIT_VM] Initialized - using base functionality with corrections");
    }

    #endregion

    #region ✅ CORRIGIDO: Sobrescrever ApplyQueryAttributes para setar IsEditMode

    /// <summary>
    /// ✅ CORRIGIDO: Aplica parâmetros e seta modo de edição corretamente
    /// </summary>
    public new void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        // Chamar o base primeiro
        base.ApplyQueryAttributes(query);

        // Notificar mudança no título
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(IsEditMode));

        Debug.WriteLine($"✅ [FAMILY_EDIT_VM] IsEditMode: {IsEditMode}, PageTitle: {PageTitle}");
    }

    #endregion

    // ✅ FUNCIONALIDADES AGORA NA BASE:
    // ✅ SaveCommand - funciona automaticamente via base
    // ✅ CancelCommand - funciona automaticamente via base  
    // ✅ IsFavorite toggle - funciona automaticamente
    // ✅ Validação de nome único - funciona automaticamente
    // ✅ Progress calculation - funciona automaticamente
    // ✅ Loading states + toasts - funcionam automaticamente
    // ✅ Navigation + interceptação - funciona automaticamente

    // ✅ RESULTADO: Family deve salvar/atualizar normalmente agora
}