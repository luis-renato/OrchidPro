using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrchidPro.Models;
using OrchidPro.ViewModels.Base;
using OrchidPro.Services.Contracts;
using OrchidPro.Services.Navigation;
using OrchidPro.Services.Localization;
using OrchidPro.Extensions;
using System.Collections.ObjectModel;

namespace OrchidPro.ViewModels.Plants;

public partial class PlantsEditViewModel : BaseEditViewModel<Plant>
{
    private readonly IPlantRepository _plantRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ISpeciesRepository _speciesRepository;
    private readonly IVariantRepository _variantRepository;
    private readonly IFieldOptionsService _fieldOptionsService;
    private readonly ILocalizationService _localizationService;
    private readonly ILanguageService _languageService;

    #region Entity Properties
    [ObservableProperty]
    private string plantCode = string.Empty;

    [ObservableProperty]
    private string commonName = string.Empty;

    [ObservableProperty]
    private Species? selectedSpecies;

    [ObservableProperty]
    private Variant? selectedVariant;

    [ObservableProperty]
    private ObservableCollection<Species> availableSpecies = new();

    [ObservableProperty]
    private ObservableCollection<Variant> availableVariants = new();
    #endregion

    #region Event-Sourced Display Properties
    [ObservableProperty]
    private ObservableCollection<Event> recentEvents = new();

    [ObservableProperty]
    private string healthStatus = "Unknown";

    [ObservableProperty]
    private string healthStatusColor = "#9E9E9E";

    [ObservableProperty]
    private bool hasHealthIssues;

    [ObservableProperty]
    private bool needsWatering;

    [ObservableProperty]
    private bool needsFertilizing;

    [ObservableProperty]
    private bool isCurrentlyBlooming;

    [ObservableProperty]
    private DateTime? lastWatered;

    [ObservableProperty]
    private DateTime? lastFertilized;

    [ObservableProperty]
    private int daysSinceLastWatering;

    [ObservableProperty]
    private int daysSinceLastFertilizing;
    #endregion

    #region Quick Action Keys (for database)
    [ObservableProperty]
    private string quickWaterTypeKey = "water_type_tap";

    [ObservableProperty]
    private string quickFertilizerTypeKey = "fertilizer_balanced";

    [ObservableProperty]
    private string quickHealthSeverityKey = "severity_minor";

    [ObservableProperty]
    private string quickHealthSymptoms = string.Empty;
    #endregion

    #region Display Properties (for XAML compatibility)
    [ObservableProperty]
    private string quickWaterType = "";

    [ObservableProperty]
    private string quickFertilizerType = "";

    [ObservableProperty]
    private string quickHealthSeverity = "";
    #endregion

    #region Field Options Properties
    public List<string> AvailableWaterTypeKeys => _fieldOptionsService.GetWaterTypeKeys();
    public List<string> AvailableFertilizerTypeKeys => _fieldOptionsService.GetFertilizerTypeKeys();
    public List<string> AvailableHealthSeverityKeys => _fieldOptionsService.GetHealthSeverityKeys();

    public List<string> AvailableWaterTypes => _fieldOptionsService.GetWaterTypeOptions();
    public List<string> AvailableFertilizerTypes => _fieldOptionsService.GetFertilizerTypeOptions();
    public List<string> AvailableHealthSeverities => _fieldOptionsService.GetHealthSeverityOptions();
    #endregion

    #region UI Properties - Matching .resx Keys
    public new string PageTitle => IsEditMode
        ? _localizationService.GetString("Plants.Page.Edit.Title", "Edit Plant")
        : _localizationService.GetString("Plants.Page.Add.Title", "Add Plant");

    public string PlantCodeLabel => _localizationService.GetString("Plants.Label.PlantCode", "Plant Code");
    public string CommonNameLabel => _localizationService.GetString("Plants.Label.CommonName", "Common Name");
    public string SpeciesLabel => _localizationService.GetString("Plants.Label.Species", "Species");
    public string VariantLabel => _localizationService.GetString("Plants.Label.Variant", "Variant");

    public string QuickActionsLabel => _localizationService.GetString("Plants.Label.QuickActions", "Quick Actions");
    public string WaterTypeLabel => _localizationService.GetString("Plants.Label.WaterType", "Water Type");
    public string FertilizerTypeLabel => _localizationService.GetString("Plants.Label.FertilizerType", "Fertilizer Type");
    public string HealthSeverityLabel => _localizationService.GetString("Plants.Label.HealthSeverity", "Severity");
    public string SymptomsLabel => _localizationService.GetString("Plants.Label.Symptoms", "Symptoms");

    public string PlantCodePlaceholder => _localizationService.GetString("Plants.Placeholder.EnterPlantCode", "Enter plant code");
    public string CommonNamePlaceholder => _localizationService.GetString("Plants.Placeholder.EnterCommonName", "Enter common name");
    public string SymptomsPlaceholder => _localizationService.GetString("Plants.Placeholder.EnterSymptoms", "Enter symptoms");

    public string QuickWaterButton => _localizationService.GetString("Plants.Button.QuickWater", "Water Now");
    public string QuickFertilizeButton => _localizationService.GetString("Plants.Button.QuickFertilize", "Fertilize Now");
    public string QuickHealthButton => _localizationService.GetString("Plants.Button.RecordHealth", "Record Health Issue");
    public string QuickFlowerButton => _localizationService.GetString("Plants.Button.RecordFlowering", "Record Flowering");

    public string WaterSuccessMessage => _localizationService.GetString("Plants.Message.WaterSuccess", "Plant watered successfully");
    public string FertilizeSuccessMessage => _localizationService.GetString("Plants.Message.FertilizeSuccess", "Plant fertilized successfully");
    public string HealthRecordedMessage => _localizationService.GetString("Plants.Message.HealthRecorded", "Health issue recorded");
    public string FloweringRecordedMessage => _localizationService.GetString("Plants.Message.FloweringRecorded", "Flowering recorded");
    #endregion

    #region Constructor
    public PlantsEditViewModel(
        IPlantRepository plantRepository,
        IEventRepository eventRepository,
        ISpeciesRepository speciesRepository,
        IVariantRepository variantRepository,
        INavigationService navigationService,
        IFieldOptionsService fieldOptionsService,
        ILocalizationService localizationService,
        ILanguageService languageService)
        : base(plantRepository, navigationService, "Plant", "Plants")
    {
        _plantRepository = plantRepository;
        _eventRepository = eventRepository;
        _speciesRepository = speciesRepository;
        _variantRepository = variantRepository;
        _fieldOptionsService = fieldOptionsService;
        _localizationService = localizationService;
        _languageService = languageService;

        _languageService.LanguageChanged += OnLanguageChanged;
        _localizationService.LanguageChanged += OnLocalizationChanged;

        Title = "Plant Details";
        this.LogInfo("PlantsEditViewModel initialized with correct .resx localization keys");
    }
    #endregion

    #region Abstract Methods Implementation
    protected override string GetEntityName() => "Plant";
    protected override string GetEntityNamePlural() => "Plants";
    #endregion

    #region Language Change Handling
    private void OnLanguageChanged(object? sender, string newLanguage)
    {
        RefreshAllUIProperties();
    }

    private void OnLocalizationChanged(object? sender, EventArgs e)
    {
        RefreshAllUIProperties();
    }

    private void RefreshAllUIProperties()
    {
        OnPropertyChanged(nameof(AvailableWaterTypes));
        OnPropertyChanged(nameof(AvailableFertilizerTypes));
        OnPropertyChanged(nameof(AvailableHealthSeverities));

        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(PlantCodeLabel));
        OnPropertyChanged(nameof(CommonNameLabel));
        OnPropertyChanged(nameof(SpeciesLabel));
        OnPropertyChanged(nameof(VariantLabel));
        OnPropertyChanged(nameof(QuickActionsLabel));
        OnPropertyChanged(nameof(WaterTypeLabel));
        OnPropertyChanged(nameof(FertilizerTypeLabel));
        OnPropertyChanged(nameof(HealthSeverityLabel));
        OnPropertyChanged(nameof(SymptomsLabel));

        OnPropertyChanged(nameof(PlantCodePlaceholder));
        OnPropertyChanged(nameof(CommonNamePlaceholder));
        OnPropertyChanged(nameof(SymptomsPlaceholder));

        OnPropertyChanged(nameof(QuickWaterButton));
        OnPropertyChanged(nameof(QuickFertilizeButton));
        OnPropertyChanged(nameof(QuickHealthButton));
        OnPropertyChanged(nameof(QuickFlowerButton));

        // Update display properties from keys
        if (!string.IsNullOrEmpty(QuickWaterTypeKey))
            QuickWaterType = _fieldOptionsService.GetDisplayForKey(QuickWaterTypeKey);
        if (!string.IsNullOrEmpty(QuickFertilizerTypeKey))
            QuickFertilizerType = _fieldOptionsService.GetDisplayForKey(QuickFertilizerTypeKey);
        if (!string.IsNullOrEmpty(QuickHealthSeverityKey))
            QuickHealthSeverity = _fieldOptionsService.GetDisplayForKey(QuickHealthSeverityKey);

        this.LogInfo("[RefreshAllUIProperties] All UI properties refreshed after language change");
    }
    #endregion

    #region Property Change Handlers
    partial void OnQuickWaterTypeChanged(string value)
    {
        var key = _fieldOptionsService.GetKeyForDisplay(value, _fieldOptionsService.GetWaterTypeKeys());
        if (QuickWaterTypeKey != key)
            QuickWaterTypeKey = key;
    }

    partial void OnQuickFertilizerTypeChanged(string value)
    {
        var key = _fieldOptionsService.GetKeyForDisplay(value, _fieldOptionsService.GetFertilizerTypeKeys());
        if (QuickFertilizerTypeKey != key)
            QuickFertilizerTypeKey = key;
    }

    partial void OnQuickHealthSeverityChanged(string value)
    {
        var key = _fieldOptionsService.GetKeyForDisplay(value, _fieldOptionsService.GetHealthSeverityKeys());
        if (QuickHealthSeverityKey != key)
            QuickHealthSeverityKey = key;
    }

    partial void OnQuickWaterTypeKeyChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            var display = _fieldOptionsService.GetDisplayForKey(value);
            if (QuickWaterType != display)
                QuickWaterType = display;
        }
    }

    partial void OnQuickFertilizerTypeKeyChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            var display = _fieldOptionsService.GetDisplayForKey(value);
            if (QuickFertilizerType != display)
                QuickFertilizerType = display;
        }
    }

    partial void OnQuickHealthSeverityKeyChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            var display = _fieldOptionsService.GetDisplayForKey(value);
            if (QuickHealthSeverity != display)
                QuickHealthSeverity = display;
        }
    }
    #endregion

    #region Entity Mapping
    protected override void PrepareEntitySpecificFields(Plant entity)
    {
        entity.PlantCode = PlantCode;
        entity.CommonName = string.IsNullOrWhiteSpace(CommonName) ? null : CommonName;
        entity.SpeciesId = SelectedSpecies?.Id ?? Guid.Empty;
        entity.VariantId = SelectedVariant?.Id;
    }

    protected override async Task PopulateEntitySpecificFieldsAsync(Plant entity)
    {
        PlantCode = entity.PlantCode;
        CommonName = entity.CommonName ?? string.Empty;
        SelectedSpecies = entity.Species;
        SelectedVariant = entity.Variant;

        // Load computed properties
        UpdateComputedProperties(entity);

        // Load recent events
        await LoadRecentEventsAsync(entity.Id);

        // Ensure species/variants are loaded
        if (!AvailableSpecies.Any())
        {
            await LoadSpeciesAsync();
        }

        if (!AvailableVariants.Any())
        {
            await LoadVariantsAsync();
        }
    }
    #endregion

    #region Data Loading
    private async Task LoadSpeciesAsync()
    {
        try
        {
            var species = await _speciesRepository.GetAllAsync();

            AvailableSpecies.Clear();
            foreach (var s in species)
                AvailableSpecies.Add(s);
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Failed to load species");
        }
    }

    private async Task LoadVariantsAsync()
    {
        try
        {
            var variants = await _variantRepository.GetAllAsync();

            AvailableVariants.Clear();
            foreach (var v in variants)
                AvailableVariants.Add(v);
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Failed to load variants");
        }
    }

    private async Task LoadRecentEventsAsync(Guid plantId)
    {
        try
        {
            var events = await _eventRepository.GetByPlantIdAsync(plantId);
            var recentEvents = events.Take(10).ToList();

            RecentEvents.Clear();
            foreach (var e in recentEvents)
                RecentEvents.Add(e);
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Failed to load recent events");
        }
    }

    private void UpdateComputedProperties(Plant plant)
    {
        HealthStatus = plant.HealthStatus;
        HealthStatusColor = plant.HealthStatusColor;
        HasHealthIssues = plant.HasHealthIssues;
        NeedsWatering = plant.NeedsWatering;
        NeedsFertilizing = plant.NeedsFertilizing;
        IsCurrentlyBlooming = plant.IsCurrentlyBlooming;
        LastWatered = plant.LastWatered;
        LastFertilized = plant.LastFertilized;
        DaysSinceLastWatering = plant.DaysSinceLastWatering;
        DaysSinceLastFertilizing = plant.DaysSinceLastFertilizing;
    }
    #endregion

    #region Quick Actions
    [RelayCommand]
    private async Task QuickWaterAsync()
    {
        if (CurrentEntity == null) return;

        try
        {
            var waterEvent = CurrentEntity.CreateWateringEvent(QuickWaterTypeKey, "Quick watering from edit form");
            await _eventRepository.CreateAsync(waterEvent);

            await RefreshCurrentEntityAsync();
            await LoadRecentEventsAsync(CurrentEntity.Id);

            this.LogInfo($"Quick watered plant: {CurrentEntity.PlantCode}");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Failed to create watering event");
        }
    }

    [RelayCommand]
    private async Task QuickFertilizeAsync()
    {
        if (CurrentEntity == null) return;

        try
        {
            var fertEvent = new Event
            {
                PlantId = CurrentEntity.Id,
                Title = $"Fertilized {CurrentEntity.PlantCode}",
                Name = $"Fertilized {CurrentEntity.PlantCode}",
                ScheduledDate = DateTime.Today,
                ActualDate = DateTime.Now,
                EventDescription = "Quick fertilizing from edit form"
            };

            fertEvent.SetProperty(EventPropertyKeys.FertilizerType, QuickFertilizerTypeKey, "text");

            await _eventRepository.CreateAsync(fertEvent);
            await RefreshCurrentEntityAsync();
            await LoadRecentEventsAsync(CurrentEntity.Id);

            this.LogInfo($"Quick fertilized plant: {CurrentEntity.PlantCode}");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Failed to create fertilizing event");
        }
    }

    [RelayCommand]
    private async Task QuickHealthIssueAsync()
    {
        if (CurrentEntity == null || string.IsNullOrWhiteSpace(QuickHealthSymptoms)) return;

        try
        {
            var healthEvent = CurrentEntity.CreateHealthIssueEvent(QuickHealthSeverityKey, QuickHealthSymptoms);
            await _eventRepository.CreateAsync(healthEvent);

            await RefreshCurrentEntityAsync();
            await LoadRecentEventsAsync(CurrentEntity.Id);

            QuickHealthSymptoms = string.Empty;

            this.LogInfo($"Recorded health issue for plant: {CurrentEntity.PlantCode}");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Failed to create health issue event");
        }
    }

    [RelayCommand]
    private async Task QuickFloweringAsync()
    {
        if (CurrentEntity == null) return;

        try
        {
            var flowerEvent = CurrentEntity.CreateFloweringEvent();
            await _eventRepository.CreateAsync(flowerEvent);

            await RefreshCurrentEntityAsync();
            await LoadRecentEventsAsync(CurrentEntity.Id);

            this.LogInfo($"Recorded flowering for plant: {CurrentEntity.PlantCode}");
        }
        catch (Exception ex)
        {
            this.LogError(ex, "Failed to create flowering event");
        }
    }

    private async Task RefreshCurrentEntityAsync()
    {
        if (CurrentEntity?.Id != null)
        {
            var refreshed = await _plantRepository.GetByIdAsync(CurrentEntity.Id);
            if (refreshed != null)
            {
                CurrentEntity = refreshed;
                UpdateComputedProperties(CurrentEntity);
            }
        }
    }
    #endregion

    #region Validation and Change Detection
    protected override bool IsTrackedProperty(string? propertyName)
    {
        return base.IsTrackedProperty(propertyName) || propertyName is
            nameof(PlantCode) or nameof(CommonName) or nameof(SelectedSpecies) or nameof(SelectedVariant) or
            nameof(QuickWaterType) or nameof(QuickFertilizerType) or nameof(QuickHealthSeverity) or
            nameof(QuickWaterTypeKey) or nameof(QuickFertilizerTypeKey) or nameof(QuickHealthSeverityKey);
    }
    #endregion
}