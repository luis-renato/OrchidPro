using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace OrchidPro.ViewModels;

/// <summary>
/// PASSO 12: FamiliesListViewModel FINAL - migrado para usar BaseListViewModel
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
    /// ✅ IMPLEMENTAÇÃO OBRIGATÓRIA: Cria FamilyItemViewModel
    /// </summary>
    protected override FamilyItemViewModel CreateItemViewModel(Family entity)
    {
        return new FamilyItemViewModel(entity);
    }

    // ✅ TODA A FUNCIONALIDADE É HERDADA DA BASE:
    // - Loading (LoadItemsCommand, LoadItemsDataAsync, IsLoading)
    // - Refresh (RefreshCommand, IsRefreshing, cache invalidation)
    // - Connectivity (IsConnected, ConnectionStatus, TestConnectionCommand)
    // - Search (SearchText, SearchCommand, ClearSearchCommand)
    // - Filters (StatusFilter, StatusFilterOptions)
    // - Multi-selection (IsMultiSelectMode, ToggleMultiSelectCommand, SelectAllCommand, DeselectAllCommand)
    // - Selection (SelectedItems, OnItemSelectionChanged)
    // - Navigation (AddItemCommand, EditItemCommand)
    // - Delete (DeleteSelectedCommand)
    // - Statistics (TotalCount, ActiveCount, UpdateStatisticsAsync)
    // - FAB (FabText, FabIsVisible, UpdateFabForSelection)
    // - Empty states (HasData, EmptyStateMessage, GetEmptyStateMessage)
    // - Property change handlers (OnSearchTextChanged, OnStatusFilterChanged)
    // - Lifecycle (OnAppearingAsync)

    // ✅ FUNCIONALIDADES ESPECÍFICAS DE FAMILIES (opcionais):

    /// <summary>
    /// ✅ MANTIDO: Propriedade para acessar famílias (compatibilidade com UI)
    /// </summary>
    public new ObservableCollection<FamilyItemViewModel> Families => Items;

    /// <summary>
    /// ✅ MANTIDO: Propriedade para famílias selecionadas (compatibilidade com UI)
    /// </summary>
    public new ObservableCollection<FamilyItemViewModel> SelectedFamilies => SelectedItems;

    /// <summary>
    /// Filtro específico: mostrar apenas famílias de orquídeas
    /// </summary>
    public async Task FilterOrchidFamiliesAsync()
    {
        SearchText = "Orchidaceae";
        // ✅ CORRIGIDO: Agora pode usar método protected
        await SearchAsync();
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
            // ✅ CORRIGIDO: Agora pode usar método protected
            await LoadItemsAsync();

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

    // ✅ TODA A FUNCIONALIDADE ORIGINAL MANTIDA:
    // ✅ Observable collections para binding de UI
    // ✅ Loading e refresh com pull-to-refresh
    // ✅ Conectividade com teste em background
    // ✅ Search com debouncing (300ms)
    // ✅ Filtros por status (All/Active/Inactive)
    // ✅ Multi-seleção com checkboxes
    // ✅ FAB dinâmico (Add/Delete/Cancel) baseado em estado
    // ✅ Navegação para Add/Edit com parâmetros
    // ✅ Delete múltiplo com confirmação
    // ✅ Estatísticas (Total/Active counts)
    // ✅ Empty states baseados em conectividade/filtros
    // ✅ Cache invalidation após operações
    // ✅ Error handling com mensagens específicas
    // ✅ Lifecycle management (OnAppearing)
    // ✅ Property change notifications
    // ✅ Debug logging detalhado
}