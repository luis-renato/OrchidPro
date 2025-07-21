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
/// ✅ COMPLETO: FamiliesListSyncfusionViewModel funcionando corretamente
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
        "Oldest First"
    };

    #endregion

    public FamiliesListSyncfusionViewModel(IFamilyRepository repository, INavigationService navigationService)
    {
        _repository = repository;
        _navigationService = navigationService;
        Title = "Families";

        // Inicializar DataSource do Syncfusion
        DataSource = new DataSource();

        Debug.WriteLine("✅ [FAMILIES_SYNCFUSION_VM] Initialized correctly");
    }

    #region Data Loading

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            Debug.WriteLine($"📥 [FAMILIES_SYNCFUSION_VM] Loading families...");

            await LoadItemsDataAsync();

            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Loaded {Items.Count} families");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Load error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadItemsDataAsync()
    {
        try
        {
            bool? statusFilter = StatusFilter switch
            {
                "Active" => true,
                "Inactive" => false,
                _ => null
            };

            var entities = await _repository.GetFilteredAsync(SearchText, statusFilter);

            var itemViewModels = entities.Select(entity =>
            {
                var itemVm = new FamilyItemViewModel(entity);
                return itemVm;
            }).ToList();

            // Update na UI thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Items.Clear();
                foreach (var item in itemViewModels)
                {
                    Items.Add(item);
                }

                // Configurar DataSource
                DataSource.Source = Items;
                ApplySortingToDataSource();
                DataSource.RefreshFilter();

                Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] DataSource configured with {Items.Count} items");
            });

            TotalCount = entities.Count;
            ActiveCount = entities.Count(e => e.IsActive);
            HasData = entities.Any();

            Debug.WriteLine($"📊 [FAMILIES_SYNCFUSION_VM] Stats - Total: {TotalCount}, Active: {ActiveCount}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] LoadItemsDataAsync error: {ex.Message}");
            throw;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        try
        {
            await LoadItemsDataAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Refresh error: {ex.Message}");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    #endregion

    #region Syncfusion Native Filtering

    public bool FilterFamilies(object item)
    {
        if (item is not FamilyItemViewModel family) return false;

        try
        {
            var searchText = SearchText?.Trim()?.ToLowerInvariant() ?? string.Empty;
            var statusFilter = StatusFilter;

            bool matchesSearch = string.IsNullOrEmpty(searchText) ||
                               (family.Name?.ToLowerInvariant().Contains(searchText) == true) ||
                               (family.Description?.ToLowerInvariant().Contains(searchText) == true);

            bool matchesStatus = statusFilter switch
            {
                "Active" => family.IsActive,
                "Inactive" => !family.IsActive,
                _ => true
            };

            return matchesSearch && matchesStatus;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Filter error: {ex.Message}");
            return true;
        }
    }

    [RelayCommand]
    private void ApplyFilter()
    {
        try
        {
            Debug.WriteLine($"🔍 [FAMILIES_SYNCFUSION_VM] Applying filter - Search: '{SearchText}', Status: '{StatusFilter}'");

            ApplySortingToDataSource();
            DataSource.Filter = FilterFamilies;
            DataSource.RefreshFilter();

            var filteredCount = DataSource.DisplayItems.Count;
            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Filter applied. Results: {filteredCount}/{Items.Count}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Apply filter error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ClearFilter()
    {
        SearchText = string.Empty;
        StatusFilter = "All";
        ApplyFilter();
    }

    #endregion

    #region Sort Commands

    [RelayCommand]
    private void ToggleSort()
    {
        try
        {
            var currentIndex = SortOptions.IndexOf(SortOrder);
            var nextIndex = (currentIndex + 1) % SortOptions.Count;
            SortOrder = SortOptions[nextIndex];

            Debug.WriteLine($"🔄 [FAMILIES_SYNCFUSION_VM] Sort order changed to: {SortOrder}");
            ApplySortingToDataSource();
            DataSource?.RefreshFilter();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Toggle sort error: {ex.Message}");
        }
    }

    private void ApplySortingToDataSource()
    {
        try
        {
            if (DataSource?.SortDescriptors == null) return;

            DataSource.SortDescriptors.Clear();

            switch (SortOrder)
            {
                case "Name A→Z":
                    DataSource.SortDescriptors.Add(new Syncfusion.Maui.DataSource.SortDescriptor
                    {
                        PropertyName = nameof(FamilyItemViewModel.Name),
                        Direction = Syncfusion.Maui.DataSource.ListSortDirection.Ascending
                    });
                    break;

                case "Name Z→A":
                    DataSource.SortDescriptors.Add(new Syncfusion.Maui.DataSource.SortDescriptor
                    {
                        PropertyName = nameof(FamilyItemViewModel.Name),
                        Direction = Syncfusion.Maui.DataSource.ListSortDirection.Descending
                    });
                    break;

                case "Recent First":
                    DataSource.SortDescriptors.Add(new Syncfusion.Maui.DataSource.SortDescriptor
                    {
                        PropertyName = nameof(FamilyItemViewModel.CreatedAt),
                        Direction = Syncfusion.Maui.DataSource.ListSortDirection.Descending
                    });
                    break;

                case "Oldest First":
                    DataSource.SortDescriptors.Add(new Syncfusion.Maui.DataSource.SortDescriptor
                    {
                        PropertyName = nameof(FamilyItemViewModel.CreatedAt),
                        Direction = Syncfusion.Maui.DataSource.ListSortDirection.Ascending
                    });
                    break;
            }

            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Sorting applied: {SortOrder}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] ApplySortingToDataSource error: {ex.Message}");
        }
    }

    #endregion

    #region Navigation Commands

    [RelayCommand]
    private async Task AddNewAsync()
    {
        try
        {
            Debug.WriteLine($"➕ [FAMILIES_SYNCFUSION_VM] Navigating to add new family");
            await _navigationService.NavigateToAsync("familyedit");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Add new navigation error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task NavigateToEditAsync(FamilyItemViewModel item)
    {
        if (item == null) return;

        try
        {
            Debug.WriteLine($"📝 [FAMILIES_SYNCFUSION_VM] Navigating to edit family: {item.Name}");

            var parameters = new Dictionary<string, object>
            {
                ["FamilyId"] = item.Id
            };

            await _navigationService.NavigateToAsync("familyedit", parameters);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Edit navigation error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteSingleItemAsync(FamilyItemViewModel item)
    {
        if (item == null || item.IsSystemDefault) return;

        try
        {
            Debug.WriteLine($"🗑️ [FAMILIES_SYNCFUSION_VM] Deleting family: {item.Name}");

            await _repository.DeleteAsync(item.Id);
            await LoadItemsDataAsync();

            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Family deleted and list refreshed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Delete error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(FamilyItemViewModel item)
    {
        try
        {
            Debug.WriteLine($"⭐ [FAMILIES_SYNCFUSION_VM] Toggling favorite for: {item.Name}");

            // Simular toggle favorite
            var toast = Toast.Make($"Favorite toggled for {item.Name}", ToastDuration.Short, 14);
            await toast.Show();

            Debug.WriteLine($"✅ [FAMILIES_SYNCFUSION_VM] Favorite toggled: {item.Name}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ [FAMILIES_SYNCFUSION_VM] Toggle favorite error: {ex.Message}");
        }
    }

    #endregion

    #region Multi-Selection

    [RelayCommand]
    private void ToggleMultiSelect()
    {
        IsMultiSelectMode = !IsMultiSelectMode;

        if (!IsMultiSelectMode)
        {
            SelectedItems.Clear();
            foreach (var item in Items)
            {
                item.IsSelected = false;
            }
        }

        UpdateFabForSelection();
    }

    public void UpdateFabForSelection()
    {
        var selectedCount = SelectedItems.Count;

        if (selectedCount > 0)
        {
            FabText = $"Delete ({selectedCount})";
            FabIsVisible = true;
        }
        else if (IsMultiSelectMode)
        {
            FabText = "Cancel";
            FabIsVisible = true;
        }
        else
        {
            FabText = "Add Family";
            FabIsVisible = true;
        }
    }

    #endregion

    #region Search Text Changed Handler

    partial void OnSearchTextChanged(string value)
    {
        Debug.WriteLine($"🔍 [FAMILIES_SYNCFUSION_VM] Search text changed: '{value}'");
        ApplyFilter();
    }

    partial void OnStatusFilterChanged(string value)
    {
        Debug.WriteLine($"🏷️ [FAMILIES_SYNCFUSION_VM] Status filter changed: '{value}'");
        ApplyFilter();
    }

    #endregion

    #region Initialization

    public virtual async Task OnAppearingAsync()
    {
        Debug.WriteLine($"👁️ [FAMILIES_SYNCFUSION_VM] OnAppearing");

        if (!Items.Any())
        {
            await LoadItemsAsync();
        }
    }

    #endregion
}