using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using Syncfusion.Maui.DataSource;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace OrchidPro.ViewModels.Families;

/// <summary>
/// ✅ CORRIGIDO: FamiliesListSyncfusionViewModel com favoritos e comandos completos
/// </summary>
public partial class FamiliesListSyncfusionViewModel : BaseViewModel
{
    private readonly IFamilyRepository _repository;
    private readonly INavigationService _navigationService;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<FamilyItemViewModel> items = new();

    [ObservableProperty]
    private ObservableCollection<FamilyItemViewModel> selectedItems = new();

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
    private int favoriteCount;

    [ObservableProperty]
    private bool fabIsVisible = true;

    [ObservableProperty]
    private string fabText = "Add Family";

    [ObservableProperty]
    private string connectionStatus = "Connected";

    [ObservableProperty]
    private Color connectionStatusColor = Colors.Green;

    [ObservableProperty]
    private bool isConnected = true;

    [ObservableProperty]
    private DataSource dataSource;

    [ObservableProperty]
    private string sortOrder = "Name A→Z";

    #endregion

    #region Filter Options

    public List<string> StatusFilterOptions { get; } = new() { "All", "Active", "Inactive" };

    public List<string> SortOptions { get; } = new()
    {
        "Name A→Z",
        "Name Z→A",
        "Recent First",
        "Oldest First",
        "Favorites First"
    };

    #endregion

    public FamiliesListSyncfusionViewModel(IFamilyRepository repository, INavigationService navigationService)
    {
        _repository = repository;
        _navigationService = navigationService;

        Title = "Families";

        // Initialize DataSource for Syncfusion
        DataSource = new DataSource();
        DataSource.Source = Items;

        Debug.WriteLine("✅ [FAMILIES_SYNCFUSION_VM] Initialized with optimized features");
    }

    #region Lifecycle

    public async Task InitializeAsync()
    {
        try
        {
            Debug.WriteLine("🚀 [FAMILIES_SYNCFUSION_VM] Initializing...");

            await LoadDataAsync();
            await UpdateConnectionStatusAsync();

            Debug.WriteLine("✅ [FAMILIES_SYNCFUSION_VM] Initialization completed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Initialization error: {ex.Message}");
        }
    }

    #endregion

    #region Data Loading

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            Debug.WriteLine("🔄 [FAMILIES_SYNCFUSION_VM] Refreshing data...");

            IsRefreshing = true;
            await LoadDataAsync();
            await UpdateConnectionStatusAsync();

            Debug.WriteLine("✅ [FAMILIES_SYNCFUSION_VM] Refresh completed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Refresh error: {ex.Message}");
            await ShowErrorAsync("Refresh Error", ex.Message);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;

            Debug.WriteLine("📥 [FAMILIES_SYNCFUSION_VM] Loading families...");

            // Get families with filters
            var families = await GetFilteredFamiliesAsync();

            Debug.WriteLine($"📊 [FAMILIES_SYNCFUSION_VM] Retrieved {families.Count} families from repository");

            // Convert to ViewModels
            var familyViewModels = families.Select(f => new FamilyItemViewModel(f)).ToList();

            // Apply sorting
            familyViewModels = ApplySorting(familyViewModels);

            // ✅ CORREÇÃO CRÍTICA: Fazer update na UI thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                // Clear and populate Items
                Items.Clear();

                foreach (var item in familyViewModels)
                {
                    Items.Add(item);
                    Debug.WriteLine($"📝 [FAMILIES_SYNCFUSION_VM] Added family to UI: {item.Name}");
                }

                Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] UI updated with {Items.Count} families");
            });

            // Update statistics
            UpdateStatistics(families);

            HasData = Items.Count > 0;
            UpdateEmptyStateMessage();

            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Loaded {Items.Count} families in Items collection");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Load data error: {ex.Message}");
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<List<Family>> GetFilteredFamiliesAsync()
    {
        try
        {
            var families = await _repository.GetAllAsync(true); // Include inactive

            // Apply text search
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLowerInvariant();
                families = families.Where(f =>
                    f.Name.ToLowerInvariant().Contains(searchLower) ||
                    (!string.IsNullOrEmpty(f.Description) && f.Description.ToLowerInvariant().Contains(searchLower))
                ).ToList();
            }

            // Apply status filter
            families = StatusFilter switch
            {
                "Active" => families.Where(f => f.IsActive).ToList(),
                "Inactive" => families.Where(f => !f.IsActive).ToList(),
                _ => families
            };

            return families;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Filter error: {ex.Message}");
            return new List<Family>();
        }
    }

    private List<FamilyItemViewModel> ApplySorting(List<FamilyItemViewModel> families)
    {
        return SortOrder switch
        {
            "Name Z→A" => families.OrderByDescending(f => f.Name).ToList(),
            "Recent First" => families.OrderByDescending(f => f.CreatedAt).ToList(),
            "Oldest First" => families.OrderBy(f => f.CreatedAt).ToList(),
            "Favorites First" => families.OrderByDescending(f => f.IsFavorite).ThenBy(f => f.Name).ToList(),
            _ => families.OrderBy(f => f.Name).ToList() // Default: Name A→Z
        };
    }

    private void UpdateStatistics(List<Family> families)
    {
        TotalCount = families.Count;
        ActiveCount = families.Count(f => f.IsActive);
        FavoriteCount = families.Count(f => f.IsFavorite);

        Debug.WriteLine($"📊 [FAMILIES_SYNCFUSION_VM] Stats - Total: {TotalCount}, Active: {ActiveCount}, Favorites: {FavoriteCount}");
    }

    private void UpdateEmptyStateMessage()
    {
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            EmptyStateMessage = $"No families found for '{SearchText}'";
        }
        else if (StatusFilter != "All")
        {
            EmptyStateMessage = $"No {StatusFilter.ToLower()} families found";
        }
        else
        {
            EmptyStateMessage = "No families available";
        }
    }

    #endregion

    #region Search and Filter Commands

    [RelayCommand]
    private async Task ApplyFilterAsync()
    {
        try
        {
            Debug.WriteLine($"🔍 [FAMILIES_SYNCFUSION_VM] Applying filter - Search: '{SearchText}', Status: {StatusFilter}");
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Filter error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ClearSearchAsync()
    {
        try
        {
            SearchText = string.Empty;
            await ApplyFilterAsync();
            Debug.WriteLine("🗑️ [FAMILIES_SYNCFUSION_VM] Search cleared");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Clear search error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ToggleSortAsync()
    {
        try
        {
            Debug.WriteLine($"🔄 [FAMILIES_SYNCFUSION_VM] Sorting by: {SortOrder}");
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Sort error: {ex.Message}");
        }
    }

    #endregion

    #region Navigation Commands

    [RelayCommand]
    private async Task AddNewAsync()
    {
        try
        {
            Debug.WriteLine("➕ [FAMILIES_SYNCFUSION_VM] Navigating to add new family");
            await _navigationService.NavigateToAsync("familyedit");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Add new navigation error: {ex.Message}");
            await ShowErrorAsync("Navigation Error", "Failed to open add family page");
        }
    }

    [RelayCommand]
    private async Task EditItemAsync(object parameter)
    {
        try
        {
            if (parameter is not FamilyItemViewModel item)
            {
                Debug.WriteLine("❌ [FAMILIES_SYNCFUSION_VM] Invalid item for edit");
                return;
            }

            Debug.WriteLine($"✏️ [FAMILIES_SYNCFUSION_VM] Navigating to edit family: {item.Name} (ID: {item.Id})");

            var parameters = new Dictionary<string, object>
            {
                { "FamilyId", item.Id.ToString() }
            };

            await _navigationService.NavigateToAsync("familyedit", parameters);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Edit navigation error: {ex.Message}");
            await ShowErrorAsync("Navigation Error", "Failed to open edit family page");
        }
    }

    /// <summary>
    /// ✅ NOVO: Command para navegação direta do item tapped
    /// </summary>
    [RelayCommand]
    private async Task NavigateToEditAsync(FamilyItemViewModel item)
    {
        await EditItemAsync(item);
    }

    #endregion

    #region Multi-Selection Commands

    [RelayCommand]
    private void ToggleMultiSelect()
    {
        try
        {
            IsMultiSelectMode = !IsMultiSelectMode;
            Debug.WriteLine($"🔘 [FAMILIES_SYNCFUSION_VM] Multi-select mode: {IsMultiSelectMode}");

            if (!IsMultiSelectMode)
            {
                // Clear selections
                SelectedItems.Clear();
                foreach (var item in Items)
                {
                    item.IsSelected = false;
                }
            }

            UpdateFabForSelection();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Toggle multi-select error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteSingleAsync(FamilyItemViewModel item)
    {
        try
        {
            if (item.IsSystemDefault)
            {
                var toast = Toast.Make("System families cannot be deleted", ToastDuration.Short, 14);
                await toast.Show();
                return;
            }

            Debug.WriteLine($"🗑️ [FAMILIES_SYNCFUSION_VM] Deleting family: {item.Name}");

            await _repository.DeleteAsync(item.Id);
            Items.Remove(item);
            
            var successToast = Toast.Make($"Family '{item.Name}' deleted", ToastDuration.Short, 14);
            await successToast.Show();

            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Family deleted: {item.Name}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Delete error: {ex.Message}");
            await ShowErrorAsync("Delete Error", ex.Message);
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(FamilyItemViewModel item)
    {
        try
        {
            Debug.WriteLine($"⭐ [FAMILIES_SYNCFUSION_VM] Toggling favorite for: {item.Name}");

            // Cast para acessar o método específico do FamilyRepository
            if (_repository is FamilyRepository familyRepo)
            {
                var updatedFamily = await familyRepo.ToggleFavoriteAsync(item.Id);
                
                // Find and update the item in the list
                var existingItem = Items.FirstOrDefault(i => i.Id == item.Id);
                if (existingItem != null)
                {
                    var index = Items.IndexOf(existingItem);
                    Items[index] = new FamilyItemViewModel(updatedFamily);
                }

                var message = updatedFamily.IsFavorite ? "Added to favorites" : "Removed from favorites";
                var toast = Toast.Make(message, ToastDuration.Short, 14);
                await toast.Show();

                Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Favorite toggled: {item.Name} -> {updatedFamily.IsFavorite}");
            }
            else
            {
                throw new InvalidOperationException("Repository does not support favorite operations");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Toggle favorite error: {ex.Message}");
            await ShowErrorAsync("Favorite Error", ex.Message);
        }
    }

    #endregion

    #region Connection Status

    private async Task UpdateConnectionStatusAsync()
    {
        try
        {
            var isConnected = await _repository.TestConnectionAsync();

            IsConnected = isConnected;
            ConnectionStatus = isConnected ? "Connected" : "Offline";
            ConnectionStatusColor = isConnected ? Colors.Green : Colors.Orange;

            Debug.WriteLine($"📡 [FAMILIES_SYNCFUSION_VM] Connection status: {ConnectionStatus}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Connection test error: {ex.Message}");

            IsConnected = false;
            ConnectionStatus = "Error";
            ConnectionStatusColor = Colors.Red;
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        try
        {
            Debug.WriteLine("🧪 [FAMILIES_SYNCFUSION_VM] Testing connection...");
            await UpdateConnectionStatusAsync();

            var message = IsConnected ? "Connection successful!" : "Connection failed. Check your internet.";
            await ShowSuccessAsync("Connection Test", message);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Connection test error: {ex.Message}");
            await ShowErrorAsync("Connection Error", ex.Message);
        }
    }

    #endregion

    #region Helper Methods

    public void UpdateFabForSelection()
    {
        var selectedCount = SelectedItems.Count;

        if (selectedCount > 0)
        {
            FabText = $"Delete ({selectedCount})";
            FabIsVisible = IsConnected;
        }
        else if (IsMultiSelectMode)
        {
            FabText = "Cancel";
            FabIsVisible = true;
        }
        else
        {
            FabText = IsConnected ? "Add Family" : "Offline";
            FabIsVisible = true;
        }
    }

    private async Task ShowSuccessAsync(string title, string message)
    {
        try
        {
            var toast = Toast.Make(message, ToastDuration.Short, 14);
            await toast.Show();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Show success error: {ex.Message}");
        }
    }

    private async Task ShowErrorAsync(string title, string message)
    {
        try
        {
            var toast = Toast.Make($"{title}: {message}", ToastDuration.Long, 14);
            await toast.Show();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Show error error: {ex.Message}");
        }
    }

    #endregion
}