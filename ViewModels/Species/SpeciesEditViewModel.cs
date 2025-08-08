using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OrchidPro.Extensions;
using OrchidPro.Messages;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using System.Collections.ObjectModel;

namespace OrchidPro.ViewModels.Species;

/// <summary>
/// MINIMAL Species edit ViewModel following EXACT pattern of GenusEditViewModel.
/// Uses BaseEditViewModel for ALL common functionality + genus relationship management.
/// NO custom save logic - uses base SaveCommand exactly like GenusEditViewModel.
/// </summary>
public partial class SpeciesEditViewModel : BaseEditViewModel<Models.Species>, IQueryAttributable
{
    #region Private Fields

    private readonly ISpeciesRepository _speciesRepository;
    private readonly IGenusRepository _genusRepository;

    #endregion

    #region Observable Properties - Following GenusEditViewModel Pattern

    [ObservableProperty]
    private Guid? selectedGenusId;

    [ObservableProperty]
    private string selectedGenusName = string.Empty;

    [ObservableProperty]
    private bool isGenusSelectionVisible = true;

    [ObservableProperty]
    private List<Genus> availableGenera = new();

    [ObservableProperty]
    private bool isLoadingGenera;

    [ObservableProperty]
    private string saveAndContinueButtonText = "Save & Add Another";

    [ObservableProperty]
    private bool showSaveAndContinue = true;

    /// <summary>
    /// Currently selected genus object for ComboBox binding
    /// </summary>
    public Genus? SelectedGenus
    {
        get => AvailableGenera?.FirstOrDefault(g => g.Id == SelectedGenusId);
        set
        {
            if (value != null)
            {
                SetSelectedGenus(value);
            }
        }
    }

    #endregion

    #region Species-Specific Properties

    [ObservableProperty]
    private string commonName = string.Empty;

    [ObservableProperty]
    private string sizeCategory = "Medium";

    [ObservableProperty]
    private string rarityStatus = "Common";

    [ObservableProperty]
    private bool fragrance = false;

    [ObservableProperty]
    private string cultivationNotes = string.Empty;

    [ObservableProperty]
    private string habitatInfo = string.Empty;

    [ObservableProperty]
    private string floweringSeason = string.Empty;

    [ObservableProperty]
    private string flowerColors = string.Empty;

    [ObservableProperty]
    private string temperaturePreference = string.Empty;

    [ObservableProperty]
    private string lightRequirements = string.Empty;

    [ObservableProperty]
    private string humidityPreference = string.Empty;

    [ObservableProperty]
    private string growthHabit = string.Empty;

    [ObservableProperty]
    private string bloomDuration = string.Empty;

    #endregion

    #region ComboBox Options

    public ObservableCollection<string> SizeCategories { get; } = new()
    {
        "Miniature", "Small", "Medium", "Large", "Giant"
    };

    public ObservableCollection<string> RarityStatuses { get; } = new()
    {
        "Common", "Uncommon", "Rare", "Very Rare", "Extinct"
    };

    public ObservableCollection<string> FloweringSeasons { get; } = new()
    {
        "Spring", "Summer", "Autumn", "Winter", "Year-round", "Variable"
    };

    public ObservableCollection<string> TemperaturePreferences { get; } = new()
    {
        "Cool", "Intermediate", "Warm"
    };

    public ObservableCollection<string> LightRequirementsList { get; } = new()
    {
        "Low", "Medium", "High", "Very High"
    };

    public ObservableCollection<string> HumidityPreferences { get; } = new()
    {
        "Low", "Medium", "High"
    };

    public ObservableCollection<string> GrowthHabits { get; } = new()
    {
        "Epiphyte", "Terrestrial", "Lithophyte"
    };

    #endregion

    #region Required Base Class Overrides

    public override string EntityName => "Species";
    public override string EntityNamePlural => "Species";

    /// <summary>
    /// Override Name property to notify CanSave changes
    /// </summary>
    public new string Name
    {
        get => base.Name;
        set
        {
            if (base.Name != value)
            {
                base.Name = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(CanCreateAnother));
            }
        }
    }

    #endregion

    #region Page Title Management - Following GenusEditViewModel Pattern

    /// <summary>
    /// Dynamic page title based on edit mode state and genus context
    /// </summary>
    public new string PageTitle => IsEditMode ? "Edit Species" : "New Species";

    /// <summary>
    /// Current edit mode state for UI binding
    /// </summary>
    public bool IsEditMode => _isEditMode;

    /// <summary>
    /// Genus context for display
    /// </summary>
    public string GenusContext =>
        !string.IsNullOrEmpty(SelectedGenusName) ? $"in {SelectedGenusName}" : "";

    /// <summary>
    /// Can create another species property
    /// </summary>
    public bool CanCreateAnother => CanSave && SelectedGenusId.HasValue;

    /// <summary>
    /// Can save species (genus selected + valid name)
    /// </summary>
    public bool CanSave => !string.IsNullOrWhiteSpace(Name) && SelectedGenusId.HasValue;

    #endregion

    #region Constructor - Following GenusEditViewModel Pattern

    /// <summary>
    /// Initialize species edit ViewModel with enhanced base functionality and genus management
    /// </summary>
    public SpeciesEditViewModel(ISpeciesRepository speciesRepository, IGenusRepository genusRepository, INavigationService navigationService)
        : base(speciesRepository, navigationService)
    {
        _speciesRepository = speciesRepository;
        _genusRepository = genusRepository;

        SaveAndContinueCommand = new AsyncRelayCommand(SaveAndCreateAnotherAsync, () => CanCreateAnother);
        DeleteCommand = new AsyncRelayCommand(DeleteSpeciesAsync, () => CanDelete);

        // Subscribe to genus created message
        WeakReferenceMessenger.Default.Register<GenusCreatedMessage>(this, OnGenusCreated);

        this.LogInfo("Initialized - using base functionality with genus relationship management");

        // Load available genera for selection
        _ = Task.Run(LoadAvailableGeneraAsync);
    }

    #endregion

    #region Commands

    public IAsyncRelayCommand SaveAndContinueCommand { get; }
    public IAsyncRelayCommand DeleteCommand { get; }

    #endregion

    #region Save Operations

    /// <summary>
    /// Override save to include genus relationship - EXACT pattern from GenusEditViewModel
    /// </summary>
    [RelayCommand]
    private async Task SaveWithGenusAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (!ValidateForSave())
            {
                await this.ShowErrorToast("Please correct the validation errors before saving.");
                return;
            }

            this.LogInfo($"Saving species: {Name} in genus {SelectedGenusName}");

            // Create or update the species
            var species = new Models.Species
            {
                Id = EntityId ?? Guid.NewGuid(),
                GenusId = SelectedGenusId!.Value,
                Name = Name.Trim(),
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                CommonName = string.IsNullOrWhiteSpace(CommonName) ? null : CommonName.Trim(),
                SizeCategory = SizeCategory,
                RarityStatus = RarityStatus,
                Fragrance = Fragrance,
                CultivationNotes = string.IsNullOrWhiteSpace(CultivationNotes) ? null : CultivationNotes.Trim(),
                HabitatInfo = string.IsNullOrWhiteSpace(HabitatInfo) ? null : HabitatInfo.Trim(),
                FloweringSeason = string.IsNullOrWhiteSpace(FloweringSeason) ? null : FloweringSeason,
                FlowerColors = string.IsNullOrWhiteSpace(FlowerColors) ? null : FlowerColors.Trim(),
                TemperaturePreference = string.IsNullOrWhiteSpace(TemperaturePreference) ? null : TemperaturePreference,
                LightRequirements = string.IsNullOrWhiteSpace(LightRequirements) ? null : LightRequirements,
                HumidityPreference = string.IsNullOrWhiteSpace(HumidityPreference) ? null : HumidityPreference,
                GrowthHabit = string.IsNullOrWhiteSpace(GrowthHabit) ? null : GrowthHabit,
                BloomDuration = string.IsNullOrWhiteSpace(BloomDuration) ? null : BloomDuration.Trim(),
                // ScientificName será gerado automaticamente ou deixado nulo
                ScientificName = null,
                IsActive = IsActive,
                IsFavorite = IsFavorite,
                CreatedAt = IsEditMode ? CreatedAt : DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            Models.Species savedSpecies;
            if (IsEditMode)
            {
                savedSpecies = await _speciesRepository.UpdateAsync(species);
                this.LogSuccess($"Updated species: {savedSpecies.Name}");
            }
            else
            {
                savedSpecies = await _speciesRepository.CreateAsync(species);
                this.LogSuccess($"Created species: {savedSpecies.Name}");
            }

            // Update local data
            EntityId = savedSpecies.Id;
            UpdatedAt = savedSpecies.UpdatedAt;

            // Clear unsaved changes state
            HasUnsavedChanges = false;

            // Show toast instead of alert
            await this.ShowSuccessToast($"Species '{savedSpecies.Name}' saved successfully!");

            // Navigate back
            await _navigationService.GoBackAsync();

        }, "Failed to save species");
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validate genus selection
    /// </summary>
    private bool ValidateGenusSelection()
    {
        return this.SafeExecute(() =>
        {
            if (!SelectedGenusId.HasValue)
            {
                this.LogWarning("Validation failed: No genus selected");
                return false;
            }

            return true;

        }, fallbackValue: false, operationName: "ValidateGenusSelection");
    }

    /// <summary>
    /// Enhanced pre-save validation
    /// </summary>
    private bool ValidateForSave()
    {
        return this.SafeExecute(() =>
        {
            var isValid = true;

            // Validate genus selection
            if (!ValidateGenusSelection())
            {
                isValid = false;
            }

            // Validate name is not empty (base validation should handle this)
            if (string.IsNullOrWhiteSpace(Name))
            {
                isValid = false;
                this.LogWarning("Validation failed: Name is required");
            }

            this.LogInfo($"Species validation complete: {isValid}");
            return isValid;

        }, fallbackValue: false, operationName: "ValidateForSave");
    }

    #endregion

    #region Genus Management - Following GenusEditViewModel Pattern

    /// <summary>
    /// Load available genera for selection dropdown
    /// </summary>
    private async Task LoadAvailableGeneraAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            IsLoadingGenera = true;
            this.LogInfo("Loading available genera for species selection");

            var genera = await _genusRepository.GetAllAsync(false); // Only active genera
            AvailableGenera = genera.OrderBy(g => g.Name).ToList();

            this.LogSuccess($"Loaded {AvailableGenera.Count} genera for selection");

            // Notify SelectedGenus after genera are loaded
            OnPropertyChanged(nameof(SelectedGenus));

        }, "Failed to load available genera");

        IsLoadingGenera = false;
    }

    /// <summary>
    /// Set selected genus for species
    /// </summary>
    [RelayCommand]
    private void SetSelectedGenus(Genus? genus)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"Setting selected genus: {genus?.Name ?? "None"}");

            SelectedGenusId = genus?.Id;
            SelectedGenusName = genus?.Name ?? string.Empty;

            // Update UI
            OnPropertyChanged(nameof(SelectedGenus));
            OnPropertyChanged(nameof(GenusContext));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(CanCreateAnother));

            // Mark as having changes if we're not initializing
            HasUnsavedChanges = true;

        }, "Set Selected Genus");
    }

    /// <summary>
    /// Handle genus created message
    /// </summary>
    private void OnGenusCreated(object recipient, GenusCreatedMessage message)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"Genus created message received: {message.GenusName}");
            _ = Task.Run(LoadAvailableGeneraAsync);
        }, "Handle Genus Created");
    }

    #endregion

    #region Species-Specific Operations - Following GenusEditViewModel Pattern

    /// <summary>
    /// Save and create another species - EXACT pattern from GenusEditViewModel
    /// </summary>
    [RelayCommand]
    private async Task SaveAndCreateAnotherAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (SelectedGenusId == null)
            {
                await this.ShowErrorToast("Please select a genus first");
                return;
            }

            // Remember current genus selection
            var currentGenusId = SelectedGenusId;
            var currentGenusName = SelectedGenusName;

            // Save current entity using SaveWithGenus
            await SaveWithGenusAsync();

            if (!HasUnsavedChanges) // Save was successful
            {
                // Reset form for new entry - EXACT pattern from GenusEditViewModel
                Name = string.Empty;
                Description = string.Empty;
                CommonName = string.Empty;
                SizeCategory = "Medium";
                RarityStatus = "Common";
                Fragrance = false;
                CultivationNotes = string.Empty;
                HabitatInfo = string.Empty;
                FloweringSeason = string.Empty;
                FlowerColors = string.Empty;
                TemperaturePreference = string.Empty;
                LightRequirements = string.Empty;
                HumidityPreference = string.Empty;
                GrowthHabit = string.Empty;
                BloomDuration = string.Empty;
                IsActive = true;
                IsFavorite = false;
                EntityId = null;
                _isEditMode = false;
                HasUnsavedChanges = false;
            }

            // Restore genus selection
            SelectedGenusId = currentGenusId;
            SelectedGenusName = currentGenusName;

            OnPropertyChanged(nameof(GenusContext));
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(SelectedGenus));

            this.LogSuccess($"Ready to create another species in {currentGenusName}");

        }, "Failed to save and create another");
    }

    /// <summary>
    /// Delete species with confirmation - EXACT pattern from GenusEditViewModel
    /// </summary>
    [RelayCommand]
    private async Task DeleteSpeciesAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (!EntityId.HasValue) return;

            var confirmed = await ShowConfirmationAsync("Delete Species", $"Are you sure you want to delete '{Name}'?", "Delete", "Cancel");

            if (confirmed)
            {
                await _speciesRepository.DeleteAsync(EntityId.Value);
                this.LogSuccess($"Species deleted: {Name}");
                await _navigationService.GoBackAsync();
            }

        }, "Failed to delete species");
    }

    /// <summary>
    /// Create new genus command
    /// </summary>
    [RelayCommand]
    private async Task CreateNewGenusAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            await _navigationService.NavigateToAsync("//genera/edit");
        }, "CreateNewGenus");
    }

    #endregion

    #region Override Save Success for Messaging - EXACT pattern from GenusEditViewModel

    /// <summary>
    /// Override OnSaveSuccessAsync to send species created message
    /// </summary>
    protected override async Task OnSaveSuccessAsync(Models.Species savedEntity)
    {
        await base.OnSaveSuccessAsync(savedEntity);

        // Send species created message if it was a new species
        if (!IsEditMode)
        {
            WeakReferenceMessenger.Default.Send(new SpeciesCreatedMessage { SpeciesName = savedEntity.Name ?? string.Empty });
        }
    }

    #endregion

    #region Data Loading - Following GenusEditViewModel Pattern

    /// <summary>
    /// Load species data for editing - EXACT pattern from GenusEditViewModel
    /// </summary>
    private async Task LoadSpeciesDataAsync(Guid speciesId)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Loading species data: {speciesId}");

            var species = await _speciesRepository.GetByIdAsync(speciesId);
            if (species != null)
            {
                // Populate basic fields - handled by BaseEditViewModel automatically
                EntityId = species.Id;
                Name = species.Name;
                Description = species.Description ?? string.Empty;
                IsActive = species.IsActive;
                IsFavorite = species.IsFavorite;

                // Load species-specific properties
                CommonName = species.CommonName ?? string.Empty;
                SizeCategory = species.SizeCategory ?? "Medium";
                RarityStatus = species.RarityStatus ?? "Common";
                Fragrance = species.Fragrance ?? false;
                CultivationNotes = species.CultivationNotes ?? string.Empty;
                HabitatInfo = species.HabitatInfo ?? string.Empty;
                FloweringSeason = species.FloweringSeason ?? string.Empty;
                FlowerColors = species.FlowerColors ?? string.Empty;
                TemperaturePreference = species.TemperaturePreference ?? string.Empty;
                LightRequirements = species.LightRequirements ?? string.Empty;
                HumidityPreference = species.HumidityPreference ?? string.Empty;
                GrowthHabit = species.GrowthHabit ?? string.Empty;
                BloomDuration = species.BloomDuration ?? string.Empty;

                // Set genus relationship
                SelectedGenusId = species.GenusId;
                var genus = AvailableGenera?.FirstOrDefault(g => g.Id == species.GenusId);
                SelectedGenusName = genus?.Name ?? string.Empty;

                OnPropertyChanged(nameof(SelectedGenus));
                OnPropertyChanged(nameof(GenusContext));

                _isEditMode = true;
                HasUnsavedChanges = false;

                this.LogSuccess($"Species data loaded: {species.Name}");
            }

        }, "Failed to load species data");
    }

    #endregion

    #region Navigation and Query Attributes - Following GenusEditViewModel Pattern

    /// <summary>
    /// Apply query attributes for navigation - FIXED to ensure edit mode is set correctly
    /// </summary>
    public new async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"ApplyQueryAttributes for Species with {query.Count} parameters");

            // ✅ CRÍTICO: Chamar o base primeiro para configurar _isEditMode
            base.ApplyQueryAttributes(query);

            // Load available genera first
            await LoadAvailableGeneraAsync();

            // Check for species ID or entity parameter - HANDLE BOTH CASES
            Guid? speciesId = null;

            // ✅ CORRIGIDO: Verificar ambos os formatos (maiúsculo e minúsculo)
            if (query.TryGetValue("SpeciesId", out var idValue) ||
                query.TryGetValue("speciesId", out idValue))
            {
                speciesId = this.ConvertToGuid(idValue);
                this.LogInfo($"Found species ID parameter: {idValue} -> {speciesId}");
            }
            else if (query.TryGetValue("species", out var entityValue) && entityValue is Models.Species species)
            {
                speciesId = species.Id;
                this.LogInfo($"Found species entity parameter: {species.Name} (ID: {species.Id})");
            }

            if (speciesId.HasValue && speciesId.Value != Guid.Empty)
            {
                this.LogInfo($"Loading species data for ID: {speciesId.Value}");
                await LoadSpeciesDataAsync(speciesId.Value);

                // ✅ CRÍTICO: Garantir que o modo de edição está definido
                _isEditMode = true;
                OnPropertyChanged(nameof(IsEditMode));
                OnPropertyChanged(nameof(PageTitle));
                this.LogInfo($"Edit mode set: IsEditMode = {IsEditMode}");
            }
            else
            {
                this.LogInfo("No valid species ID found - creating new species");
                _isEditMode = false;
                OnPropertyChanged(nameof(IsEditMode));
                OnPropertyChanged(nameof(PageTitle));
            }

            // Handle genus pre-selection for new species
            if (query.TryGetValue("genusId", out var genusIdValue) ||
                query.TryGetValue("GenusId", out genusIdValue))
            {
                var genusId = this.ConvertToGuid(genusIdValue);
                if (genusId.HasValue)
                {
                    SetSelectedGenus(AvailableGenera?.FirstOrDefault(g => g.Id == genusId.Value));
                    this.LogInfo($"Pre-selected genus: {SelectedGenusName}");
                }
            }

        }, "Apply Query Attributes");
    }
    /// <summary>
    /// Convert object to Guid safely
    /// </summary>
    private Guid? ConvertToGuid(object obj)
    {
        if (obj == null) return null;
        if (obj is Guid guid) return guid;
        if (obj is string str && Guid.TryParse(str, out var parsedGuid)) return parsedGuid;
        return null;
    }

    #endregion
}

/// <summary>
/// Message for genus created events
/// </summary>
public class GenusCreatedMessage
{
    public string GenusName { get; set; } = string.Empty;
}

/// <summary>
/// Message for species created events
/// </summary>
public class SpeciesCreatedMessage
{
    public string SpeciesName { get; set; } = string.Empty;
}