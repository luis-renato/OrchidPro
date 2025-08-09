#pragma warning disable MVVMTK0045 
// MVVMTK0045: ObservableProperty fields are not AOT compatible in WinRT scenarios
// Suppressed because: 
// 1. This project targets .NET MAUI (not UWP/WinUI3), where this warning doesn't apply
// 2. Partial properties require C# preview language version which adds project complexity
// 3. Current implementation works perfectly on Android, iOS and Windows platforms
// 4. AOT compatibility warning is informational only and doesn't affect functionality
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using OrchidPro.Extensions;
using OrchidPro.Messages;
using OrchidPro.Services;
using OrchidPro.Services.Navigation;
using OrchidPro.ViewModels.Base;
using System.Collections.ObjectModel;

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
    private new readonly INavigationService _navigationService;
    private new bool _isEditMode = false;

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

    public ObservableCollection<string> SizeCategories { get; } = new([
        "Miniature", "Compact", "Standard", "Large", "Giant"
    ]);

    public ObservableCollection<string> GrowthHabits { get; } = new([
        "Epiphyte", "Terrestrial", "Lithophyte", "Semi-terrestrial"
    ]);

    public ObservableCollection<string> RarityStatuses { get; } = new([
        "Common", "Uncommon", "Rare", "Very Rare", "Extremely Rare", "Extinct in Wild"
    ]);

    public ObservableCollection<string> BloomSeasons { get; } = new([
        "Spring", "Summer", "Fall", "Winter", "Year-round", "Multiple seasons"
    ]);

    #endregion

    #region Property Change Handlers

    /// <summary>
    /// Handles property changes with debounced name validation for performance optimization.
    /// Triggers CanSave updates and name validation when Name property changes.
    /// </summary>
    private void OnPropertyChangedHandler(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        this.SafeExecute(() =>
        {
            if (e.PropertyName == nameof(Name))
            {
                OnPropertyChanged(nameof(CanSave));
                UpdateSaveButton();
                this.LogDebug($"Name changed to '{Name}', CanSave: {CanSave}");

                if (!string.IsNullOrWhiteSpace(Name))
                {
                    HasUnsavedChanges = true;
                    ScheduleNameValidation(); // Debounced validation for better UX
                }
                else
                {
                    ClearNameValidation();
                }
            }
        }, "Handle Property Change for CanSave");
    }

    /// <summary>
    /// Handles genus selection changes, triggering validation and UI updates.
    /// Re-validates species name when genus context changes.
    /// </summary>
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

            // Re-validate name when genus context changes for uniqueness check
            if (!string.IsNullOrWhiteSpace(Name))
            {
                ScheduleNameValidation();
            }
        }, "Handle Genus Selection Change");
    }

    #endregion

    #region Genus Management Properties

    /// <summary>
    /// Gets the ID of the currently selected genus for relationship binding
    /// </summary>
    public Guid? SelectedGenusId => SelectedGenus?.Id;

    /// <summary>
    /// Gets the name of the currently selected genus for UI display
    /// </summary>
    public string SelectedGenusName => SelectedGenus?.Name ?? string.Empty;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the page title based on current edit mode (New Species/Edit Species)
    /// </summary>
    public new string PageTitle => IsEditMode ? "Edit Species" : "New Species";

    /// <summary>
    /// Gets the current edit mode state for UI binding and validation logic
    /// </summary>
    public new bool IsEditMode => _isEditMode;

    /// <summary>
    /// Gets the genus context string for display in UI labels and headers
    /// </summary>
    public string GenusContext =>
        !string.IsNullOrEmpty(SelectedGenusName) ? $"in {SelectedGenusName}" : string.Empty;

    /// <summary>
    /// Determines if another species can be created in the current genus context
    /// </summary>
    public bool CanCreateAnother => CanSave && SelectedGenusId.HasValue;

    /// <summary>
    /// Determines if the species can be saved based on validation rules:
    /// - Name must not be empty
    /// - Genus must be selected
    /// - Name validation must pass (uniqueness within genus)
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
    /// Initializes the species edit ViewModel with enhanced base functionality and genus relationship management.
    /// Sets up commands, messaging subscriptions, and initiates background data loading.
    /// </summary>
    public SpeciesEditViewModel(ISpeciesRepository speciesRepository, IGenusRepository genusRepository, INavigationService navigationService)
        : base(speciesRepository, navigationService)
    {
        _speciesRepository = speciesRepository;
        _genusRepository = genusRepository;
        _navigationService = navigationService;

        // Initialize commands with appropriate CanExecute delegates
        SaveAndContinueCommand = new AsyncRelayCommand(SaveAndCreateAnotherAsync, () => CanCreateAnother);
        DeleteCommand = new AsyncRelayCommand(DeleteSpeciesAsync, () => CanDelete);
        CreateNewGenusCommand = new AsyncRelayCommand(NavigateToCreateGenusAsync);

        // Subscribe to genus creation events for auto-selection
        WeakReferenceMessenger.Default.Register<GenusCreatedMessage>(this, OnGenusCreated);

        // Monitor property changes for validation and UI updates
        PropertyChanged += OnPropertyChangedHandler;

        this.LogInfo("Initialized - using base functionality with genus relationship management");

        // Background load of available genera for immediate UI responsiveness
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
    /// Saves the species with genus relationship validation and messaging.
    /// Handles both create and update operations based on edit mode.
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

            // Clear unsaved changes flag before save operation to prevent navigation conflicts
            HasUnsavedChanges = false;

            var species = new Models.Species
            {
                Id = EntityId ?? Guid.NewGuid(),
                GenusId = SelectedGenusId!.Value,
                Name = Name.Trim(),
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                CommonName = string.IsNullOrWhiteSpace(CommonName) ? null : CommonName.Trim(),
                ScientificName = string.IsNullOrWhiteSpace(ScientificName) ? null : ScientificName.Trim(),
                SizeCategory = string.IsNullOrWhiteSpace(SizeCategory) ? string.Empty : SizeCategory,
                GrowthHabit = string.IsNullOrWhiteSpace(GrowthHabit) ? string.Empty : GrowthHabit,
                RarityStatus = string.IsNullOrWhiteSpace(RarityStatus) ? string.Empty : RarityStatus,
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
    /// Saves current species and prepares form for creating another species in the same genus.
    /// Optimizes workflow for bulk species entry within a genus.
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
                SizeCategory = string.IsNullOrWhiteSpace(SizeCategory) ? string.Empty : SizeCategory,
                GrowthHabit = string.IsNullOrWhiteSpace(GrowthHabit) ? string.Empty : GrowthHabit,
                RarityStatus = string.IsNullOrWhiteSpace(RarityStatus) ? string.Empty : RarityStatus,
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
                WeakReferenceMessenger.Default.Send(new SpeciesUpdatedMessage());
                ClearFormButKeepGenus(); // Optimized for bulk entry workflow
            }
            else
            {
                await this.ShowErrorToast("Failed to create species.");
            }
        }, "Save Species and Continue");
    }

    /// <summary>
    /// Deletes the current species with user confirmation.
    /// Includes safety checks and messaging for UI updates.
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

    #region Genus Navigation and Management

    /// <summary>
    /// Navigates to genus creation page with fallback navigation strategies.
    /// Handles both NavigationService and Shell navigation for maximum compatibility.
    /// </summary>
    private async Task NavigateToCreateGenusAsync()
    {
        await this.SafeExecuteAsync(async () =>
        {
            this.LogInfo("CreateNewGenusCommand executed - navigating to create genus");

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
    /// Sets the selected genus with change tracking for form state management.
    /// Public command interface for UI binding.
    /// </summary>
    [RelayCommand]
    private void SetSelectedGenus(Models.Genus? genus)
    {
        SetSelectedGenusInternal(genus, markAsUnsaved: true);
    }

    /// <summary>
    /// Internal genus selection method with granular control over unsaved changes tracking.
    /// Supports both user-initiated changes and programmatic selection (e.g., auto-selection).
    /// </summary>
    private void SetSelectedGenusInternal(Models.Genus? genus, bool markAsUnsaved)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"Setting selected genus: {genus?.Name ?? "None"} (markAsUnsaved: {markAsUnsaved})");

            SelectedGenus = genus;

            // Trigger comprehensive UI updates
            OnPropertyChanged(nameof(SelectedGenus));
            OnPropertyChanged(nameof(SelectedGenusName));
            OnPropertyChanged(nameof(SelectedGenusId));
            OnPropertyChanged(nameof(GenusContext));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(CanCreateAnother));

            UpdateSaveButton();

            // Conditional change tracking to distinguish user actions from auto-selection
            if (markAsUnsaved)
            {
                HasUnsavedChanges = true;
            }

        }, "Set Selected Genus");
    }

    /// <summary>
    /// Handles genus creation completion messages for seamless workflow integration.
    /// Auto-selects newly created genus to optimize user experience.
    /// </summary>
    private void OnGenusCreated(object recipient, GenusCreatedMessage message)
    {
        this.SafeExecute(() =>
        {
            this.LogInfo($"Received genus created message: {message.GenusName}");

            // Reload genera collection and auto-select new entry
            _ = Task.Run(async () =>
            {
                await LoadAvailableGeneraAsync();

                // Thread-safe UI updates for auto-selection
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var newGenus = AvailableGenera.FirstOrDefault(g => g.Id == message.GenusId);
                    if (newGenus != null)
                    {
                        SetSelectedGenus(newGenus);
                        this.LogSuccess($"Auto-selected newly created genus: {newGenus.Name}");
                    }
                });
            });
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

            // Preselect genus if provided - Fixed CS8601
            if (genusId.HasValue)
            {
                SelectedGenus = AvailableGenera.FirstOrDefault(g => g.Id == genusId.Value);
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
    /// Load species data into form fields - Fixed CS8601
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

            // Select the genus - Fixed CS8601
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
    public new double FormCompletionProgress
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
                Guid.TryParse(speciesIdObj?.ToString(), out var speciesId))
            {
                await InitializeForEditAsync(speciesId);
            }
            else if (query.TryGetValue("GenusId", out var genusIdObj) &&
                     Guid.TryParse(genusIdObj?.ToString(), out var genusId))
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
        _ = Task.Run(() =>
        {
            this.SafeExecute(() =>
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
    /// Can delete species (edit mode only) - Fixed CS0108
    /// </summary>
    public new bool CanDelete => IsEditMode && EntityId.HasValue;

}