using OrchidPro.Models;
using OrchidPro.ViewModels.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Navigation;
using OrchidPro.Extensions;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using OrchidPro.Services.Localization;

namespace OrchidPro.ViewModels.Plants;

public partial class PlantsListViewModel : BaseListViewModel<Plant, PlantItemViewModel>
{
    #region Private Fields
    private readonly IPlantRepository _plantRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILocalizationService _localizationService; // Add this field
    #endregion


    #region Required Base Class Overrides
    public override string EntityName => "Plant";
    public override string EntityNamePlural => "Plants";
    public override string EditRoute => "plantedit";
    #endregion

    #region Dashboard Properties
    [ObservableProperty]
    private ObservableCollection<PlantItemViewModel> plantsNeedingCare = new();

    [ObservableProperty]
    private ObservableCollection<PlantItemViewModel> healthyPlants = new();

    [ObservableProperty]
    private ObservableCollection<PlantItemViewModel> plantsWithIssues = new();

    [ObservableProperty]
    private ObservableCollection<PlantItemViewModel> bloomingPlants = new();

    [ObservableProperty]
    private string selectedFilter = "All";

    [ObservableProperty]
    private ObservableCollection<string> filterOptions = new();

    // Dashboard stats
    [ObservableProperty]
    private int totalPlantsCount;

    [ObservableProperty]
    private int healthyPlantsCount;

    [ObservableProperty]
    private int plantsWithIssuesCount;

    [ObservableProperty]
    private int plantsNeedingWaterCount;

    [ObservableProperty]
    private int plantsNeedingFertilizerCount;

    [ObservableProperty]
    private int bloomingPlantsCount;
    #endregion

    #region Constructor
    public PlantsListViewModel(
        IPlantRepository plantRepository,
        IEventRepository eventRepository,
        INavigationService navigationService,
        ILocalizationService localizationService) // Add this parameter
        : base(plantRepository, navigationService)
    {
        _plantRepository = plantRepository;
        _eventRepository = eventRepository;
        _localizationService = localizationService; // Initialize this field
        LoadFilterOptions();
        this.LogInfo("Plants List ViewModel initialized with Event-Sourced capabilities");
    }
    #endregion

    #region ONLY REQUIRED: CreateItemViewModel
    protected override PlantItemViewModel CreateItemViewModel(Plant entity)
    {
        return new PlantItemViewModel(entity);
    }
    #endregion

    #region Dashboard Loading
    public async Task LoadDashboardAsync()
    {
        try
        {
            IsLoading = true;

            // Load stats
            var stats = await _plantRepository.GetPlantStatisticsAsync();
            TotalPlantsCount = stats.TotalCount;
            HealthyPlantsCount = stats.HealthyPlantsCount;
            PlantsWithIssuesCount = stats.PlantsWithIssuesCount;
            PlantsNeedingWaterCount = stats.PlantsNeedingWaterCount;
            PlantsNeedingFertilizerCount = stats.PlantsNeedingFertilizerCount;
            BloomingPlantsCount = stats.BloomingPlantsCount;

            // Load categorized lists
            await Task.WhenAll(
                LoadPlantsNeedingCareAsync(),
                LoadHealthyPlantsAsync(),
                LoadPlantsWithIssuesAsync(),
                LoadBloomingPlantsAsync()
            );

            this.LogInfo("Dashboard loaded successfully");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Failed to load dashboard");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadPlantsNeedingCareAsync()
    {
        var plants = await _plantRepository.GetPlantsNeedingCareAsync();
        var careItems = plants.Take(10).Select(CreateItemViewModel).ToList();

        PlantsNeedingCare.Clear();
        foreach (var plant in careItems)
            PlantsNeedingCare.Add(plant);
    }

    private async Task LoadHealthyPlantsAsync()
    {
        var plants = await _plantRepository.GetByHealthStatusAsync("Healthy");
        var healthyItems = plants.Take(10).Select(CreateItemViewModel).ToList();

        HealthyPlants.Clear();
        foreach (var plant in healthyItems)
            HealthyPlants.Add(plant);
    }

    private async Task LoadPlantsWithIssuesAsync()
    {
        var plants = await _plantRepository.GetPlantsWithHealthIssuesAsync();
        var issueItems = plants.Take(10).Select(CreateItemViewModel).ToList();

        PlantsWithIssues.Clear();
        foreach (var plant in issueItems)
            PlantsWithIssues.Add(plant);
    }

    private async Task LoadBloomingPlantsAsync()
    {
        var plants = await _plantRepository.GetBloomingPlantsAsync();
        var bloomingItems = plants.Take(10).Select(CreateItemViewModel).ToList();

        BloomingPlants.Clear();
        foreach (var plant in bloomingItems)
            BloomingPlants.Add(plant);
    }
    #endregion

    #region Filtering
    private void LoadFilterOptions()
    {
        FilterOptions.Clear();
        FilterOptions.Add(_localizationService.GetString("Plants.Filter.All", "All"));
        FilterOptions.Add(_localizationService.GetString("Plants.Filter.Healthy", "Healthy"));
        FilterOptions.Add(_localizationService.GetString("Plants.Filter.NeedsWater", "Needs Water"));
        FilterOptions.Add(_localizationService.GetString("Plants.Filter.NeedsFertilizer", "Needs Fertilizer"));
        FilterOptions.Add(_localizationService.GetString("Plants.Filter.HealthIssues", "Health Issues"));
        FilterOptions.Add(_localizationService.GetString("Plants.Filter.Blooming", "Blooming"));
        FilterOptions.Add(_localizationService.GetString("Plants.Filter.Favorites", "Favorites"));
    }

    [RelayCommand]
    private async Task FilterPlantsAsync(string filter)
    {
        SelectedFilter = filter;
        await ApplyFilterAsync();
    }

    private async Task ApplyFilterAsync()
    {
        try
        {
            IsLoading = true;

            List<Plant> filteredPlants = SelectedFilter switch
            {
                "Healthy" => (await _plantRepository.GetByHealthStatusAsync("Healthy")).ToList(),
                "Needs Water" => (await _plantRepository.GetPlantsNeedingCareAsync()).Where(p => p.NeedsWatering).ToList(),
                "Needs Fertilizer" => (await _plantRepository.GetPlantsNeedingCareAsync()).Where(p => p.NeedsFertilizing).ToList(),
                "Health Issues" => (await _plantRepository.GetPlantsWithHealthIssuesAsync()).ToList(),
                "Blooming" => (await _plantRepository.GetBloomingPlantsAsync()).ToList(),
                "Favorites" => (await _plantRepository.GetAllAsync()).Where(p => p.IsFavorite).ToList(),
                _ => (await _plantRepository.GetAllAsync()).ToList()
            };

            Items.Clear();
            foreach (var plant in filteredPlants)
                Items.Add(CreateItemViewModel(plant));

            this.LogInfo($"Applied filter: {SelectedFilter}, found {Items.Count} plants");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Failed to apply filter");
        }
        finally
        {
            IsLoading = false;
        }
    }
    #endregion

    #region Quick Actions
    [RelayCommand]
    private async Task QuickWaterPlantAsync(PlantItemViewModel plantVm)
    {
        try
        {
            var waterEvent = plantVm.Entity.CreateWateringEvent("Quick watering from dashboard");
            await _eventRepository.CreateAsync(waterEvent);

            // Refresh the plant data
            await RefreshPlantAsync(plantVm);
            await LoadDashboardAsync(); // Refresh dashboard stats

            this.LogInfo($"Quick watered plant: {plantVm.PlantCode}");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Failed to water plant");
        }
    }

    [RelayCommand]
    private async Task QuickFertilizePlantAsync(PlantItemViewModel plantVm)
    {
        try
        {
            var fertEvent = new Event
            {
                PlantId = plantVm.Entity.Id,
                Title = $"Fertilized {plantVm.PlantCode}",
                Name = $"Fertilized {plantVm.PlantCode}",
                ScheduledDate = DateTime.Today,
                ActualDate = DateTime.Now,
                EventDescription = "Quick fertilizing from dashboard"
            };

            await _eventRepository.CreateAsync(fertEvent);
            await RefreshPlantAsync(plantVm);
            await LoadDashboardAsync();

            this.LogInfo($"Quick fertilized plant: {plantVm.PlantCode}");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Failed to fertilize plant");
        }
    }

    private async Task RefreshPlantAsync(PlantItemViewModel plantVm)
    {
        var refreshedPlant = await _plantRepository.GetByIdAsync(plantVm.Entity.Id);
        if (refreshedPlant != null)
        {
            // Find and update the item in the main list
            var existingItem = Items.FirstOrDefault(i => i.Entity.Id == plantVm.Entity.Id);
            if (existingItem != null)
            {
                var index = Items.IndexOf(existingItem);
                Items[index] = CreateItemViewModel(refreshedPlant);
            }
        }
    }
    #endregion

    #region UI COMPATIBILITY: Expose Commands
    public IAsyncRelayCommand<PlantItemViewModel> DeleteSingleCommand => DeleteSingleItemCommand;
    public new IAsyncRelayCommand DeleteSelectedCommand => base.DeleteSelectedCommand;
    #endregion
}
