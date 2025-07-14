using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace OrchidPro.ViewModels;

/// <summary>
/// ViewModel for the Families list page with filtering, selection, and sync capabilities
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
    private string syncFilter = "All";

    [ObservableProperty]
    private int totalCount;

    [ObservableProperty]
    private int activeCount;

    [ObservableProperty]
    private int pendingSyncCount;

    [ObservableProperty]
    private bool fabIsVisible = true;

    [ObservableProperty]
    private string fabIcon = "plus";

    [ObservableProperty]
    private string fabText = "Add Family";

    public List<string> StatusFilterOptions { get; } = new() { "All", "Active", "Inactive" };
    public List<string> SyncFilterOptions { get; } = new() { "All", "Synced", "Local", "Pending", "Error" };

    public FamiliesListViewModel(IFamilyRepository familyRepository, INavigationService navigationService)
    {
        _familyRepository = familyRepository;
        _navigationService = navigationService;

        Title = "Families";
    }

    /// <summary>
    /// Loads families data with current filters
    /// </summary>
    [RelayCommand]
    private async Task LoadFamiliesAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;

            // Parse filters
            bool? statusFilter = StatusFilter switch
            {
                "Active" => true,
                "Inactive" => false,
                _ => null
            };

            SyncStatus? syncFilter = SyncFilter switch
            {
                "Synced" => SyncStatus.Synced,
                "Local" => SyncStatus.Local,
                "Pending" => SyncStatus.Pending,
                "Error" => SyncStatus.Error,
                _ => null
            };

            // Get filtered data
            var familyList = await _familyRepository.GetFilteredAsync(
                SearchText,
                statusFilter,
                syncFilter);

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
            Debug.WriteLine($"Error loading families: {ex.Message}");
            await ShowErrorAsync("Failed to load families", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Refreshes the families list
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsRefreshing) return;

        try
        {
            IsRefreshing = true;
            await LoadFamiliesAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    /// <summary>
    /// Searches families based on text input
    /// </summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
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
    /// Navigates to add new family page
    /// </summary>
    [RelayCommand]
    private async Task AddFamilyAsync()
    {
        try
        {
            await _navigationService.NavigateToAsync("familyedit");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error navigating to add family: {ex.Message}");
            await ShowErrorAsync("Navigation Error", "Failed to open add family page");
        }
    }

    /// <summary>
    /// Navigates to edit family page
    /// </summary>
    [RelayCommand]
    private async Task EditFamilyAsync(FamilyItemViewModel? family)
    {
        if (family == null) return;

        try
        {
            var parameters = new Dictionary<string, object>
            {
                ["FamilyId"] = family.Id
            };

            await _navigationService.NavigateToAsync("familyedit", parameters);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error navigating to edit family: {ex.Message}");
            await ShowErrorAsync("Navigation Error", "Failed to open edit family page");
        }
    }

    /// <summary>
    /// Deletes selected families
    /// </summary>
    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (!SelectedFamilies.Any()) return;

        try
        {
            var selectedIds = SelectedFamilies.Select(f => f.Id).ToList();
            var count = selectedIds.Count;

            var confirmed = await ShowConfirmAsync(
                "Delete Families",
                $"Are you sure you want to delete {count} {(count == 1 ? "family" : "families")}?");

            if (!confirmed) return;

            IsLoading = true;

            var deletedCount = await _familyRepository.DeleteMultipleAsync(selectedIds);

            if (deletedCount > 0)
            {
                await ShowSuccessAsync($"Successfully deleted {deletedCount} {(deletedCount == 1 ? "family" : "families")}");
                ExitMultiSelectMode();
                await LoadFamiliesAsync();
            }
            else
            {
                await ShowErrorAsync("Delete Failed", "No families were deleted");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting families: {ex.Message}");
            await ShowErrorAsync("Delete Error", "Failed to delete families");
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
    }

    /// <summary>
    /// Updates FAB based on selection
    /// </summary>
    private void UpdateFabForSelection()
    {
        if (SelectedFamilies.Any())
        {
            FabIcon = "delete";
            FabText = $"Delete ({SelectedFamilies.Count})";
        }
        else if (IsMultiSelectMode)
        {
            FabIcon = "close";
            FabText = "Cancel";
        }
        else
        {
            FabIcon = "plus";
            FabText = "Add Family";
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
            PendingSyncCount = stats.PendingCount + stats.LocalCount;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating statistics: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets appropriate empty state message
    /// </summary>
    private string GetEmptyStateMessage()
    {
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            return $"No families found matching '{SearchText}'";
        }

        if (StatusFilter != "All" || SyncFilter != "All")
        {
            return "No families match the current filters";
        }

        return "No families yet. Add your first family to get started!";
    }

    /// <summary>
    /// Handles page appearing
    /// </summary>
    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync();
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
        _ = LoadFamiliesAsync();
    }

    /// <summary>
    /// Handles sync filter changes
    /// </summary>
    partial void OnSyncFilterChanged(string value)
    {
        _ = LoadFamiliesAsync();
    }
}