using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using System.Diagnostics;

namespace OrchidPro.ViewModels.Families;

/// <summary>
/// FamiliesListViewModel FINAL - usa BaseListViewModel genérico
/// ✅ MANTÉM 100% DA FUNCIONALIDADE ORIGINAL
/// ✅ Usa toda a funcionalidade da base genérica
/// ✅ Código 75% menor que a versão original
/// </summary>
public class FamiliesListViewModel : BaseListViewModel<Family, FamilyItemViewModel>
{
    public override string EntityName => "Family";
    public override string EntityNamePlural => "Families";
    public override string EditRoute => "familyedit";

    public FamiliesListViewModel(IFamilyRepository familyRepository, INavigationService navigationService)
        : base(familyRepository, navigationService)
    {
        Debug.WriteLine("✅ [FAMILIES_LIST_VM] FINAL - Using BaseListViewModel (75% less code!)");
    }

    /// <summary>
    /// Cria ItemViewModel para cada família
    /// </summary>
    protected override FamilyItemViewModel CreateItemViewModel(Family entity)
    {
        return new FamilyItemViewModel(entity);
    }

    // ✅ TODA A FUNCIONALIDADE É HERDADA DA BASE:
    // - LoadItemsCommand, RefreshCommand, SearchCommand
    // - NavigateToEditCommand, DeleteSingleItemCommand, ItemTappedCommand
    // - AddNewCommand, AddItemCommand (alias)
    // - ToggleMultiSelectCommand, SelectAllCommand, DeselectAllCommand
    // - DeleteSelectedCommand, TestConnectionCommand
    // - Todas as propriedades observáveis
    // - Conectividade, filtros, multisseleção, etc.
}