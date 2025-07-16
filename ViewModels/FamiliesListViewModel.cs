using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace OrchidPro.ViewModels;

/// <summary>
/// CORRIGIDO: FamiliesListViewModel - lista de famílias botânicas
/// </summary>
public class FamiliesListViewModel : BaseListViewModel<Family, FamilyItemViewModel>
{
    public override string EntityName => "Family";
    public override string EntityNamePlural => "Families";
    public override string EditRoute => "familyedit";

    public FamiliesListViewModel(IFamilyRepository familyRepository, INavigationService navigationService)
        : base(familyRepository, navigationService)
    {
        Debug.WriteLine("✅ [FAMILIES_LIST_VM] Using BaseListViewModel");
    }

    /// <summary>
    /// IMPLEMENTAÇÃO OBRIGATÓRIA: Cria FamilyItemViewModel
    /// </summary>
    protected override FamilyItemViewModel CreateItemViewModel(Family entity)
    {
        return new FamilyItemViewModel(entity);
    }

    // ✅ COMPATIBILIDADE COM UI XAML:
    /// <summary>
    /// Propriedade para acessar famílias (compatibilidade com UI)
    /// </summary>
    public new ObservableCollection<FamilyItemViewModel> Families => Items;

    /// <summary>
    /// Propriedade para famílias selecionadas (compatibilidade com UI)
    /// </summary>
    public new ObservableCollection<FamilyItemViewModel> SelectedFamilies => SelectedItems;

    // ✅ FUNCIONALIDADES ESPECÍFICAS DE FAMILIES:

    /// <summary>
    /// Filtro específico: mostrar apenas famílias de orquídeas
    /// </summary>
    public async Task FilterOrchidFamiliesAsync()
    {
        SearchText = "Orchidaceae";
        await SearchCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Estatística específica: quantas famílias de orquídeas
    /// </summary>
    public int OrchidFamiliesCount => Items.Count(f => f.IsOrchidaceae);

    /// <summary>
    /// Ação específica: importar famílias padrão do sistema
    /// </summary>
    public async Task ImportSystemFamiliesAsync()
    {
        try
        {
            if (!IsConnected)
            {
                await ShowErrorAsync("No Connection", "Cannot import system families without internet connection.");
                return;
            }

            IsLoading = true;

            Debug.WriteLine("📥 [FAMILIES_LIST_VM] Importing system families...");

            // Force refresh para garantir que temos dados do sistema
            await _repository.RefreshCacheAsync();
            await LoadItemsCommand.ExecuteAsync(null);

            var systemFamilies = Items.Where(f => f.IsSystemDefault).ToList();

            if (systemFamilies.Any())
            {
                await ShowSuccessAsync($"Found {systemFamilies.Count} system botanical families");
            }
            else
            {
                await ShowErrorAsync("No System Families", "No system default families found in database.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_VM] Import system families error: {ex.Message}");
            await ShowErrorAsync("Import Error", "Failed to import system families.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Override do título para incluir contador (opcional)
    /// </summary>
    public string TitleWithCount => $"Botanical Families ({TotalCount})";

    /// <summary>
    /// Propriedade específica: famílias mais populares
    /// </summary>
    public List<FamilyItemViewModel> PopularFamilies =>
        Items.Where(f => f.IsOrchidaceae || f.Name.Contains("Bromeliac") || f.Name.Contains("Arac"))
             .ToList();
}