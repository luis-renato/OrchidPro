using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using OrchidPro.Extensions;
using OrchidPro.Messages;
using OrchidPro.Models;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;

namespace OrchidPro.ViewModels.Species;

/// <summary>
/// ViewModel for creating and editing species with genus relationship management.
/// Extends BaseEditViewModel to handle species-specific functionality including genus selection.
/// EXACT pattern from GenusEditViewModel with family relationship management.
/// </summary>
public partial class SpeciesEditViewModel : BaseEditViewModel<Models.Species>
{
    #region Private Fields

    private readonly ISpeciesRepository _speciesRepository;
    private readonly IGenusRepository _genusRepository;
    private readonly INavigationService _navigationService;
    private bool _isEditMode = false;

    #endregion

    #region Required Base Class Overrides

    public override string EntityName => "Species";
    public override string EntityNamePlural => "Species";

    #endregion

    #region Observable Properties - Enhanced Species Properties

    [ObservableProperty] private ObservableCollection<Models.Genus> availableGenera = [];
    [ObservableProperty] private Models.Genus? selectedGenus;
    [ObservableProperty] private bool hasGenusValidationError;
    [ObservableProperty] private string genusValidationMessage = string.Empty;

    // Species-specific properties
    [ObservableProperty] private string commonName = string.Empty;
    [ObservableProperty] private string scientificName = string.Empty;
    [ObservableProperty] private string sizeCategory = string.Empty;
    [ObservableProperty] private string growthHabit = string.Empty;
    [ObservableProperty] private string rarityStatus = string.Empty;
    [ObservableProperty] private string bloomSeason = string.Empty;
    [ObservableProperty] private string bloomDuration = string.Empty;
    [ObservableProperty] private string notes = string.Empty;
    [ObservableProperty] private bool fragrance = false;

    #endregion

    #region Data Collections

    public ObservableCollection<string> SizeCategories { get; } = [
        "Miniature", "Compact", "Standard", "Large", "Giant"
    ];

    public ObservableCollection<string> GrowthHabits { get; } = [
        "Epiphyte", "Terrestrial", "Lithophyte", "Semi-terrestrial"
    ];

    public ObservableCollection<string> RarityStatuses { get; } = [
        "Common", "Uncommon", "Rare", "Very Rare", "Extremely Rare", "Extinct in Wild"
    ];

    public ObservableCollection<string> BloomSeasons { get; } = [
        "Spring", "Summer", "Fall", "Winter", "Year-round", "Multiple seasons"
    ];

    #endregion

    #region Property Change Handlers

    /// <summary>
    /// Handle property changes to update CanSave when Name changes
    /// </summary>
    private void OnPropertyChangedHandler(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        this.SafeExecute(() =>
        {
            if (e.PropertyName == nameof(Name))
            {
                // Update CanSave when name changes
                OnPropertyChanged(nameof(CanSave));
                UpdateSaveButton();
                this.LogDebug($"Name changed to '{Name}', CanSave: {CanSave}");

                // Only mark as unsaved if we actually have content
                if (!string.IsNullOrWhiteSpace(Name))
                {
                    HasUnsavedChanges = true;

                    // Trigger name validation with debounce - IGUAL outras telas
                    ScheduleNameValidation();
                }
                else
                {
                    // Clear validation when name is empty
                    ClearNameValidation();
                }
            }
        }, "Handle Property Change for CanSave");
    }

    partial void OnSelectedGenusChanged(Models.Genus? value)
    {
        this.SafeExecute(() =>
        {
            ValidateGenus();
            OnPropertyChanged(nameof(SelectedGenusName));
            OnPropertyChanged(nameof(SelectedGenusId));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(CanCreateAnother));
            UpdateSaveButton();

            // Re-validate name when genus changes - IGUAL outras telas
            if (!string.IsNullOrWhiteSpace(Name))
            {
                ScheduleNameValidation();
            }
        }, "Handle Genus Selection Change");
    }

    #endregion

    #region Genus Management Properties

    /// <summary>
    /// Selected genus ID for relationship
    /// </summary>
    public Guid? SelectedGenusId => SelectedGenus?.Id;

    /// <summary>
    /// Selected genus name for display
    /// </summary>
    public string SelectedGenusName => SelectedGenus?.Name ?? string.Empty;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Page title based on mode - for display only, not toolbar
    /// </summary>
    public new string PageTitle => IsEditMode ? "Edit Species" : "New Species";

    /// <summary>
    /// Current edit mode state for UI binding
    /// </summary>
    public new bool IsEditMode => _isEditMode;

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
    /// Can save species (genus selected + valid name + name validation passed)
    /// </summary>
    public new bool CanSave
    {
        get
        {
            var canSave = !string.IsNullOrWhiteSpace(Name) && SelectedGenusId.HasValue && IsNameValid;
            this.LogDebug($"CanSave: {canSave} (Name: '{Name}', HasGenus: {SelectedGenusId.HasValue}, NameValid: {IsNameValid})");
            return canSave;
        }
    }

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
        _navigationService = navigationService;

        SaveAndContinueCommand = new AsyncRelayCommand(SaveAndCreateAnotherAsync, () => CanCreateAnother);
        DeleteCommand = new AsyncRelayCommand(DeleteSpeciesAsync, () => CanDelete);
        CreateNewGenusCommand = new AsyncRelayCommand(NavigateToCreateGenusAsync);

        // Subscribe to genus created message - IGUAL GenusEditViewModel
        WeakReferenceMessenger.Default.Register<GenusCreatedMessage>(this, OnGenusCreated);

        // Subscribe to property changes to update CanSave
        PropertyChanged += OnPropertyChangedHandler;

        this.LogInfo("Initialized - using base functionality with genus relationship management");

        // Load available genera for selection
        _ = Task.Run(LoadAvailableGeneraAsync);
    }

    #endregion

    #region Commands

    public IAsyncRelayCommand SaveAndContinueCommand { get; }
    public IAsyncRelayCommand DeleteCommand { get; }
    public IAsyncRelayCommand CreateNewGenusCommand { get; }

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

            // Clear unsaved changes BEFORE saving to prevent navigation confirmation
            HasUnsavedChanges = false;

            // Create or update the species
            var species = new Models.Species
            {
                Id = EntityId ?? Guid.NewGuid(),
                GenusId = SelectedGenusId!.Value,
                Name = Name.Trim(),
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                CommonName = string.IsNullOrWhiteSpace(CommonName) ? null : CommonName.Trim(),
                ScientificName = string.IsNullOrWhiteSpace(ScientificName) ? null : ScientificName.Trim(),
                SizeCategory = string.IsNullOrWhiteSpace(SizeCategory) ? null : SizeCategory,
                GrowthHabit = string.IsNullOrWhiteSpace(GrowthHabit) ? null : GrowthHabit,
                RarityStatus = string.IsNullOrWhiteSpace(RarityStatus) ? null : RarityStatus,
                Fragrance = Fragrance,
                IsActive = IsActive,
                IsFavorite = IsFavorite,
                UpdatedAt = DateTime.UtcNow
            };

            var result = _isEditMode
                ? await _speciesRepository.UpdateAsync(species)
                : await _speciesRepository.CreateAsync(species);

            if (result != null)
            {
                await this.ShowSuccessToast($"Species {(_isEditMode ? "updated" : "created")} successfully!");

                // Send message to refresh species list
                WeakReferenceMessenger.Default.Send(new SpeciesUpdatedMessage());

                await NavigateBackAsync();
            }
            else
            {
                await this.ShowErrorToast($"Failed to {(_isEditMode ? "update" : "create")} species.");
            }
        }, "Save Species with Genus");
    }

    /// <summary>
    /// Save and create another species in the same genus
    /// </summary>
    private async Task SaveAndCreateAnotherAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (!ValidateForSave())
            {
                await this.ShowErrorToast("Please correct the validation errors before saving.");
                return;
            }

            this.LogInfo($"Saving and creating another species in genus: {SelectedGenusName}");

            var species = new Models.Species
            {
                Id = Guid.NewGuid(),
                GenusId = SelectedGenusId!.Value,
                Name = Name.Trim(),
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                CommonName = string.IsNullOrWhiteSpace(CommonName) ? null : CommonName.Trim(),
                ScientificName = string.IsNullOrWhiteSpace(ScientificName) ? null : ScientificName.Trim(),
                SizeCategory = string.IsNullOrWhiteSpace(SizeCategory) ? null : SizeCategory,
                GrowthHabit = string.IsNullOrWhiteSpace(GrowthHabit) ? null : GrowthHabit,
                RarityStatus = string.IsNullOrWhiteSpace(RarityStatus) ? null : RarityStatus,
                Fragrance = Fragrance,
                IsActive = IsActive,
                IsFavorite = IsFavorite,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _speciesRepository.CreateAsync(species);

            if (result != null)
            {
                await this.ShowSuccessToast("Species created! Ready for next one.");

                // Send message to refresh species list
                WeakReferenceMessenger.Default.Send(new SpeciesUpdatedMessage());

                // Clear form but keep genus selection
                ClearFormButKeepGenus();
            }
            else
            {
                await this.ShowErrorToast("Failed to create species.");
            }
        }, "Save Species and Continue");
    }

    /// <summary>
    /// Delete current species
    /// </summary>
    private async Task DeleteSpeciesAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            if (!EntityId.HasValue) return;

            var confirmed = await this.ShowConfirmationAsync(
                "Delete Species",
                $"Are you sure you want to delete '{Name}'? This action cannot be undone.",
                "Delete",
                "Cancel");

            if (!confirmed) return;

            this.LogInfo($"Deleting species: {Name}");

            var success = await _speciesRepository.DeleteAsync(EntityId.Value);

            if (success)
            {
                await this.ShowSuccessToast("Species deleted successfully!");

                // Send message to refresh species list
                WeakReferenceMessenger.Default.Send(new SpeciesUpdatedMessage());

                await NavigateBackAsync();
            }
            else
            {
                await this.ShowErrorToast("Failed to delete species.");
            }
        }, "Delete Species");
    }

    #endregion

    #region Genus Navigation - NEW FUNCTIONALITY

    /// <summary>
    /// Navigate to create new genus and return with selection
    /// </summary>
    private async Task NavigateToCreateGenusAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("CreateNewGenusCommand executed - navigating to create genus");

            // Try navigation
            try
            {
                this.LogInfo("Attempting navigation to 'genusedit' route");
                await _navigationService.NavigateToAsync("genusedit");
                this.LogSuccess("Navigation to genusedit succeeded");
            }
            catch (Exception ex)
            {
                this.LogWarning($"NavigationService failed: {ex.Message}");

                try
                {
                    this.LogInfo("Attempting Shell navigation to 'genusedit'");
                    await Shell.Current.GoToAsync("genusedit");
                    this.LogSuccess("Shell navigation to genusedit succeeded");
                }
                catch (Exception ex2)
                {
                    this.LogError($"All navigation attempts failed: {ex2.Message}");
                    await this.ShowErrorToast($"Navigation failed: {ex2.Message}");
                }
            }
        }, "Navigate to Create Genus");
    }

    /// <summary>
    /// Set selected genus for species - EXATO como SetSelectedFamily
    /// </summary>
    [RelayCommand]
    private void SetSelectedGenus(Models.Genus? genus)
    {
        SetSelectedGenusInternal(genus, markAsUnsaved: true);
    }

    /// <summary>
    /// Internal method to set genus with control over unsaved changes flag
    /// </summary>
    private void SetSelectedGenusInternal(Models.Genus? genus, bool markAsUnsaved)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"Setting selected genus: {genus?.Name ?? "None"} (markAsUnsaved: {markAsUnsaved})");

            // Use the backing field directly
            SelectedGenus = genus;

            // Update UI - IMPORTANTE: incluir UpdateSaveButton()
            OnPropertyChanged(nameof(SelectedGenus));
            OnPropertyChanged(nameof(SelectedGenusName));
            OnPropertyChanged(nameof(SelectedGenusId));
            OnPropertyChanged(nameof(GenusContext));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(CanCreateAnother));

            // Trigger save button update from base class
            UpdateSaveButton();

            // Mark as having changes only if explicitly requested (not for auto-selection)
            if (markAsUnsaved)
            {
                HasUnsavedChanges = true;
            }

        }, "Set Selected Genus");
    }

    /// <summary>
    /// Handle genus created message - EXATO como OnFamilyCreated
    /// </summary>
    private async void OnGenusCreated(object recipient, GenusCreatedMessage message)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Received genus created message: {message.GenusName}");

            // Reload genera - IGUAL Family
            await LoadAvailableGeneraAsync();

            // Auto-select the newly created genus - IGUAL Family
            var newGenus = AvailableGenera.FirstOrDefault(g => g.Id == message.GenusId);
            if (newGenus != null)
            {
                SetSelectedGenus(newGenus);
                this.LogSuccess($"Auto-selected newly created genus: {newGenus.Name}");
            }

        }, "OnGenusCreated");
    }

    #endregion

    #region Data Loading

    /// <summary>
    /// Load available genera for selection
    /// </summary>
    private async Task LoadAvailableGeneraAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Loading available genera");

            var genera = await _genusRepository.GetAllAsync();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                AvailableGenera.Clear();
                foreach (var genus in genera.OrderBy(g => g.Name))
                {
                    AvailableGenera.Add(genus);
                }
            });

            this.LogSuccess($"Loaded {genera.Count} genera");
        }, "Load Available Genera");
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initialize for creating new species with optional genus preselection
    /// </summary>
    public async Task InitializeForCreateAsync(Guid? genusId = null)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Initializing for create mode with genus preselection: {genusId}");

            _isEditMode = false;
            EntityId = null;

            // Reset form
            ClearForm();

            // Ensure genera are loaded
            if (!AvailableGenera.Any())
            {
                await LoadAvailableGeneraAsync();
            }

            // Preselect genus if provided
            if (genusId.HasValue)
            {
                SelectedGenus = AvailableGenera.FirstOrDefault(g => g.Id == genusId.Value) ?? null;
                this.LogInfo($"Preselected genus: {SelectedGenusName}");
            }

            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(IsEditMode));
            UpdateSaveButton();
        }, "Initialize for Create");
    }

    /// <summary>
    /// Initialize for editing existing species
    /// </summary>
    public async Task InitializeForEditAsync(Guid speciesId)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo($"Initializing for edit mode: {speciesId}");

            _isEditMode = true;
            EntityId = speciesId;

            // Ensure genera are loaded first
            if (!AvailableGenera.Any())
            {
                await LoadAvailableGeneraAsync();
            }

            // Load species data
            var species = await _speciesRepository.GetByIdAsync(speciesId);
            if (species != null)
            {
                LoadSpeciesData(species);
                this.LogSuccess($"Loaded species for editing: {species.Name}");
            }
            else
            {
                this.LogError($"Species not found: {speciesId}");
                await this.ShowErrorToast("Species not found.");
                await NavigateBackAsync();
                return;
            }

            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(IsEditMode));
            UpdateSaveButton();
        }, "Initialize for Edit");
    }

    #endregion

    #region Form Management

    /// <summary>
    /// Load species data into form fields
    /// </summary>
    private void LoadSpeciesData(Models.Species species)
    {
        this.SafeExecute(() =>
        {
            Name = species.Name ?? string.Empty;
            Description = species.Description ?? string.Empty;
            CommonName = species.CommonName ?? string.Empty;
            ScientificName = species.ScientificName ?? string.Empty;
            SizeCategory = species.SizeCategory ?? string.Empty;
            GrowthHabit = species.GrowthHabit ?? string.Empty;
            RarityStatus = species.RarityStatus ?? string.Empty;
            Fragrance = species.Fragrance ?? false;
            IsActive = species.IsActive;
            IsFavorite = species.IsFavorite;

            // Select the genus
            SelectedGenus = AvailableGenera.FirstOrDefault(g => g.Id == species.GenusId);

            this.LogInfo($"Loaded species data: {species.Name} in genus {SelectedGenusName}");
        }, "Load Species Data");
    }

    /// <summary>
    /// Clear form but keep genus selection for creating another species
    /// </summary>
    private void ClearFormButKeepGenus()
    {
        this.SafeExecute(() =>
        {
            var currentGenus = SelectedGenus;

            ClearForm();

            // Restore genus selection
            SelectedGenus = currentGenus;

            this.LogInfo($"Form cleared but kept genus: {SelectedGenusName}");
        }, "Clear Form But Keep Genus");
    }

    /// <summary>
    /// Clear all form fields
    /// </summary>
    private void ClearForm()
    {
        this.SafeExecute(() =>
        {
            Name = string.Empty;
            Description = string.Empty;
            CommonName = string.Empty;
            ScientificName = string.Empty;
            SizeCategory = string.Empty;
            GrowthHabit = string.Empty;
            RarityStatus = string.Empty;
            BloomSeason = string.Empty;
            BloomDuration = string.Empty;
            Notes = string.Empty;
            Fragrance = false;
            IsActive = true;
            IsFavorite = false;
            SelectedGenus = null;

            ClearValidationErrors();
        }, "Clear Form");
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validate genus selection
    /// </summary>
    private void ValidateGenus()
    {
        this.SafeExecute(() =>
        {
            if (SelectedGenus == null)
            {
                GenusValidationMessage = "Please select a genus for this species.";
                HasGenusValidationError = true;
            }
            else
            {
                GenusValidationMessage = string.Empty;
                HasGenusValidationError = false;
            }
        }, "Validate Genus Selection");
    }

    /// <summary>
    /// Schedule name validation - usa override da base
    /// </summary>
    private new void ScheduleNameValidation()
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(500); // Debounce
            await ValidateSpeciesNameAsync();
        });
    }

    /// <summary>
    /// Validate species name uniqueness within genus - SIMPLES
    /// </summary>
    private async Task ValidateSpeciesNameAsync()
    {
        if (string.IsNullOrWhiteSpace(Name) || !SelectedGenusId.HasValue)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                NameValidationMessage = string.Empty;
                IsNameValid = true;
            });
            return;
        }

        var exists = await _speciesRepository.NameExistsInGenusAsync(Name.Trim(), SelectedGenusId.Value, EntityId);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (exists)
            {
                NameValidationMessage = $"Species '{Name}' already exists in {SelectedGenusName}";
                IsNameValid = false;
            }
            else
            {
                NameValidationMessage = string.Empty;
                IsNameValid = true;
            }

            OnPropertyChanged(nameof(CanSave));
            UpdateSaveButton();
        });
    }

    /// <summary>
    /// Clear name validation
    /// </summary>
    private void ClearNameValidation()
    {
        NameValidationMessage = string.Empty;
        IsNameValid = true;
    }

    /// <summary>
    /// Validate form for saving
    /// </summary>
    private bool ValidateForSave()
    {
        var isValid = true;

        this.SafeExecute(() =>
        {
            ValidateGenus();

            if (string.IsNullOrWhiteSpace(Name))
            {
                isValid = false;
            }

            if (SelectedGenus == null)
            {
                isValid = false;
            }

            // Check name validation
            if (!IsNameValid)
            {
                isValid = false;
            }

            this.LogDebug($"Form validation result: {isValid} (Name: {!string.IsNullOrWhiteSpace(Name)}, Genus: {SelectedGenus != null}, NameValid: {IsNameValid})");
        }, "Validate Form for Save");

        return isValid;
    }

    /// <summary>
    /// Clear validation error messages
    /// </summary>
    private void ClearValidationErrors()
    {
        this.SafeExecute(() =>
        {
            GenusValidationMessage = string.Empty;
            HasGenusValidationError = false;
            ClearNameValidation();
        }, "Clear Validation Errors");
    }

    #endregion

    #region Form Progress

    /// <summary>
    /// Calculate form completion progress for progress bar
    /// </summary>
    public double FormCompletionProgress
    {
        get
        {
            var totalFields = 4; // Name, Genus, Description, CommonName as core fields
            var completedFields = 0;

            if (!string.IsNullOrWhiteSpace(Name)) completedFields++;
            if (SelectedGenus != null) completedFields++;
            if (!string.IsNullOrWhiteSpace(Description)) completedFields++;
            if (!string.IsNullOrWhiteSpace(CommonName)) completedFields++;

            return (double)completedFields / totalFields;
        }
    }

    #endregion

    #region IQueryAttributable Implementation

    /// <summary>
    /// Handle navigation parameters
    /// </summary>
    public override async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("Applying query attributes for Species edit");

            if (query.TryGetValue("SpeciesId", out var speciesIdObj) &&
                Guid.TryParse(speciesIdObj.ToString(), out var speciesId))
            {
                await InitializeForEditAsync(speciesId);
            }
            else if (query.TryGetValue("GenusId", out var genusIdObj) &&
                     Guid.TryParse(genusIdObj.ToString(), out var genusId))
            {
                await InitializeForCreateAsync(genusId);
            }
            else
            {
                await InitializeForCreateAsync();
            }
        }, "Apply Query Attributes");
    }

    #endregion

    #region Navigation Override

    /// <summary>
    /// Override NavigateBackAsync to clear unsaved changes before navigating
    /// </summary>
    protected override async Task NavigateBackAsync()
    {
        // Force clear unsaved changes to prevent confirmation dialog
        HasUnsavedChanges = false;
        this.LogInfo("Cleared HasUnsavedChanges before navigating back");

        await base.NavigateBackAsync();
    }

    #endregion

    #region Lifecycle Management

    /// <summary>
    /// Override OnAppearing to handle return from genus creation
    /// </summary>
    public override Task OnAppearingAsync()
    {
        // Call base first
        var baseTask = base.OnAppearingAsync();

        // Then our custom logic
        _ = Task.Run(async () =>
        {
            await this.SafeExecuteAsync(async () =>
            {
                // If we're returning from creating a genus and form is empty except for auto-selected genus,
                // clear the unsaved changes flag
                if (!_isEditMode &&
                    string.IsNullOrWhiteSpace(Name) &&
                    string.IsNullOrWhiteSpace(Description) &&
                    string.IsNullOrWhiteSpace(CommonName) &&
                    SelectedGenus != null)
                {
                    HasUnsavedChanges = false;
                    this.LogInfo("Cleared HasUnsavedChanges flag - auto-selected genus only");
                }
            }, "OnAppearing Cleanup");
        });

        return baseTask;
    }

    #endregion

    /// <summary>
    /// Can delete species (edit mode only)
    /// </summary>
    public new bool CanDelete => IsEditMode && EntityId.HasValue;

}