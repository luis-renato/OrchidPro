using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace OrchidPro.ViewModels;

/// <summary>
/// CORRIGIDO: ViewModel para lista de famílias com conectividade otimizada
/// </summary>
public partial class FamiliesListViewModel : BaseViewModel
{
    private readonly IFamilyRepository _familyRepository;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<FamilyItemViewModel> families = new();

    [ObservableProperty]
    private ObservableCollection<FamilyItemViewModel> selectedFamilies = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private bool isMultiSelectMode;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasData;

    [ObservableProperty]
    private string emptyStateMessage = "No families found";

    [ObservableProperty]
    private string statusFilter = "All";

    [ObservableProperty]
    private int totalCount;

    [ObservableProperty]
    private int activeCount;

    [ObservableProperty]
    private bool fabIsVisible = true;

    [ObservableProperty]
    private string fabIcon = "plus";

    [ObservableProperty]
    private string fabText = "Add Family";

    // ✅ CORRIGIDO: Conectividade inicializada otimisticamente
    [ObservableProperty]
    private string connectionStatus = "🌐 Connected";

    [ObservableProperty]
    private Color connectionStatusColor = Colors.Green;

    [ObservableProperty]
    private bool isConnected = true; // ✅ Inicia como true

    [ObservableProperty]
    private string cacheInfo = "Cache: Ready";

    // Apenas StatusFilterOptions (sem sync)
    public List<string> StatusFilterOptions { get; } = new() { "All", "Active", "Inactive" };

    public FamiliesListViewModel(IFamilyRepository familyRepository, INavigationService navigationService)
    {
        _familyRepository = familyRepository;
        _navigationService = navigationService;

        Title = "Families";

        // ✅ CORRIGIDO: Conectividade otimista
        IsConnected = true;
        ConnectionStatus = "🌐 Connected";
        ConnectionStatusColor = Colors.Green;

        Debug.WriteLine("✅ [FAMILIES_LIST_VM] Initialized with optimistic connectivity");
    }

    /// <summary>
    /// ✅ OTIMIZADO: Carrega famílias otimisticamente
    /// </summary>
    [RelayCommand]
    private async Task LoadFamiliesAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;

            Debug.WriteLine($"📥 [FAMILIES_LIST_VM] Loading families with filter: {StatusFilter}");

            // ✅ OTIMIZADO: Carregar dados primeiro, testar conectividade em background
            var loadTask = LoadFamiliesDataAsync();
            var connectivityTask = TestConnectionInBackgroundAsync();

            // Aguardar carregamento de dados
            await loadTask;

            // Conectividade roda em background
            _ = connectivityTask;

            Debug.WriteLine($"✅ [FAMILIES_LIST_VM] Loaded {Families.Count} families");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_VM] Load error: {ex.Message}");
            await ShowErrorAsync("Failed to load families", "Check your connection and try again.");
            UpdateConnectionStatus(false);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// ✅ NOVO: Carregamento otimizado de dados
    /// </summary>
    private async Task LoadFamiliesDataAsync()
    {
        try
        {
            // Parse filters
            bool? statusFilter = StatusFilter switch
            {
                "Active" => true,
                "Inactive" => false,
                _ => null
            };

            // Get filtered data (usa cache se servidor indisponível)
            var familyList = await _familyRepository.GetFilteredAsync(SearchText, statusFilter);

            // Convert to ViewModels
            var familyViewModels = familyList.Select(f => new FamilyItemViewModel(f)
            {
                IsSelected = SelectedFamilies.Any(sf => sf.Id == f.Id),
                SelectionChangedCommand = new RelayCommand<FamilyItemViewModel>(OnFamilySelectionChanged)
            }).ToList();

            // Update collections
            Families.Clear();
            foreach (var family in familyViewModels)
            {
                Families.Add(family);
            }

            // Update statistics
            await UpdateStatisticsAsync();

            HasData = Families.Count > 0;

            if (!HasData)
            {
                EmptyStateMessage = GetEmptyStateMessage();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_VM] Load data error: {ex.Message}");

            // Se falhar, marca como desconectado
            UpdateConnectionStatus(false);

            throw; // Re-throw para ser capturado pelo método pai
        }
    }

    /// <summary>
    /// ✅ NOVO: Teste de conectividade em background
    /// </summary>
    private async Task TestConnectionInBackgroundAsync()
    {
        try
        {
            await Task.Delay(100); // Deixa UI renderizar primeiro

            Debug.WriteLine("🔍 [FAMILIES_LIST_VM] Testing connection in background...");

            var connected = await _familyRepository.TestConnectionAsync();

            // Update cache info
            CacheInfo = _familyRepository.GetCacheInfo();

            // ✅ Só atualiza se realmente mudou
            if (IsConnected != connected)
            {
                UpdateConnectionStatus(connected);

                if (!connected)
                {
                    EmptyStateMessage = GetEmptyStateMessage();
                }
            }

            Debug.WriteLine($"🔍 [FAMILIES_LIST_VM] Background test result: {connected}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_VM] Background connectivity test failed: {ex.Message}");
            UpdateConnectionStatus(false);
        }
    }

    /// <summary>
    /// Refresh com force cache refresh
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsRefreshing) return;

        try
        {
            IsRefreshing = true;

            Debug.WriteLine("🔄 [FAMILIES_LIST_VM] Force refreshing cache from server...");

            // ✅ NOVO: Verificar conectividade antes de tentar refresh
            var isConnected = await _familyRepository.TestConnectionAsync();
            if (!isConnected)
            {
                UpdateConnectionStatus(false);
                await ShowErrorAsync("No Connection", "Cannot refresh data without internet connection.");
                return;
            }

            // Force refresh do cache
            await _familyRepository.RefreshCacheAsync();

            // Reload data
            await LoadFamiliesAsync();

            Debug.WriteLine("✅ [FAMILIES_LIST_VM] Refresh completed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_VM] Refresh error: {ex.Message}");

            if (ex.Message.Contains("connection") || ex.Message.Contains("internet"))
            {
                UpdateConnectionStatus(false);
                await ShowErrorAsync("Connection Error", "Failed to refresh data. Check your internet connection.");
            }
            else
            {
                await ShowErrorAsync("Refresh Failed", "Failed to refresh data from server.");
            }
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    /// <summary>
    /// ✅ CORRIGIDO: Testa conectividade manual
    /// </summary>
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        try
        {
            Debug.WriteLine("🔍 [FAMILIES_LIST_VM] Manual connection test...");

            var connected = await _familyRepository.TestConnectionAsync();
            UpdateConnectionStatus(connected);

            // Update cache info
            CacheInfo = _familyRepository.GetCacheInfo();

            Debug.WriteLine($"🔍 [FAMILIES_LIST_VM] Manual test result: {connected}");

            var message = connected ? "✅ Connected to server" : "❌ Connection failed";
            await ShowSuccessAsync(message);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_VM] Manual connection test failed: {ex.Message}");
            UpdateConnectionStatus(false);
            await ShowErrorAsync("Connection Test", "Connection test failed");
        }
    }

    /// <summary>
    /// Searches families based on text input
    /// </summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        Debug.WriteLine($"🔍 [FAMILIES_LIST_VM] Searching for: '{SearchText}'");
        await LoadFamiliesAsync();
    }

    /// <summary>
    /// Clears the search text
    /// </summary>
    [RelayCommand]
    private async Task ClearSearchAsync()
    {
        SearchText = string.Empty;
        await LoadFamiliesAsync();
    }

    /// <summary>
    /// ✅ CORRIGIDO: Navigates to add new family page
    /// </summary>
    [RelayCommand]
    private async Task AddFamilyAsync()
    {
        // ✅ NOVO: Verificar conectividade antes de navegar
        if (!IsConnected)
        {
            var canProceed = await ShowConfirmAsync(
                "Offline Mode",
                "You're currently offline. Some features may not work properly. Continue anyway?");

            if (!canProceed) return;
        }

        try
        {
            Debug.WriteLine("➕ [FAMILIES_LIST_VM] Navigating to add family");
            await _navigationService.NavigateToAsync("familyedit");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_VM] Navigation error: {ex.Message}");
            await ShowErrorAsync("Navigation Error", "Failed to open add family page");
        }
    }

    /// <summary>
    /// ✅ CORRIGIDO: Navigates to edit family page
    /// </summary>
    [RelayCommand]
    private async Task EditFamilyAsync(FamilyItemViewModel? family)
    {
        if (family == null) return;

        // ✅ NOVO: Verificar conectividade antes de editar
        if (!IsConnected)
        {
            var canProceed = await ShowConfirmAsync(
                "Offline Mode",
                "You're currently offline. Editing may not work properly. Continue anyway?");

            if (!canProceed) return;
        }

        try
        {
            Debug.WriteLine($"📝 [FAMILIES_LIST_VM] Navigating to edit family: {family.Name}");

            var parameters = new Dictionary<string, object>
            {
                ["FamilyId"] = family.Id
            };

            await _navigationService.NavigateToAsync("familyedit", parameters);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_VM] Edit navigation error: {ex.Message}");
            await ShowErrorAsync("Navigation Error", "Failed to open edit family page");
        }
    }

    /// <summary>
    /// ✅ CORRIGIDO: Deletes selected families
    /// </summary>
    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (!SelectedFamilies.Any()) return;

        // ✅ NOVO: Verificar conectividade antes de deletar
        if (!IsConnected)
        {
            await ShowErrorAsync("No Connection", "Cannot delete families without internet connection.");
            return;
        }

        try
        {
            var selectedIds = SelectedFamilies.Select(f => f.Id).ToList();
            var count = selectedIds.Count;

            var confirmed = await ShowConfirmAsync(
                "Delete Families",
                $"Are you sure you want to delete {count} {(count == 1 ? "family" : "families")}?");

            if (!confirmed) return;

            IsLoading = true;

            Debug.WriteLine($"🗑️ [FAMILIES_LIST_VM] Deleting {count} families");

            var deletedCount = await _familyRepository.DeleteMultipleAsync(selectedIds);

            if (deletedCount > 0)
            {
                await ShowSuccessAsync($"Successfully deleted {deletedCount} {(deletedCount == 1 ? "family" : "families")}");
                ExitMultiSelectMode();
                await LoadFamiliesAsync();

                Debug.WriteLine($"✅ [FAMILIES_LIST_VM] Deleted {deletedCount} families");
            }
            else
            {
                await ShowErrorAsync("Delete Failed", "No families were deleted");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_VM] Delete error: {ex.Message}");

            if (ex.Message.Contains("connection") || ex.Message.Contains("internet"))
            {
                UpdateConnectionStatus(false);
                await ShowErrorAsync("Connection Error", "Failed to delete families. Check your internet connection.");
            }
            else
            {
                await ShowErrorAsync("Delete Error", "Failed to delete families. Please try again.");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Toggles multi-select mode
    /// </summary>
    [RelayCommand]
    private void ToggleMultiSelect()
    {
        if (IsMultiSelectMode)
        {
            ExitMultiSelectMode();
        }
        else
        {
            EnterMultiSelectMode();
        }
    }

    /// <summary>
    /// Selects all visible families
    /// </summary>
    [RelayCommand]
    private void SelectAll()
    {
        foreach (var family in Families)
        {
            if (!family.IsSelected)
            {
                family.IsSelected = true;
                SelectedFamilies.Add(family);
            }
        }
        UpdateFabForSelection();
    }

    /// <summary>
    /// Deselects all families
    /// </summary>
    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var family in Families)
        {
            family.IsSelected = false;
        }
        SelectedFamilies.Clear();
        UpdateFabForSelection();
    }

    /// <summary>
    /// Handles family selection change
    /// </summary>
    private void OnFamilySelectionChanged(FamilyItemViewModel? family)
    {
        if (family == null) return;

        if (family.IsSelected)
        {
            if (!SelectedFamilies.Contains(family))
            {
                SelectedFamilies.Add(family);
            }

            if (!IsMultiSelectMode)
            {
                EnterMultiSelectMode();
            }
        }
        else
        {
            SelectedFamilies.Remove(family);

            if (!SelectedFamilies.Any() && IsMultiSelectMode)
            {
                ExitMultiSelectMode();
            }
        }

        UpdateFabForSelection();
    }

    /// <summary>
    /// Enters multi-select mode
    /// </summary>
    private void EnterMultiSelectMode()
    {
        IsMultiSelectMode = true;
        UpdateFabForSelection();
        Debug.WriteLine("✅ [FAMILIES_LIST_VM] Entered multi-select mode");
    }

    /// <summary>
    /// Exits multi-select mode
    /// </summary>
    private void ExitMultiSelectMode()
    {
        IsMultiSelectMode = false;
        DeselectAll();
        FabIcon = "plus";
        FabText = "Add Family";
        FabIsVisible = true;
        Debug.WriteLine("✅ [FAMILIES_LIST_VM] Exited multi-select mode");
    }

    /// <summary>
    /// ✅ CORRIGIDO: Updates FAB based on selection and connectivity
    /// </summary>
    private void UpdateFabForSelection()
    {
        if (SelectedFamilies.Any())
        {
            FabIcon = "delete";
            FabText = $"Delete ({SelectedFamilies.Count})";
            // ✅ Disable delete if offline
            FabIsVisible = IsConnected;
        }
        else if (IsMultiSelectMode)
        {
            FabIcon = "close";
            FabText = "Cancel";
            FabIsVisible = true;
        }
        else
        {
            FabIcon = "plus";
            FabText = IsConnected ? "Add Family" : "Offline";
            FabIsVisible = true;
        }
    }

    /// <summary>
    /// Updates statistics display
    /// </summary>
    private async Task UpdateStatisticsAsync()
    {
        try
        {
            var stats = await _familyRepository.GetStatisticsAsync();
            TotalCount = stats.TotalCount;
            ActiveCount = stats.ActiveCount;

            Debug.WriteLine($"📊 [FAMILIES_LIST_VM] Stats updated - Total: {TotalCount}, Active: {ActiveCount}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_LIST_VM] Stats update error: {ex.Message}");
        }
    }

    /// <summary>
    /// ✅ CORRIGIDO: Atualiza status de conectividade
    /// </summary>
    private void UpdateConnectionStatus(bool connected)
    {
        IsConnected = connected;

        if (connected)
        {
            ConnectionStatus = "🌐 Connected";
            ConnectionStatusColor = Colors.Green;
        }
        else
        {
            ConnectionStatus = "📡 Disconnected";
            ConnectionStatusColor = Colors.Red;
        }

        // ✅ NOVO: Atualizar FAB baseado em conectividade
        UpdateFabForSelection();
    }

    /// <summary>
    /// ✅ CORRIGIDO: Gets appropriate empty state message
    /// </summary>
    private string GetEmptyStateMessage()
    {
        if (!IsConnected)
        {
            return "No internet connection\nConnect to view your families";
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            return $"No families found matching '{SearchText}'";
        }

        if (StatusFilter != "All")
        {
            return $"No {StatusFilter.ToLower()} families found";
        }

        return "No families yet. Add your first family to get started!";
    }

    /// <summary>
    /// ✅ CORRIGIDO: Handles page appearing
    /// </summary>
    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();

        Debug.WriteLine("👁️ [FAMILIES_LIST_VM] Page appearing - loading data optimistically");
        await LoadFamiliesAsync();
    }

    /// <summary>
    /// Handles search text changes with debouncing
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        // Debounce search to avoid too many API calls
        _ = Task.Run(async () =>
        {
            await Task.Delay(300);
            if (SearchText == value) // Still the same search text
            {
                await SearchAsync();
            }
        });
    }

    /// <summary>
    /// Handles status filter changes
    /// </summary>
    partial void OnStatusFilterChanged(string value)
    {
        Debug.WriteLine($"🔽 [FAMILIES_LIST_VM] Status filter changed to: {value}");
        _ = LoadFamiliesAsync();
    }
}